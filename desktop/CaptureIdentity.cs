using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenSaab.Collector {
 // Contributor-selected family, never a claim of detected hardware or qualification.
 public static class CaptureIdentity {
  public static readonly string[] Models={"Chipsoft","MDI","Mongoose","Nano"};
  public static bool IsModel(string value){return Array.IndexOf(Models,value)>=0;}
  public static string RequireModel(string value){
   if(!IsModel(value))throw new Exception("Choose an adapter model: Chipsoft, MDI, Mongoose or Nano.");
   return value;
  }
  public static string CreateId(string model,DateTime startedUtc){
   RequireModel(model);
   if(startedUtc.Kind!=DateTimeKind.Utc)throw new ArgumentException("Capture start must be UTC.","startedUtc");
   return "OpenSAAB_"+model+"_"+startedUtc.ToString("yyyyMMdd_HHmmss'Z'",CultureInfo.InvariantCulture)+"_"+Guid.NewGuid().ToString("N").Substring(0,8);
  }
  public static string Filename(Dictionary<string,object> session){
   if(session==null)throw new Exception("Missing capture metadata.");
   bool model=session.ContainsKey("adapter_model"),id=session.ContainsKey("capture_id");
   if(!model && !id)return "capture.zip"; // Legacy captures are never relabelled by the current UI.
   if(!model || !id)throw new Exception("Incomplete adapter identity in capture metadata.");
   string selected=session["adapter_model"] as string,captureId=session["capture_id"] as string;
   RequireModel(selected);
   if(captureId==null || !Regex.IsMatch(captureId,@"\AOpenSAAB_"+selected+@"_[0-9]{8}_[0-9]{6}Z_[0-9a-f]{8}\z",RegexOptions.CultureInvariant))throw new Exception("Invalid capture filename identity.");
   object value;DateTimeOffset stamp;
   if(!session.TryGetValue("started_utc",out value) || !(value is string) || !Regex.IsMatch((string)value,@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?(Z|\+00:00)\z",RegexOptions.CultureInvariant) || !DateTimeOffset.TryParse((string)value,CultureInfo.InvariantCulture,DateTimeStyles.None,out stamp) || stamp.Offset!=TimeSpan.Zero)throw new Exception("Invalid UTC capture start.");
   string prefix="OpenSAAB_"+selected+"_"+stamp.UtcDateTime.ToString("yyyyMMdd_HHmmss'Z'",CultureInfo.InvariantCulture)+"_";
   if(!captureId.StartsWith(prefix,StringComparison.Ordinal))throw new Exception("Capture identity does not match its model and start time.");
   return captureId+".zip";
  }
 }
}
