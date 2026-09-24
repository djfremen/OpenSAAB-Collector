using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Web.Script.Serialization;

namespace OpenSaab.Collector {
 public static class CaptureEngine {
  [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr CreateFile(string n,uint a,uint s,IntPtr sa,uint c,uint f,IntPtr t);
  [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetHandleInformation(IntPtr h,uint m,uint f);
  [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcess(string app,StringBuilder cmd,IntPtr pa,IntPtr ta,bool inherit,uint flags,IntPtr env,string dir,ref STARTUPINFO si,out PROCESS_INFORMATION pi);
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct STARTUPINFO {
   public int cb;public string reserved,desktop,title;public uint x,y,w,h,cx,cy,fill,flags;public short show,reserved2;public IntPtr extra,input,output,error;
  }
  [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION {public IntPtr process,thread;public int pid,tid;}
  [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h,uint timeout);
  [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr h,out uint exit);
  [DllImport("kernel32.dll")] static extern bool TerminateProcess(IntPtr h,uint exit);
  public static string Quote(string s) { if(s.IndexOf('"')>=0 || s.IndexOf('\n')>=0 || s.IndexOf('\r')>=0) throw new Exception("Invalid command argument"); return "\""+s+"\""; }
  public static void Save(string path,object value) { File.WriteAllText(path,new JavaScriptSerializer().Serialize(value),new UTF8Encoding(false)); }
  public static string Query(string exe,string args) {
   using(var p=new Process()) { p.StartInfo=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};p.Start();
    var output=p.StandardOutput.ReadToEndAsync();var err=p.StandardError.ReadToEndAsync();
    if(!p.WaitForExit(15000)){p.Kill();p.WaitForExit();throw new Exception("USBPcap query timed out");}
    if(p.ExitCode!=0)throw new Exception("USBPcap query failed");return output.Result;
   }
  }
  public static int Worker(string configPath) {
   var cfg=new JavaScriptSerializer().Deserialize<CaptureConfig>(File.ReadAllText(configPath));
   if(!System.Text.RegularExpressions.Regex.IsMatch(cfg.Interface,@"^\\\\\.\\USBPcap[0-9]+$") || cfg.Address<1 || cfg.Address>127) return 2;
   string dir=Path.GetDirectoryName(configPath);IntPtr child=IntPtr.Zero;
   System.IO.Pipes.NamedPipeServerStream pipe=null;
   IntPtr writer=IntPtr.Zero;
   System.Threading.Tasks.Task copy=null;FileStream output=null,errors=null;
   try {
    string name="OpenSAAB-Capture-"+Guid.NewGuid().ToString("N");
    pipe=new System.IO.Pipes.NamedPipeServerStream(name,System.IO.Pipes.PipeDirection.InOut,1,System.IO.Pipes.PipeTransmissionMode.Byte,System.IO.Pipes.PipeOptions.Asynchronous);
    var connect=pipe.BeginWaitForConnection(null,null);
    // Keep the inherited writer native: a managed async handle would share the CLR IO completion port with USBPcap.
    writer=CreateFile(@"\\.\pipe\"+name,0xC0000000,0,IntPtr.Zero,3,0x40000000,IntPtr.Zero);
    if(writer==new IntPtr(-1))throw new Win32Exception();pipe.EndWaitForConnection(connect);
    IntPtr wh=writer;if(!SetHandleInformation(wh,1,1))throw new Win32Exception();
    output=new FileStream(Path.Combine(dir,"usb.pcap"),FileMode.CreateNew,FileAccess.Write,FileShare.Read);
    errors=new FileStream(Path.Combine(dir,"capture.stderr.txt"),FileMode.CreateNew,FileAccess.Write,FileShare.Read);
    IntPtr eh=errors.SafeFileHandle.DangerousGetHandle();SetHandleInformation(eh,1,1);
    var si=new STARTUPINFO{cb=Marshal.SizeOf(typeof(STARTUPINFO)),flags=0x100,input=IntPtr.Zero,output=wh,error=eh};PROCESS_INFORMATION pi;
    var cmd=new StringBuilder(Quote(cfg.Executable)+" -d "+Quote(cfg.Interface)+" --devices "+cfg.Address+" --inject-descriptors -s 65535 -o -");
    if(!CreateProcess(cfg.Executable,cmd,IntPtr.Zero,IntPtr.Zero,true,0x08000000,IntPtr.Zero,null,ref si,out pi))throw new Win32Exception();
    child=pi.process;CloseHandle(pi.thread);CloseHandle(writer);writer=IntPtr.Zero;
    copy=pipe.CopyToAsync(output);Save(Path.Combine(dir,"worker-started.json"),new {pid=pi.pid});
    string reason="requested";DateTime started=DateTime.UtcNow;
    while(WaitForSingleObject(child,150)==258) {
     if(File.Exists(Path.Combine(dir,"stop.request")))break;
     if((DateTime.UtcNow-started).TotalMinutes>=15){reason="time_limit";break;}
     if(output.Position>=60L*1024*1024){reason="size_limit";break;}
     if(copy.IsCompleted){reason="pipe_closed";break;}
     try { using(var parent=Process.GetProcessById(cfg.ParentPid)){if(parent.HasExited){reason="parent_closed";break;}} } catch {reason="parent_closed";break;}
    }
    bool requested=WaitForSingleObject(child,0)==258;
    // USBPcap monitors a duplex output pipe for disconnect, including while idle.
    // Closing it is the supported worker termination path, without killing capture.
    pipe.Dispose();pipe=null;
    try{if(!copy.Wait(5000))throw new Exception("Recording pipe did not close");}catch(AggregateException){}output.Flush();
    bool clean=WaitForSingleObject(child,10000)==0;
    if(!clean){TerminateProcess(child,99);WaitForSingleObject(child,5000);reason="forced_stop";}
    uint exit;GetExitCodeProcess(child,out exit);
    Save(Path.Combine(dir,"worker-result.json"),new {exit_code=exit,graceful=clean && requested && exit==0,reason=reason,stopped_utc=DateTime.UtcNow.ToString("o")});
    return clean && requested && exit==0 ? 0:3;
   } catch(Exception e) {
    if(child!=IntPtr.Zero && WaitForSingleObject(child,0)==258){TerminateProcess(child,99);WaitForSingleObject(child,5000);}
    Save(Path.Combine(dir,"worker-result.json"),new {exit_code=-1,graceful=false,reason="capture_error",error=e.Message});return 4;
   } finally {if(pipe!=null)pipe.Dispose();if(writer!=IntPtr.Zero && writer!=new IntPtr(-1))CloseHandle(writer);if(copy!=null){try{copy.Wait(5000);}catch{}}if(output!=null)output.Dispose();if(errors!=null)errors.Dispose();if(child!=IntPtr.Zero)CloseHandle(child);}
  }
  public static CaptureSummary Validate(string path,int address) {
   using(var f=File.OpenRead(path))using(var r=new BinaryReader(f)) {
    if(f.Length<24 || f.Length>64L*1024*1024 || r.ReadUInt32()!=0xa1b2c3d4 || r.ReadUInt16()!=2 || r.ReadUInt16()!=4)throw new Exception("Invalid capture header or size");
    f.Position=20;if(r.ReadUInt32()!=249)throw new Exception("Not a USBPcap capture");int count=0,truncated=0,transfers=0;
    while(f.Position<f.Length){if(f.Length-f.Position<16)throw new Exception("Incomplete packet header");r.ReadUInt32();r.ReadUInt32();uint n=r.ReadUInt32(),original=r.ReadUInt32();
     if(n<27 || n>65535 || n>original || n>f.Length-f.Position)throw new Exception("Invalid packet length");long next=f.Position+n;
     ushort header=r.ReadUInt16();if(header<27 || header>n)throw new Exception("Invalid USB header");f.Position+=17;int dev=r.ReadUInt16();r.ReadByte();byte transfer=r.ReadByte();uint data=r.ReadUInt32();
     if(data>n-header)throw new Exception("Truncated USB payload");if(dev!=address)throw new Exception("Capture includes an unexpected USB address");
     if(transfer==3 && data>0)transfers++;if(n<original)truncated++;count++;f.Position=next;
    }
    if(count==0 || truncated>0)throw new Exception("Empty or truncated capture; local files retained");
    return new CaptureSummary{packets=count,bytes=f.Length,bulk_payload_packets=transfers,truncated_packets=truncated};
   }
  }
 }
 public class CaptureConfig {public string Executable; public string Interface;public int Address;public int ParentPid;}
 public class CaptureSummary {public int packets;public long bytes;public int truncated_packets;public int bulk_payload_packets;}
}
