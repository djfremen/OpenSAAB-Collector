using System;using System.IO;using System.Net;using System.Net.Http;using System.Diagnostics;using System.Drawing;using System.Windows.Forms;using System.Threading.Tasks;using System.Text;using System.Text.RegularExpressions;using System.Collections.Generic;using System.Web.Script.Serialization;using System.Security.Cryptography;using System.IO.Compression;
[assembly:System.Reflection.AssemblyVersion("0.5.6.0")]
[assembly:System.Reflection.AssemblyFileVersion("0.5.6.0")]
[assembly:System.Reflection.AssemblyProduct("OpenSAAB Collector")]
[assembly:System.Reflection.AssemblyDescription("Private USB adapter capture and upload")]
namespace OpenSaab.Collector {
 static class Program {
  [STAThread] static int Main(string[] args) {
   if(args.Length==2 && args[0]=="--worker")return CaptureEngine.Worker(args[1]);
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new CollectorForm());return 0;
  }
 }
 public static class Bundle {
  public const string Version="0.5.6";
  public const string Consent="collector-capture-v1";
  public const string Endpoint="https://www.opensaab.com/api/collector/captures";
  public static string Hash(string path){using(var f=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
  public static string Build(string dir){
   // Always package a fresh snapshot. A previously failed upload may contain
   // personal data that the contributor has since removed from the source files.
   string zip;
   string snapshot=Path.Combine(dir,".upload-"+Guid.NewGuid().ToString("N")),temp=snapshot+".zip";
   Directory.CreateDirectory(snapshot);
   try{
    foreach(string name in new[]{"usb.pcap","session.json","actions.jsonl"})File.Copy(Path.Combine(dir,name),Path.Combine(snapshot,name));
    if(File.Exists(Path.Combine(dir,CaptureSanitizer.ReportFile)))File.Copy(Path.Combine(dir,CaptureSanitizer.ReportFile),Path.Combine(snapshot,CaptureSanitizer.ReportFile));
    CaptureSanitizer.VerifyReport(snapshot);
    string meta=Path.Combine(snapshot,"session.json"),capture=Path.Combine(snapshot,"usb.pcap");
    var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(meta));
    zip=Path.Combine(dir,CaptureIdentity.Filename(data));
    if((string)data["consent"]!=Consent || (string)data["capture_state"]!="stopped_gracefully")throw new Exception("Select a completed capture. Interrupted recordings cannot be uploaded.");
    CaptureEngine.Validate(capture,Convert.ToInt32(data["usb_address"]));
    data["capture_sha256"]=Hash(capture);CaptureEngine.Save(meta,data);
    File.SetLastWriteTimeUtc(meta,File.GetLastWriteTimeUtc(Path.Combine(dir,"session.json")));
    using(var z=ZipFile.Open(temp,ZipArchiveMode.Create)){foreach(string name in new[]{"usb.pcap","session.json","actions.jsonl"})z.CreateEntryFromFile(Path.Combine(snapshot,name),name,CompressionLevel.Optimal);}
    if(new FileInfo(temp).Length>64L*1024*1024)throw new Exception("Capture exceeds upload limit; files are saved locally");
    if(File.Exists(zip))File.Replace(temp,zip,null);else File.Move(temp,zip);
    return zip;
   }finally{if(File.Exists(temp))File.Delete(temp);Directory.Delete(snapshot,true);}
  }
  public static async Task<string> Upload(string dir){
   string zip=Build(dir),digest=Hash(zip),filename=Path.GetFileName(zip);
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   using(var handler=new HttpClientHandler{AllowAutoRedirect=false})using(var client=new HttpClient(handler){Timeout=TimeSpan.FromMinutes(5)})using(var f=File.OpenRead(zip))using(var req=new HttpRequestMessage(HttpMethod.Post,Endpoint)){
    long bytes=f.Length;req.Content=new StreamContent(f);req.Content.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");req.Content.Headers.ContentLength=bytes;req.Content.Headers.ContentDisposition=new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment"){FileName=filename};
    req.Headers.Add("X-Content-SHA256",digest);req.Headers.Add("X-OpenSAAB-Consent",Consent);
    using(var response=await client.SendAsync(req)){
     if(!response.IsSuccessStatusCode)throw new Exception("Upload not confirmed (HTTP "+(int)response.StatusCode+"). Local files retained. Retry later.");
     string text=await response.Content.ReadAsStringAsync();var value=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(text);
     if(!value.ContainsKey("stored") || !(value["stored"] is bool) || !(bool)value["stored"] || !value.ContainsKey("sha256") || (string)value["sha256"]!=digest || !value.ContainsKey("receipt") || (string)value["receipt"]!="OSCAP-"+digest || Convert.ToInt64(value["bytes"])!=bytes)throw new Exception("Server receipt could not be verified. Local files retained.");
     if(filename!="capture.zip"){
      object summary;var fields=value.TryGetValue("summary",out summary)?summary as Dictionary<string,object>:null;
      if(fields==null || !fields.ContainsKey("capture_filename") || (string)fields["capture_filename"]!=filename)throw new Exception("Upload receipt did not confirm the adapter-labelled filename. Local files retained.");
     }
     File.WriteAllText(Path.Combine(dir,"upload-receipt.json"),text,new UTF8Encoding(false));return (string)value["receipt"];
    }
   }
  }
 }
 public partial class CollectorForm:Form {
  ComboBox devices=new ComboBox(),adapterModels=new ComboBox();TextBox adapter=new TextBox(),note=new TextBox();CheckBox consent=new CheckBox();Button setup=new Button(),refresh=new Button(),start=new Button(),stop=new Button(),add=new Button(),retry=new Button(),folder=new Button(),sanitize=new Button(),reportButton=new Button();Label status=new Label();
  string usb,session;Process worker;Dictionary<string,object> manifest;bool busy,finishing;int notes;DateTime captureStarted;Timer timer=new Timer();
  public CollectorForm(){BuildLayout();}
  void ShowSupport(){
   if(worker!=null || busy){MessageBox.Show("Finish the capture or upload before opening project support.","Support OpenSAAB");return;}
   if(MessageBox.Show("Support is voluntary and does not unlock features. Contributions help fund adapter testing and documentation.\n\nOpen the OpenSAAB Ko-fi page in your browser? No capture or vehicle data is added to the link.","Support OpenSAAB",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)==DialogResult.OK){
    try{Process.Start(new ProcessStartInfo("https://ko-fi.com/djfremen"){UseShellExecute=true});}catch(Exception){MessageBox.Show("No browser is available to open the donation page.","Support OpenSAAB");}
   }
  }
  string BaseDir(){string p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"OpenSAAB-Captures");Directory.CreateDirectory(p);return p;}
  void SetBusy(bool value){busy=value;setup.Enabled=!value && worker==null && usb==null;setup.Text=usb==null?"Set up USBPcap":"USBPcap installed";refresh.Enabled=retry.Enabled=sanitize.Enabled=reportButton.Enabled=!value && worker==null;start.Enabled=!value && worker==null && devices.SelectedItem is UsbDevice && CaptureIdentity.IsModel(adapterModels.SelectedItem as string);stop.Enabled=!value && worker!=null;devices.Enabled=adapterModels.Enabled=adapter.Enabled=consent.Enabled=!value && worker==null;add.Enabled=!value && worker!=null;}
  void Error(Exception e){status.Text=e.Message;MessageBox.Show(e.Message,"OpenSAAB Collector",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  string FindUsb(){foreach(string path in new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"USBPcap\USBPcapCMD.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),@"USBPcap\USBPcapCMD.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"Wireshark\extcap\USBPcapCMD.exe")})if(File.Exists(path))return path;return null;}
  async Task RefreshDevices(){SetBusy(true);try{
   usb=FindUsb();devices.Items.Clear();if(usb==null){status.Text="Click Set up USBPcap to download the official driver installer.";return;}
   var found=await Task.Run(delegate{
    var list=new List<UsbDevice>();string hubs=CaptureEngine.Query(usb,"--extcap-interfaces");
    foreach(Match h in Regex.Matches(hubs,@"(?m)^interface \{value=(\\\\\.\\USBPcap[0-9]+)\}")){
     string hub=h.Groups[1].Value;string data=CaptureEngine.Query(usb,"--extcap-interface "+CaptureEngine.Quote(hub)+" --extcap-config");
     list.AddRange(UsbDevice.ParseConfig(hub,data));
    }return list;
   });foreach(var d in found)devices.Items.Add(d);status.Text=found.Count>0?"Select the adapter itself, including when connected through a USB hub. Do not select the hub, mouse or keyboard.":"No capture devices found. Restart Windows if USBPcap was just installed.";
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
  async Task StartCapture(){if(!consent.Checked){MessageBox.Show("Please acknowledge the capture privacy notice before recording.",Text);return;}var device=devices.SelectedItem as UsbDevice;if(device==null){MessageBox.Show("Select your adapter first.",Text);return;}
   string model=adapterModels.SelectedItem as string;if(!CaptureIdentity.IsModel(model)){MessageBox.Show("Choose an adapter model first.",Text);return;}
   SetBusy(true);try{
    using(var service=new System.ServiceProcess.ServiceController("OpenSAABCollector")){try{if(service.Status==System.ServiceProcess.ServiceControllerStatus.Running)throw new Exception("The old Collector service is running. Stop it before recording; it has a separate uploader.");}catch(InvalidOperationException){}}
    if(Process.GetProcessesByName("USBPcapCMD").Length>0)throw new Exception("Another USBPcap process is running. Stop it first.");
    if(Process.GetProcessesByName("Tech2Win").Length>0 || Process.GetProcessesByName("emulator").Length>0)throw new Exception("Close Tech2Win before capture so initialization is included.");
    string fresh=await Task.Run(()=>CaptureEngine.Query(usb,"--extcap-interface "+CaptureEngine.Quote(device.Hub)+" --extcap-config"));
    if(!UsbDevice.ParseConfig(device.Hub,fresh).Exists(d=>d.Address==device.Address && d.Label==device.Label))throw new Exception("Device list changed. Refresh and select the adapter again.");
    captureStarted=DateTime.UtcNow;string captureId=CaptureIdentity.CreateId(model,captureStarted);session=Path.Combine(BaseDir(),captureId);Directory.CreateDirectory(session);notes=0;
    manifest=new Dictionary<string,object>{{"format",1},{"collector_version",Bundle.Version},{"adapter_model",model},{"capture_id",captureId},{"adapter",adapter.Text},{"device_label",device.Label},{"usb_interface",device.Hub},{"usb_address",device.Address},{"started_utc",captureStarted.ToString("o")},{"stopped_utc",""},{"os",Environment.OSVersion.VersionString},{"capture_state","starting"},{"stop_reason",""},{"consent",Bundle.Consent},{"diagnostic_success","not inferred"},{"capture_sha256",""}};
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
   status.Text="Saved "+summary.packets+" packets locally as "+(string)manifest["capture_id"]+".\nNothing uploaded. Review or sanitize before Upload.";
  }catch(Exception e){Error(e);}finally{finishing=false;SetBusy(false);}}
  async Task SanitizeSaved(){
   using(var folderDialog=new FolderBrowserDialog{Description="Choose a completed capture. A separate sanitized copy will be created.",SelectedPath=session ?? BaseDir()}){
    if(folderDialog.ShowDialog()!=DialogResult.OK)return;
    using(var options=new SanitizeDialog()){
     if(options.ShowDialog(this)!=DialogResult.OK)return;
     var selected=options.Options;SetBusy(true);try{
      status.Text="Sanitizing a local copy… Nothing is being uploaded.";
      var result=await Task.Run(()=>CaptureSanitizer.Create(folderDialog.SelectedPath,selected));session=result.Folder;
      status.Text="Sanitized copy selected. Original retained. Nothing uploaded.\nReview the report, then choose Upload if ready.";
      SanitizeReportDialog.Show(this,result.Report,false);
     }catch(Exception e){Error(e);}finally{SetBusy(false);}
    }
   }
  }
  void ReviewSanitization(){try{
   if(session==null){MessageBox.Show("Choose Sanitize capture to create a copy and report first.",Text);return;}
   var report=CaptureSanitizer.VerifyReport(session);
   if(report==null){MessageBox.Show("This folder has no sanitization report. Choose Sanitize capture first.",Text);return;}
   SanitizeReportDialog.Show(this,report,false);
  }catch(Exception e){Error(e);}}
  async Task UploadSaved(){using(var dialog=new FolderBrowserDialog{Description="Select the reviewed capture folder to upload",SelectedPath=session ?? BaseDir()}){if(dialog.ShowDialog()!=DialogResult.OK)return;
   string dir=dialog.SelectedPath;
   try{
    var saved=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(dir,"session.json")));
    string filename=CaptureIdentity.Filename(saved);
    var report=CaptureSanitizer.VerifyReport(dir);
    if(report!=null){if(SanitizeReportDialog.Show(this,report,true)!=DialogResult.OK)return;}
    else if(MessageBox.Show("This folder has no sanitization report. Upload its current files privately to OpenSAAB?\n\n"+dir+"\n\nPackage: "+filename+"\n\nFiles: usb.pcap, session.json and actions.jsonl. They may contain VINs, serials and security data. Choose Cancel to use Sanitize capture or review the files first. Nothing is sent until you confirm.","Confirm upload without sanitization report",MessageBoxButtons.OKCancel,MessageBoxIcon.Information,MessageBoxDefaultButton.Button2)!=DialogResult.OK)return;
    SetBusy(true);session=dir;status.Text="Uploading "+filename+" privately…";string receipt=await Bundle.Upload(dir);status.Text="Uploaded "+filename+".\nReceipt: "+receipt.Substring(0,22)+"… Full receipt saved locally.";
   }catch(Exception e){Error(e);}finally{SetBusy(false);}}}
 }
}
