using System;using System.Drawing;using System.Windows.Forms;
namespace OpenSaab.Collector {
 public sealed class SanitizeDialog:Form {
  TextBox vin=new TextBox(),computer=new TextBox(),serial=new TextBox();
  CheckBox notes=new CheckBox(),descriptions=new CheckBox();
  public SanitizeOptions Options{get{return new SanitizeOptions{Vin=vin.Text,ComputerName=computer.Text,Serial=serial.Text,RemoveNotes=notes.Checked,RemoveDescriptions=descriptions.Checked};}}
  public SanitizeDialog(){
   Text="Sanitize a local copy";Font=new Font("Segoe UI",11);AutoScaleMode=AutoScaleMode.Font;ClientSize=new Size(660,520);MinimumSize=new Size(360,360);MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
   var content=CollectorTouchUi.ScrollContent(this,"sanitizeViewport");
   CollectorTouchUi.Add(content,CollectorTouchUi.Label("Creates a separate copy. The original is kept and nothing is uploaded. Detects complete VIN-shaped strings; known values improve matching.",11,false));
   AddField(content,"Known VIN (optional)",vin,"knownVin");
   AddField(content,"Computer name to mask",computer,"computerName");computer.Text=Environment.MachineName;
   AddField(content,"Adapter serial (optional)",serial,"adapterSerial");
   ConfigureOption(notes,"removeNotes","Remove all step notes");CollectorTouchUi.Add(content,notes);
   ConfigureOption(descriptions,"removeDescriptions","Remove adapter description and device label");CollectorTouchUi.Add(content,descriptions);
   CollectorTouchUi.Add(content,CollectorTouchUi.Label("Names/serials: exact printable text, 3–128 characters. The PC name above is this computer; change it for an imported capture. Split/encoded identifiers and security exchanges may remain. Review the resulting report.",10,false));
   var create=new Button{DialogResult=DialogResult.OK};CollectorTouchUi.Button(create,"createSanitizedCopy","Create copy + report",delegate{});create.Width=230;
   var cancel=new Button{DialogResult=DialogResult.Cancel};CollectorTouchUi.Button(cancel,"cancelSanitization","Cancel",delegate{});cancel.Width=150;
   CollectorTouchUi.Add(content,CollectorTouchUi.Actions(create,cancel));AcceptButton=create;CancelButton=cancel;
  }
  void AddField(TableLayoutPanel content,string title,TextBox field,string name){
   CollectorTouchUi.Add(content,CollectorTouchUi.Label(title,11,false));CollectorTouchUi.TextField(field,name,title,128);
   var keyboard=new Button();CollectorTouchUi.Button(keyboard,name+"Keyboard","Keyboard",delegate{CollectorTouchUi.ShowKeyboard(this,field);});
   CollectorTouchUi.Add(content,CollectorTouchUi.InputRow(field,keyboard,124));
  }
  void ConfigureOption(CheckBox option,string name,string text){option.Name=name;option.Text=text;option.Checked=true;option.AutoSize=true;option.Dock=DockStyle.Top;option.MinimumSize=new Size(0,48);option.Margin=new Padding(0,4,0,4);}
 }
 public static class SanitizeReportDialog {
  public static DialogResult Show(IWin32Window owner,SanitizeReport report,bool upload){
   using(var form=Create(report,upload))return form.ShowDialog(owner);
  }
  internal static Form Create(SanitizeReport report,bool upload){
    var form=new Form{Text=upload?"Review sanitization before upload":"Sanitization report",Font=new Font("Segoe UI",11),AutoScaleMode=AutoScaleMode.Font,ClientSize=new Size(680,500),MinimumSize=new Size(360,360),StartPosition=FormStartPosition.CenterParent};
    var layout=new TableLayoutPanel{ColumnCount=1,RowCount=2,Dock=DockStyle.Fill,Padding=new Padding(12)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));form.Controls.Add(layout);
    var text=new TextBox{Name="sanitizationReport",AccessibleName="Sanitization review report",Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=(upload?"Send this reviewed copy privately to OpenSAAB? Nothing is sent until you choose Upload this copy.\r\n\r\n":"")+report.Summary()};layout.Controls.Add(text,0,0);
    var close=new Button{DialogResult=DialogResult.Cancel};CollectorTouchUi.Button(close,"closeReport",upload?"Cancel":"Close",delegate{});close.Width=140;form.CancelButton=close;
    FlowLayoutPanel actions;
    if(upload){var send=new Button{DialogResult=DialogResult.OK};CollectorTouchUi.Button(send,"uploadReviewedCopy","Upload this copy",delegate{});send.Width=210;actions=CollectorTouchUi.Actions(close,send);}
    else actions=CollectorTouchUi.Actions(close);
    layout.Controls.Add(actions,0,1);
    // Enter never authorizes an upload accidentally; the explicit button remains required.
    form.ActiveControl=close;return form;
  }
 }
}
