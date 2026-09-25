using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace OpenSaab.Collector {
 public sealed class SanitizeOptions {
  public string Vin="",ComputerName="",Serial="";
  public bool RemoveNotes=true,RemoveDescriptions=true;
 }
 public sealed class SanitizeReport {
  public int format=1,packets_scanned,packets_changed,known_vin_matches,vin_candidate_matches,computer_name_matches,serial_matches,notes_removed,description_fields_removed;
  public string created_utc=DateTime.UtcNow.ToString("o");
  public Dictionary<string,string> file_sha256=new Dictionary<string,string>();
  // Locations only. Never persist input identifiers or original/replacement text.
  public List<string> changes=new List<string>();
  public string Summary(){return
   "LOCAL SANITIZATION REPORT\r\n\r\n"+
   "Known VIN occurrences masked: "+known_vin_matches+"\r\n"+
   "Additional VIN-shaped strings masked: "+vin_candidate_matches+"\r\n"+
   "Computer-name occurrences masked: "+computer_name_matches+"\r\n"+
   "Supplied serial-number occurrences masked: "+serial_matches+"\r\n"+
   "Step notes removed: "+notes_removed+"\r\n"+
   "Description fields removed: "+description_fields_removed+"\r\n"+
   "USB packets scanned: "+packets_scanned+"; changed: "+packets_changed+"\r\n\r\n"+
   "Counts are occurrences, not distinct vehicles or people. Zero means no supported match was found; it does not certify that the capture is anonymous.\r\n\r\n"+
   "Scope: contiguous ASCII and UTF-16 text inside individual USB payloads, plus session metadata and retained notes. VIN-shaped matches are a heuristic, not verified VINs. Computer names and serials are matched only against the values supplied.\r\n\r\n"+
   "NOT COVERED: identifiers split across packets or diagnostic messages, other encodings, unknown serials/names, other personal information, or security seed/key exchanges. These may remain.\r\n\r\n"+
   "The original folder is unchanged. Packet framing and lengths are preserved, but edited payloads may no longer be valid diagnostic messages or suitable for replay. Review before sharing. This report stays local and is not in the upload ZIP.\r\n\r\n"+
   "Change locations (first 200, no original values):\r\n"+string.Join("\r\n",changes.ToArray());}
 }
 public sealed class SanitizeResult {public string Folder;public SanitizeReport Report;}
 public static class CaptureSanitizer {
  public const string ReportFile="sanitization-report.json",Marker="not inferred; sanitized-local-v1";
  static readonly Regex VinPattern=new Regex(@"(?<![A-Z0-9])[A-HJ-NPR-Z0-9]{17}(?![A-Z0-9])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
  static readonly string[] Files={"usb.pcap","session.json","actions.jsonl"};
  static readonly string[] Fields={"format","collector_version","adapter","device_label","usb_interface","usb_address","started_utc","stopped_utc","os","capture_state","stop_reason","consent","diagnostic_success","capture_sha256"};
  static void ValidateOptions(SanitizeOptions o){
   o.Vin=(o.Vin??"").Trim();o.ComputerName=(o.ComputerName??"").Trim();o.Serial=(o.Serial??"").Trim();
   if(o.Vin.Length>0 && !Regex.IsMatch(o.Vin,@"\A[A-HJ-NPR-Z0-9]{17}\z",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant))throw new Exception("Enter a 17-character VIN, or leave it blank.");
   foreach(string s in new[]{o.ComputerName,o.Serial}){
    if(s.Length>0 && (s.Length<3 || s.Length>128 || s.Trim('X','x').Length==0))throw new Exception("Computer names and serials must be 3–128 characters, or blank.");
    foreach(char c in s)if(c<32 || c>126)throw new Exception("This version supports printable ASCII names and serials only.");
   }
  }
  static void Count(SanitizeReport r,string kind,string location){
   switch(kind){case "known VIN":r.known_vin_matches++;break;case "VIN-shaped string":r.vin_candidate_matches++;break;case "computer name":r.computer_name_matches++;break;case "serial":r.serial_matches++;break;}
   if(r.changes.Count<200)r.changes.Add(location+": "+kind+" masked");
  }
  static string Exact(string text,string value,string kind,string location,SanitizeReport r){
   if(value.Length==0)return text;
   return Regex.Replace(text,Regex.Escape(value),delegate(Match m){Count(r,kind,location);return new string('X',m.Length);},RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
  }
  static string Redact(string text,SanitizeOptions o,SanitizeReport r,string location){
   text=Exact(text,o.Vin,"known VIN",location,r);
   text=VinPattern.Replace(text,delegate(Match m){bool digit=false,letter=false;foreach(char c in m.Value){digit|=c>='0'&&c<='9';letter|=char.IsLetter(c);}if(!digit||!letter)return m.Value;Count(r,"VIN-shaped string",location);return new string('X',m.Length);});
   text=Exact(text,o.ComputerName,"computer name",location,r);
   return Exact(text,o.Serial,"serial",location,r);
  }
  static void RedactPayload(byte[] data,int begin,int length,SanitizeOptions o,SanitizeReport r,string location){
   // Latin-1 is a reversible byte mapping; only ASCII patterns can match here.
   var encoding=Encoding.GetEncoding(28591);
   string before=encoding.GetString(data,begin,length),after=Redact(before,o,r,location+" ASCII");
   if(after!=before)Buffer.BlockCopy(encoding.GetBytes(after),0,data,begin,length);
   // UTF-16 strings can start at either byte alignment in a USB payload.
   foreach(bool big in new[]{false,true})foreach(int alignment in new[]{0,1}){
    int count=(length-alignment)/2;if(count<=0)continue;
    char[] chars=new char[count];for(int j=0;j<count;j++){int k=begin+alignment+j*2;chars[j]=(char)(big?(data[k]<<8)|data[k+1]:data[k]|(data[k+1]<<8));}
    before=new string(chars);after=Redact(before,o,r,location+(big?" UTF-16BE":" UTF-16LE"));
    // Write only changed code units. Never decode/re-encode unrelated bytes.
    for(int j=0;j<count;j++)if(before[j]!=after[j]){int k=begin+alignment+j*2;char c=after[j];data[k]=(byte)(big?c>>8:c);data[k+1]=(byte)(big?c:c>>8);}
   }
  }
  static int Matches(SanitizeReport r){return r.known_vin_matches+r.vin_candidate_matches+r.computer_name_matches+r.serial_matches;}
  public static SanitizeResult Create(string source,SanitizeOptions options){
   if(options==null)throw new ArgumentNullException("options");ValidateOptions(options);
   source=Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
   string output=source+"-sanitized-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
   Directory.CreateDirectory(output);
   try{
    foreach(string name in Files){long limit=name=="usb.pcap"?64L*1024*1024:name=="session.json"?16384:65536;if(new FileInfo(Path.Combine(source,name)).Length>limit)throw new Exception("Capture or metadata exceeds the supported size.");File.Copy(Path.Combine(source,name),Path.Combine(output,name));}
    var json=new JavaScriptSerializer();string meta=Path.Combine(output,"session.json"),pcap=Path.Combine(output,"usb.pcap"),notes=Path.Combine(output,"actions.jsonl");
    var data=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(meta));
    if(data==null || data.Count!=Fields.Length)throw new Exception("Unrecognized capture metadata. Nothing was sanitized.");
    foreach(string field in Fields)if(!data.ContainsKey(field))throw new Exception("Incomplete capture metadata.");
    if(Convert.ToInt32(data["format"])!=1 || (string)data["consent"]!=Bundle.Consent || (string)data["capture_state"]!="stopped_gracefully")throw new Exception("Choose a completed capture before sanitizing.");
    if(!Regex.IsMatch((string)data["usb_interface"],@"\A\\\\\.\\USBPcap[0-9]+\z"))throw new Exception("Invalid USB interface.");
    foreach(string field in new[]{"started_utc","stopped_utc"}){DateTime stamp;if(!DateTime.TryParse((string)data[field],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out stamp))throw new Exception("Invalid capture timestamp.");}
    int address=Convert.ToInt32(data["usb_address"]);if(address<1 || address>127)throw new Exception("Invalid USB address.");
    CaptureEngine.Validate(pcap,address);var report=new SanitizeReport();byte[] bytes=File.ReadAllBytes(pcap);
    int position=24;
    while(position<bytes.Length){
     int packetLength=(int)BitConverter.ToUInt32(bytes,position+8),usb=position+16,header=BitConverter.ToUInt16(bytes,usb),payload=(int)BitConverter.ToUInt32(bytes,usb+23);
     report.packets_scanned++;int before=Matches(report);
     RedactPayload(bytes,usb+header,payload,options,report,"usb.pcap packet "+report.packets_scanned);
     if(Matches(report)>before)report.packets_changed++;
     position=usb+packetLength;
    }
    File.WriteAllBytes(pcap,bytes);CaptureEngine.Validate(pcap,address);
    foreach(string field in Fields){
     if(field=="format"||field=="usb_address")continue;
     string value=data[field] as string;if(value==null || value.Length>1024)throw new Exception("Invalid metadata value.");
     if(options.RemoveDescriptions && (field=="adapter"||field=="device_label")){if(value.Length>0)report.description_fields_removed++;data[field]="";}
     else if(field=="adapter"||field=="device_label"||field=="os"||field=="stop_reason"||field=="collector_version")data[field]=Redact(value,options,report,"session.json "+field);
    }
    var kept=new List<string>();int lines=0;
    foreach(string line in File.ReadAllLines(notes)){
     if(string.IsNullOrWhiteSpace(line))continue;if(++lines>200)throw new Exception("Too many step notes.");
     var note=json.Deserialize<Dictionary<string,object>>(line);
     if(note==null || note.Count!=2 || !note.ContainsKey("utc") || !note.ContainsKey("action") || !(note["utc"] is string) || !(note["action"] is string) || ((string)note["utc"]).Length>512 || ((string)note["action"]).Length>512)throw new Exception("Invalid step note format.");
     if(options.RemoveNotes){report.notes_removed++;continue;}
     DateTime noteStamp;if(!DateTime.TryParse((string)note["utc"],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out noteStamp))throw new Exception("Invalid note timestamp.");
     note["action"]=Redact((string)note["action"],options,report,"actions.jsonl note "+lines);kept.Add(json.Serialize(note));
    }
    File.WriteAllText(notes,kept.Count==0?"":string.Join("\n",kept.ToArray())+"\n",new UTF8Encoding(false));
    data["diagnostic_success"]=Marker;data["capture_sha256"]=Bundle.Hash(pcap);CaptureEngine.Save(meta,data);
    foreach(string name in Files)report.file_sha256[name]=Bundle.Hash(Path.Combine(output,name));
    CaptureEngine.Save(Path.Combine(output,ReportFile),report);
    return new SanitizeResult{Folder=output,Report=report};
   }catch{Directory.Delete(output,true);throw;}
  }
  public static SanitizeReport VerifyReport(string dir){
   string path=Path.Combine(dir,ReportFile);
   var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(dir,"session.json")));
   bool marked=data.ContainsKey("diagnostic_success") && (string)data["diagnostic_success"]==Marker;
   if(!File.Exists(path)){if(marked)throw new Exception("Sanitization report is missing. Run Sanitize capture again before uploading this copy.");return null;}
   if(new FileInfo(path).Length>256*1024)throw new Exception("Invalid sanitization report.");
   var report=new JavaScriptSerializer().Deserialize<SanitizeReport>(File.ReadAllText(path));
   if(report==null || report.format!=1 || report.file_sha256==null || report.file_sha256.Count!=3)throw new Exception("Invalid sanitization report.");
   foreach(string name in Files)if(!report.file_sha256.ContainsKey(name) || report.file_sha256[name]!=Bundle.Hash(Path.Combine(dir,name)))throw new Exception("Files changed after sanitization. Run Sanitize capture again to get an up-to-date report before upload.");
   return report;
  }
 }
}
