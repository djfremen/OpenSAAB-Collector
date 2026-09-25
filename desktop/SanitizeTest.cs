// No network, USB access or personal fixtures. Synthetic payloads only.
using System;using System.IO;using System.Text;using System.Collections.Generic;using System.IO.Compression;using OpenSaab.Collector;
class SanitizeTest {
 const string Vin="YS3TEST1234567890",Other="YS3DEMA1234567891",Machine="TEST-PC-77",Serial="FAKE-SERIAL-88";
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS: "+name);}
 static void Capture(string path,List<byte[]> payloads){using(var w=new BinaryWriter(File.Create(path))){
  w.Write(0xa1b2c3d4u);w.Write((ushort)2);w.Write((ushort)4);w.Write(0u);w.Write(0u);w.Write(65535u);w.Write(249u);
  foreach(var payload in payloads){w.Write(1u);w.Write(2u);w.Write((uint)(27+payload.Length));w.Write((uint)(27+payload.Length));w.Write((ushort)27);w.Write(0UL);w.Write(0u);w.Write((ushort)9);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)4);w.Write((byte)0x81);w.Write((byte)3);w.Write((uint)payload.Length);w.Write(payload);}
 }}
 static Dictionary<string,object> Metadata(string pcap){return new Dictionary<string,object>{{"format",1},{"collector_version",Bundle.Version},{"adapter","user-entered private description"},{"device_label",Machine},{"usb_interface",@"\\.\USBPcap1"},{"usb_address",4},{"started_utc","2026-09-25T00:00:00Z"},{"stopped_utc","2026-09-25T00:01:00Z"},{"os","test"},{"capture_state","stopped_gracefully"},{"stop_reason","user"},{"consent",Bundle.Consent},{"diagnostic_success","not inferred"},{"capture_sha256",Bundle.Hash(pcap)}};}
 static bool Contains(byte[] hay,byte[] needle){for(int i=0;i<=hay.Length-needle.Length;i++){bool ok=true;for(int j=0;j<needle.Length;j++)if(hay[i+j]!=needle[j]){ok=false;break;}if(ok)return true;}return false;}
 static byte[] Odd(byte[] b){byte[] o=new byte[b.Length+1];o[0]=0xff;Buffer.BlockCopy(b,0,o,1,b.Length);return o;}
 static bool Reject(Action action){try{action();return false;}catch{return true;}}
 static int Main(){string root=Path.Combine(Path.GetTempPath(),"sanitize-test-"+Guid.NewGuid().ToString("N")),source=Path.Combine(root,"source");Directory.CreateDirectory(source);try{
  string pcap=Path.Combine(source,"usb.pcap"),meta=Path.Combine(source,"session.json"),notes=Path.Combine(source,"actions.jsonl");
  var payloads=new List<byte[]>{Encoding.ASCII.GetBytes(Vin+"|"+Vin+"|"+Other+"|"+Machine+"|"+Serial),Odd(Encoding.Unicode.GetBytes(Vin+"|"+Machine+"|"+Serial)),Odd(Encoding.BigEndianUnicode.GetBytes(Vin+"|"+Machine+"|"+Serial)),Encoding.ASCII.GetBytes(Vin.Substring(0,9)),Encoding.ASCII.GetBytes(Vin.Substring(9)),new byte[]{0,0xff,0x27,0x01,0x67,0x01,0xa1,0xb2,0xc3,0xd4}};
  Capture(pcap,payloads);CaptureEngine.Save(meta,Metadata(pcap));File.WriteAllText(notes,"{\"utc\":\"2026-09-25T00:00:00Z\",\"action\":\"private note\"}\n");
  var original=new Dictionary<string,string>();foreach(string f in new[]{"usb.pcap","session.json","actions.jsonl"})original[f]=Bundle.Hash(Path.Combine(source,f));
  var options=new SanitizeOptions{Vin=Vin,ComputerName=Machine,Serial=Serial};var result=CaptureSanitizer.Create(source,options);var r=result.Report;
  Check(r.known_vin_matches==4 && r.vin_candidate_matches==1,"Counts repeated known VIN and additional VIN-shaped strings exactly");
  Check(r.computer_name_matches==3 && r.serial_matches==3,"Counts ASCII and both UTF-16 byte orders at odd alignment");
  Check(r.notes_removed==1 && r.description_fields_removed==2,"Removes notes and description fields by default");
  Check(r.packets_scanned==6 && r.packets_changed==3,"Counts changed packets separately from scanned packets");
  foreach(var pair in original)Check(Bundle.Hash(Path.Combine(source,pair.Key))==pair.Value,"Original "+pair.Key+" is unchanged");
  byte[] old=File.ReadAllBytes(pcap),clean=File.ReadAllBytes(Path.Combine(result.Folder,"usb.pcap"));Check(old.Length==clean.Length,"Capture length unchanged");
  int position=24;bool headers=true,untouched=true;for(int i=0;i<24;i++)headers&=old[i]==clean[i];int packet=0;
  while(position<old.Length){int n=(int)BitConverter.ToUInt32(old,position+8);for(int j=0;j<43;j++)headers&=old[position+j]==clean[position+j];if(++packet>=4)for(int j=43;j<16+n;j++)untouched&=old[position+j]==clean[position+j];position+=16+n;}
  Check(headers,"All PCAP and USB headers preserved byte-for-byte");Check(untouched,"Split VIN fragments and security-like bytes are unchanged and explicitly out of scope");
  foreach(Encoding e in new[]{Encoding.ASCII,Encoding.Unicode,Encoding.BigEndianUnicode})Check(!Contains(clean,e.GetBytes(Vin))&&!Contains(clean,e.GetBytes(Machine))&&!Contains(clean,e.GetBytes(Serial)),"Known contiguous identifiers absent in "+e.WebName);
  string report=File.ReadAllText(Path.Combine(result.Folder,CaptureSanitizer.ReportFile));Check(!report.Contains(Vin)&&!report.Contains(Machine)&&!report.Contains(Serial)&&!report.Contains(source),"Report contains counts and locations without input identifiers or paths");
  Check(r.Summary().Contains("NOT COVERED")&&r.Summary().Contains("Zero means"),"Report states incomplete coverage even when counts are zero");
  Check(CaptureSanitizer.VerifyReport(result.Folder)!=null,"Report hashes match sanitized files");
  using(var zip=ZipFile.OpenRead(Bundle.Build(result.Folder)))Check(zip.Entries.Count==3&&zip.GetEntry(CaptureSanitizer.ReportFile)==null,"Sanitized upload bundle keeps local report out of the ZIP");
  File.AppendAllText(Path.Combine(result.Folder,"actions.jsonl")," ");Check(Reject(()=>Bundle.Build(result.Folder)),"Post-review edits block upload packaging despite existing ZIP");
  var missing=CaptureSanitizer.Create(source,options);File.Delete(Path.Combine(missing.Folder,CaptureSanitizer.ReportFile));Check(Reject(()=>Bundle.Build(missing.Folder)),"Missing report on a sanitized capture blocks packaging");
  File.WriteAllText(notes,"{\"utc\":\"2026-09-25T00:00:00Z\",\"action\":\""+Vin+" "+Machine+"\"}\n");options.RemoveNotes=false;options.RemoveDescriptions=false;
  var keep=CaptureSanitizer.Create(source,options);Check(keep.Report.known_vin_matches==5&&keep.Report.computer_name_matches==5,"Retained notes and descriptions have matching identifiers masked");
  Capture(pcap,new List<byte[]>{Encoding.ASCII.GetBytes("ordinary payload")});CaptureEngine.Save(meta,Metadata(pcap));File.WriteAllText(notes,"");
  var zero=CaptureSanitizer.Create(source,new SanitizeOptions());Check(zero.Report.known_vin_matches==0&&zero.Report.vin_candidate_matches==0&&zero.Report.Summary().Contains("does not certify"),"Zero-hit report does not claim anonymity");
  int directories=Directory.GetDirectories(root).Length;File.WriteAllText(pcap,"broken");Check(Reject(()=>CaptureSanitizer.Create(source,options))&&Directory.GetDirectories(root).Length==directories,"Malformed input leaves no misleading sanitized folder");
  Check(Reject(()=>CaptureSanitizer.Create(source,new SanitizeOptions{Vin="short"})),"Invalid known VIN rejected");
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{Directory.Delete(root,true);}}
}
