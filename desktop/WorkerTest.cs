using System;using System.IO;using System.Diagnostics;using System.Threading;using OpenSaab.Collector;
class WorkerTest {
 static int Main(string[] a) {
  if(a[0]=="--worker")return CaptureEngine.Worker(a[1]);
  Directory.CreateDirectory(a[3]);string cfg=Path.Combine(a[3],"worker.json");
  CaptureEngine.Save(cfg,new CaptureConfig{Executable=a[0],Interface=a[1],Address=int.Parse(a[2]),ParentPid=Process.GetCurrentProcess().Id});
  using(var p=Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--worker "+CaptureEngine.Quote(cfg)){UseShellExecute=false,CreateNoWindow=true})) {
   Thread.Sleep(int.Parse(a[4])*1000);File.WriteAllText(Path.Combine(a[3],"stop.request"),"");
   if(!p.WaitForExit(15000)){Console.WriteLine("WORKER DID NOT STOP");return 5;}
   Console.WriteLine(File.ReadAllText(Path.Combine(a[3],"worker-result.json")));Console.WriteLine(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(CaptureEngine.Validate(Path.Combine(a[3],"usb.pcap"),int.Parse(a[2]))));return p.ExitCode;
  }
 }
}
