// Developer CLI harness; not included in the downloadable application.
using System;using System.IO;using System.Collections.Generic;using System.Web.Script.Serialization;using OpenSaab.Collector;
class UploadTest {
 static int Main(string[] a){try{
  if(a.Length!=2 || (a[0]!="prepare" && a[0]!="upload"))return 2;
  string dir=a[1];
  if(a[0]=="prepare"){
   var result=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(dir,"worker-result.json")));
   if(!(bool)result["graceful"])throw new Exception("Capture did not stop gracefully");
   var config=new JavaScriptSerializer().Deserialize<CaptureConfig>(File.ReadAllText(Path.Combine(dir,"worker.json")));
   CaptureEngine.Validate(Path.Combine(dir,"usb.pcap"),config.Address);
   CaptureEngine.Save(Path.Combine(dir,"session.json"),new Dictionary<string,object>{{"format",1},{"collector_version",Bundle.Version},{"adapter","Chipsoft Pro, read-only bench DTC validation"},{"device_label","USB Serial Device (COM7)"},{"usb_interface",config.Interface},{"usb_address",config.Address},{"started_utc",File.GetCreationTimeUtc(Path.Combine(dir,"usb.pcap")).ToString("o")},{"stopped_utc",result["stopped_utc"]},{"os",Environment.OSVersion.VersionString},{"capture_state","stopped_gracefully"},{"stop_reason",result["reason"]},{"consent",Bundle.Consent},{"diagnostic_success","separate bench probe reports six DTC records"},{"capture_sha256",Bundle.Hash(Path.Combine(dir,"usb.pcap"))}});
   File.WriteAllText(Path.Combine(dir,"actions.jsonl"),new JavaScriptSerializer().Serialize(new{utc=File.GetCreationTimeUtc(Path.Combine(dir,"usb.pcap")).ToString("o"),action="Capture starts; read-only bench DTC probe runs before capture stops"})+"\n");
   Console.WriteLine(Bundle.Build(dir));
  }else Console.WriteLine(Bundle.Upload(dir).GetAwaiter().GetResult());return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
