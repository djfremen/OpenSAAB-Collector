// Synthetic captures only. No network requests or USB hardware access.
using System;using System.IO;using System.IO.Compression;using System.Collections.Generic;using System.Web.Script.Serialization;using OpenSaab.Collector;
class BundleTest {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS: "+name);}
 static string Read(ZipArchive z,string name){using(var r=new StreamReader(z.GetEntry(name).Open()))return r.ReadToEnd();}
 static void Capture(string path,byte payload){using(var w=new BinaryWriter(File.Create(path))){
  w.Write(0xa1b2c3d4u);w.Write((ushort)2);w.Write((ushort)4);w.Write(0u);w.Write(0u);w.Write(65535u);w.Write(249u);
  w.Write(0u);w.Write(0u);w.Write(28u);w.Write(28u);
  w.Write((ushort)27);w.Write(0UL);w.Write(0u);w.Write((ushort)9);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)4);w.Write((byte)0x81);w.Write((byte)3);w.Write(1u);w.Write(payload);
 }}
 static int Main(){string dir=Path.Combine(Path.GetTempPath(),"collector-bundle-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);try{
  string capture=Path.Combine(dir,"usb.pcap"),meta=Path.Combine(dir,"session.json"),notes=Path.Combine(dir,"actions.jsonl");Capture(capture,1);
  var data=new Dictionary<string,object>{{"consent",Bundle.Consent},{"capture_state","stopped_gracefully"},{"usb_address",4},{"capture_sha256",Bundle.Hash(capture)},{"adapter","synthetic description"}};
  CaptureEngine.Save(meta,data);File.WriteAllText(notes,"synthetic original note");File.WriteAllText(Path.Combine(dir,"worker.json"),"local only");
  string zip=Bundle.Build(dir),first=Bundle.Hash(zip);
  Capture(capture,2);data["adapter"]="";CaptureEngine.Save(meta,data);string originalMetadata=File.ReadAllText(meta);File.WriteAllText(notes,"");
  Bundle.Build(dir);string reviewed=Bundle.Hash(zip);Check(reviewed!=first,"Reviewed edits replace an existing ZIP");
  using(var z=ZipFile.OpenRead(zip)){
   Check(z.Entries.Count==3 && z.GetEntry("worker.json")==null,"Only the three documented files are packaged");
   Check(Read(z,"actions.jsonl")=="","Removed notes stay removed");
   var uploaded=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(Read(z,"session.json"));
   Check((string)uploaded["adapter"]=="","Removed metadata stays removed");
   Check((string)uploaded["capture_sha256"]==Bundle.Hash(capture),"Edited PCAP checksum is refreshed");
   using(var m=new MemoryStream()){using(var f=z.GetEntry("usb.pcap").Open())f.CopyTo(m);Check(m.ToArray()[m.Length-1]==2,"Edited packet bytes are uploaded");}
  }
  Check(File.ReadAllText(meta)==originalMetadata,"Review source metadata is not overwritten");
  Bundle.Build(dir);Check(Bundle.Hash(zip)==reviewed,"Unchanged retry creates byte-identical bundle");
  File.WriteAllText(capture,"invalid edited capture");bool rejected=false;try{Bundle.Build(dir);}catch{rejected=true;}
  Check(rejected,"Invalid edited PCAP fails instead of reusing old ZIP");
  Capture(capture,3);data["capture_state"]="interrupted";CaptureEngine.Save(meta,data);rejected=false;try{Bundle.Build(dir);}catch{rejected=true;}Check(rejected,"Interrupted capture is rejected");
  data["capture_state"]="stopped_gracefully";CaptureEngine.Save(meta,data);File.Delete(notes);rejected=false;try{Bundle.Build(dir);}catch{rejected=true;}Check(rejected,"Missing reviewed file fails instead of reusing cached ZIP");
  Check(Directory.GetDirectories(dir,".upload-*").Length==0 && Directory.GetFiles(dir,".upload-*").Length==0,"Temporary snapshots are removed on success and failure");
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{Directory.Delete(dir,true);}}
}
