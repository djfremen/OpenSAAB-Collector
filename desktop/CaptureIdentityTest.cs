// Pure provenance tests; no USB, network, WinForms or real contributor data.
using System;
using System.Collections.Generic;
using OpenSaab.Collector;
class CaptureIdentityTest {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS: "+name);}
 static bool Reject(Action action){try{action();return false;}catch{return true;}}
 static Dictionary<string,object> Session(string model){return new Dictionary<string,object>{{"adapter_model",model},{"capture_id","OpenSAAB_"+model+"_20261004_073025Z_abcdef01"},{"started_utc","2026-10-04T07:30:25.1234567Z"}};}
 static int Main(){try{
  Check(CaptureIdentity.Models.Length==4,"Only the four requested models are offered");
  foreach(string model in CaptureIdentity.Models){
   var data=Session(model);string filename=CaptureIdentity.Filename(data);
   Check(filename=="OpenSAAB_"+model+"_20261004_073025Z_abcdef01.zip",model+" is burned into the UTC package filename");
   string id=CaptureIdentity.CreateId(model,new DateTime(2026,10,4,7,30,25,DateTimeKind.Utc));data["capture_id"]=id;
   Check(CaptureIdentity.Filename(data)==id+".zip" && CaptureIdentity.CreateId(model,new DateTime(2026,10,4,7,30,25,DateTimeKind.Utc))!=id,"Independent same-second "+model+" sessions have distinct names");
  }
  Check(CaptureIdentity.Filename(new Dictionary<string,object>{{"adapter","old free text"}})=="capture.zip","Legacy captures retain their original unnamed package");
  Check(Reject(()=>CaptureIdentity.RequireModel(null)) && Reject(()=>CaptureIdentity.RequireModel("Select adapter model (required)")) && Reject(()=>CaptureIdentity.RequireModel("Other")),"Missing and unknown selections rejected");
  var session=Session("MDI");session.Remove("capture_id");Check(Reject(()=>CaptureIdentity.Filename(session)),"Partial identity rejected");
  session=Session("MDI");session.Remove("adapter_model");Check(Reject(()=>CaptureIdentity.Filename(session)),"Orphan filename identity rejected");
  foreach(string bad in new[]{"../../MDI","OpenSAAB_MDI_20261004_073025Z_abcdef01.zip","OpenSAAB_Nano_20261004_073025Z_abcdef01","OpenSAAB_MDI_20261005_073025Z_abcdef01","OpenSAAB_MDI_20261004_073025Z_ABCDEF01"}){
   session=Session("MDI");session["capture_id"]=bad;Check(Reject(()=>CaptureIdentity.Filename(session)),"Unsafe, renamed or inconsistent identity rejected");
  }
  session=Session("MDI");session["started_utc"]="2026-10-04T00:30:25-07:00";Check(Reject(()=>CaptureIdentity.Filename(session)),"New identities require UTC metadata");
  session=Session("MDI");session["started_utc"]="2026-10-04T07:30:25";Check(Reject(()=>CaptureIdentity.Filename(session)),"Missing timezone rejected independently of the host timezone");
  Check(Reject(()=>CaptureIdentity.CreateId("MDI",new DateTime(2026,10,4))),"Ambiguous local start time rejected");
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
