using System;using System.IO;using System.Net;using System.Net.Http;using System.Diagnostics;using System.Drawing;using System.Windows.Forms;using System.Threading.Tasks;using System.Text;using System.Text.RegularExpressions;using System.Collections.Generic;using System.Web.Script.Serialization;using System.Security.Cryptography;using System.IO.Compression;
[assembly:System.Reflection.AssemblyVersion("0.5.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("0.5.0.0")]
[assembly:System.Reflection.AssemblyProduct("OpenSAAB Collector")]
[assembly:System.Reflection.AssemblyDescription("Private USB adapter capture and upload")]
namespace OpenSaab.Collector {
 static class Program {
  [STAThread] static int Main(string[] args) {
   if(args.Length==2 && args[0]=="--worker")return CaptureEngine.Worker(args[1]);
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new CollectorForm());return 0;
  }
 }
 public class UsbDevice {
  public string Hub;public int Address;public string Label;
  public override string ToString(){return Hub.Replace(@"\\.\","")+" — "+Label;}
 }
 public static class Bundle {
  public const string Version="0.5.0";
  public const string Consent="collector-capture-v1";
  public const string Endpoint="https://www.opensaab.com/api/collector/captures";
  public static string Hash(string path){using(var f=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
  public static string Build(string dir){
   string zip=Path.Combine(dir,"capture.zip");if(File.Exists(zip))return zip;
   string temp=zip+".tmp";if(File.Exists(temp))File.Delete(temp);
   using(var z=ZipFile.Open(temp,ZipArchiveMode.Create)){foreach(string name in new[]{"usb.pcap","session.json","actions.jsonl"})z.CreateEntryFromFile(Path.Combine(dir,name),name,CompressionLevel.Optimal);}
   if(new FileInfo(temp).Length>64L*1024*1024)throw new Exception("Capture exceeds upload limit; files are saved locally");File.Move(temp,zip);return zip;
  }
  public static async Task<string> Upload(string dir){
   string zip=Build(dir),digest=Hash(zip);
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   using(var handler=new HttpClientHandler{AllowAutoRedirect=false})using(var client=new HttpClient(handler){Timeout=TimeSpan.FromMinutes(5)})using(var f=File.OpenRead(zip))using(var req=new HttpRequestMessage(HttpMethod.Post,Endpoint)){
    long bytes=f.Length;req.Content=new StreamContent(f);req.Content.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");req.Content.Headers.ContentLength=bytes;
    req.Headers.Add("X-Content-SHA256",digest);req.Headers.Add("X-OpenSAAB-Consent",Consent);
    using(var response=await client.SendAsync(req)){
     if(!response.IsSuccessStatusCode)throw new Exception("Upload not confirmed (HTTP "+(int)response.StatusCode+"). Local files retained. Retry later.");
     string text=await response.Content.ReadAsStringAsync();var value=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(text);
     if(!value.ContainsKey("stored") || !(value["stored"] is bool) || !(bool)value["stored"] || !value.ContainsKey("sha256") || (string)value["sha256"]!=digest || !value.ContainsKey("receipt") || (string)value["receipt"]!="OSCAP-"+digest || Convert.ToInt64(value["bytes"])!=bytes)throw new Exception("Server receipt could not be verified. Local files retained.");
     File.WriteAllText(Path.Combine(dir,"upload-receipt.json"),text,new UTF8Encoding(false));return (string)value["receipt"];
    }
   }
  }
 }
 public class CollectorForm:Form {
  ComboBox devices=new ComboBox();TextBox adapter=new TextBox(),note=new TextBox();CheckBox consent=new CheckBox();Button setup=new Button(),refresh=new Button(),start=new Button(),stop=new Button(),add=new Button(),retry=new Button(),folder=new Button();Label status=new Label();
  string usb,session;Process worker;Dictionary<string,object> manifest;bool busy,finishing;int notes;DateTime captureStarted;Timer timer=new Timer();
  public CollectorForm(){
   Text="OpenSAAB Collector";ClientSize=new Size(690,570);MinimumSize=new Size(706,609);Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(246,248,251);
   AddLabel("OpenSAAB Collector",24,18,640,36,21,true);AddLabel("Record your adapter. Help bring more devices to OpenSAAB.",24,58,640,27,10,false);
   AddLabel("1   Choose your USB adapter",24,100,640,27,12,true);
   devices.SetBounds(24,134,500,30);devices.DropDownStyle=ComboBoxStyle.DropDownList;Controls.Add(devices);Btn(refresh,"Refresh",536,132,128,32,async delegate{await RefreshDevices();});
   adapter.SetBounds(24,177,640,27);adapter.MaxLength=240;Controls.Add(adapter);AddLabel("Adapter model / driver version / car model and year (optional)",24,208,640,24,9,false);
   Btn(setup,"Set up USBPcap",24,243,170,34,async delegate{await Install();});AddLabel("Wireshark is not required. First driver install needs a restart.",207,248,454,30,9,false);
   consent.SetBounds(24,290,640,52);consent.Text="I agree to upload this adapter capture privately to OpenSAAB when I stop.\nIt may contain my VIN, adapter serial and diagnostic/security data.";Controls.Add(consent);
   Btn(start,"Start capture",24,357,194,42,async delegate{await StartCapture();});Btn(stop,"Stop & upload",234,357,194,42,async delegate{await Finish();});stop.Enabled=false;
   Btn(folder,"Open saved files",445,357,219,42,delegate{Process.Start(session ?? BaseDir());});
   note.SetBounds(24,418,500,28);note.MaxLength=400;Controls.Add(note);Btn(add,"Add step note",536,415,128,34,delegate{AddNote();});add.Enabled=false;
   status.SetBounds(24,460,640,56);status.Text="Connect the adapter, then refresh.";Controls.Add(status);
   Btn(retry,"Retry saved upload",24,522,205,32,async delegate{await Retry();});
   AddLabel("Private uploads • Local copy retained • No driver shims",245,528,435,24,9,false);
   timer.Interval=500;timer.Tick+=async delegate{if(worker!=null && !busy && !finishing){if(worker.HasExited)await Finish();else status.Text="Recording — launch Tech2Win, select your adapter, then read VIN / ECM information / DTCs.\nElapsed: "+(DateTime.UtcNow-captureStarted).ToString(@"mm\:ss")+" (15-minute / 60 MiB limit)";}};timer.Start();
   Shown+=async delegate{await RefreshDevices();};FormClosing+=delegate(object sender,FormClosingEventArgs e){if(worker!=null || busy){e.Cancel=true;MessageBox.Show("Finish the capture or current upload before closing. Local files will be retained.",Text);}};
  }
  string BaseDir(){string p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"OpenSAAB-Captures");Directory.CreateDirectory(p);return p;}
  void AddLabel(string text,int x,int y,int w,int h,int size,bool bold){var l=new Label{Text=text,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular)};l.SetBounds(x,y,w,h);Controls.Add(l);}
  void Btn(Button b,string text,int x,int y,int w,int h,EventHandler action){b.Text=text;b.SetBounds(x,y,w,h);b.Click+=action;Controls.Add(b);}
  void SetBusy(bool value){busy=value;setup.Enabled=!value && worker==null && usb==null;setup.Text=usb==null?"Set up USBPcap":"USBPcap installed";refresh.Enabled=retry.Enabled=!value && worker==null;start.Enabled=!value && worker==null && devices.Items.Count>0;stop.Enabled=!value && worker!=null;devices.Enabled=adapter.Enabled=consent.Enabled=!value && worker==null;add.Enabled=!value && worker!=null;}
  void Error(Exception e){status.Text=e.Message;MessageBox.Show(e.Message,"OpenSAAB Collector",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  string FindUsb(){foreach(string path in new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"USBPcap\USBPcapCMD.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),@"USBPcap\USBPcapCMD.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"Wireshark\extcap\USBPcapCMD.exe")})if(File.Exists(path))return path;return null;}
  async Task RefreshDevices(){SetBusy(true);try{
   usb=FindUsb();devices.Items.Clear();if(usb==null){status.Text="Click Set up USBPcap to download the official driver installer.";return;}
   var found=await Task.Run(delegate{
    var list=new List<UsbDevice>();string hubs=CaptureEngine.Query(usb,"--extcap-interfaces");
    foreach(Match h in Regex.Matches(hubs,@"(?m)^interface \{value=(\\\\\.\\USBPcap[0-9]+)\}")){
     string hub=h.Groups[1].Value;string data=CaptureEngine.Query(usb,"--extcap-interface "+CaptureEngine.Quote(hub)+" --extcap-config");
     foreach(string line in data.Split('\n')){var d=Regex.Match(line,@"^value \{arg=\d+\}\{value=(\d+)\}\{display=([^}]+)\}");if(d.Success && !line.Contains("{parent="))list.Add(new UsbDevice{Hub=hub,Address=int.Parse(d.Groups[1].Value),Label=d.Groups[2].Value});}
    }return list;
   });foreach(var d in found)devices.Items.Add(d);status.Text=found.Count>0?"Select the adapter (not a keyboard, camera or other peripheral). Keep it connected during capture.":"No capture devices found. Restart Windows if USBPcap was just installed.";
  }catch(Exception e){Error(e);}finally{SetBusy(false);}}
  async Task Install(){SetBusy(true);try{
   status.Text="Downloading the official USBPcap installer…";ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   string path=Path.Combine(Path.GetTempPath(),"USBPcapSetup-1.5.4.0-"+Guid.NewGuid().ToString("N")+".exe");
   using(var client=new HttpClient()){client.Timeout=TimeSpan.FromMinutes(3);var bytes=await client.GetByteArrayAsync("https://github.com/desowin/usbpcap/releases/download/1.5.4.0/USBPcapSetup-1.5.4.0.exe");File.WriteAllBytes(path,bytes);}
   if(Bundle.Hash(path)!="87a7edf9bbbcf07b5f4373d9a192a6770d2ff3add7aa1e276e82e38582ccb622"){File.Delete(path);throw new Exception("USBPcap download did not match the official release checksum");}
   status.Text="Complete the USBPcap installer, restart Windows, then reopen Collector.";
   using(var p=Process.Start(new ProcessStartInfo(path){UseShellExecute=true}))await Task.Run(delegate{p.WaitForExit();});
   status.Text="If USBPcap was installed, restart Windows and reopen Collector before recording.";
  }catch(Exception e){Error(e);}finally{SetBusy(false);}}
  async Task StartCapture(){if(!consent.Checked){MessageBox.Show("Please confirm the private upload notice before recording.",Text);return;}var device=devices.SelectedItem as UsbDevice;if(device==null){MessageBox.Show("Select your adapter first.",Text);return;}
   SetBusy(true);try{
    using(var service=new System.ServiceProcess.ServiceController("OpenSAABCollector")){try{if(service.Status==System.ServiceProcess.ServiceControllerStatus.Running)throw new Exception("The old Collector service is running. Stop it before recording; it has a separate uploader.");}catch(InvalidOperationException){}}
    if(Process.GetProcessesByName("USBPcapCMD").Length>0)throw new Exception("Another USBPcap process is running. Stop it first.");
    if(Process.GetProcessesByName("Tech2Win").Length>0 || Process.GetProcessesByName("emulator").Length>0)throw new Exception("Close Tech2Win before capture so initialization is included.");
    string fresh=await Task.Run(()=>CaptureEngine.Query(usb,"--extcap-interface "+CaptureEngine.Quote(device.Hub)+" --extcap-config"));
    if(!fresh.Contains("{value="+device.Address+"}{display="+device.Label+"}"))throw new Exception("Device list changed. Refresh and select the adapter again.");
    captureStarted=DateTime.UtcNow;session=Path.Combine(BaseDir(),captureStarted.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(session);notes=0;
    manifest=new Dictionary<string,object>{{"format",1},{"collector_version",Bundle.Version},{"adapter",adapter.Text},{"device_label",device.Label},{"usb_interface",device.Hub},{"usb_address",device.Address},{"started_utc",captureStarted.ToString("o")},{"stopped_utc",""},{"os",Environment.OSVersion.VersionString},{"capture_state","starting"},{"stop_reason",""},{"consent",Bundle.Consent},{"diagnostic_success","not inferred"},{"capture_sha256",""}};
    CaptureEngine.Save(Path.Combine(session,"session.json"),manifest);File.WriteAllText(Path.Combine(session,"actions.jsonl"),"",new UTF8Encoding(false));
    string cfg=Path.Combine(session,"worker.json");CaptureEngine.Save(cfg,new CaptureConfig{Executable=usb,Interface=device.Hub,Address=device.Address,ParentPid=Process.GetCurrentProcess().Id});
    worker=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--worker "+CaptureEngine.Quote(cfg)){UseShellExecute=false,CreateNoWindow=true});
    await Task.Delay(1500);if(worker.HasExited)throw new Exception("USBPcap could not start. Local session retained.");
    manifest["capture_state"]="recording";CaptureEngine.Save(Path.Combine(session,"session.json"),manifest);
   }catch(Exception e){if(worker!=null && worker.HasExited){worker.Dispose();worker=null;}Error(e);}finally{SetBusy(false);}}
  void AddNote(){if(worker==null || string.IsNullOrWhiteSpace(note.Text))return;if(notes>=200){MessageBox.Show("Maximum 200 notes per capture.");return;}File.AppendAllText(Path.Combine(session,"actions.jsonl"),new JavaScriptSerializer().Serialize(new{utc=DateTime.UtcNow.ToString("o"),action=note.Text})+"\n",new UTF8Encoding(false));notes++;note.Clear();}
  async Task Finish(){if(worker==null || finishing)return;finishing=true;SetBusy(true);try{
   AddNote();status.Text="Stopping and checking the recording…";File.WriteAllText(Path.Combine(session,"stop.request"),"");var p=worker;
   if(!await Task.Run(()=>p.WaitForExit(15000)))throw new Exception("Capture has not stopped. Recording remains local; no upload attempted.");
   worker.Dispose();worker=null;
   var result=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(session,"worker-result.json")));
   manifest["stopped_utc"]=DateTime.UtcNow.ToString("o");manifest["stop_reason"]=result["reason"];manifest["capture_state"]=(bool)result["graceful"]?"stopped_gracefully":"interrupted";
   CaptureEngine.Save(Path.Combine(session,"session.json"),manifest);
   if(!(bool)result["graceful"])throw new Exception("Capture was interrupted. Files retained locally; not uploaded.");
   var summary=await Task.Run(()=>CaptureEngine.Validate(Path.Combine(session,"usb.pcap"),(int)manifest["usb_address"]));
   manifest["capture_sha256"]=Bundle.Hash(Path.Combine(session,"usb.pcap"));CaptureEngine.Save(Path.Combine(session,"session.json"),manifest);
   status.Text="Saved "+summary.packets+" packets. Uploading privately to OpenSAAB…";
   string receipt=await Bundle.Upload(session);status.Text="Upload confirmed: "+receipt.Substring(0,22)+"…\nLocal files and full receipt saved. Thank you!";
  }catch(Exception e){Error(e);}finally{finishing=false;SetBusy(false);}}
  async Task Retry(){using(var dialog=new FolderBrowserDialog{Description="Select the completed OpenSAAB capture folder",SelectedPath=BaseDir()}){if(dialog.ShowDialog()!=DialogResult.OK)return;SetBusy(true);try{
   string dir=dialog.SelectedPath;var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(dir,"session.json")));
   if((string)data["consent"]!=Bundle.Consent || (string)data["capture_state"]!="stopped_gracefully")throw new Exception("This folder has no completed capture with upload consent.");
   CaptureEngine.Validate(Path.Combine(dir,"usb.pcap"),Convert.ToInt32(data["usb_address"]));session=dir;status.Text="Retrying private upload…";string receipt=await Bundle.Upload(dir);status.Text="Upload confirmed: "+receipt.Substring(0,22)+"…\nLocal files retained.";
  }catch(Exception e){Error(e);}finally{SetBusy(false);}}}
 }
}
