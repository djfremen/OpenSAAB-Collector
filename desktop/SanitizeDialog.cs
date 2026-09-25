using System;using System.Drawing;using System.Windows.Forms;
namespace OpenSaab.Collector {
 public sealed class SanitizeDialog:Form {
  TextBox vin=new TextBox(),computer=new TextBox(),serial=new TextBox();
  CheckBox notes=new CheckBox(),descriptions=new CheckBox();
  public SanitizeOptions Options{get{return new SanitizeOptions{Vin=vin.Text,ComputerName=computer.Text,Serial=serial.Text,RemoveNotes=notes.Checked,RemoveDescriptions=descriptions.Checked};}}
  public SanitizeDialog(){
   Text="Sanitize a local copy";Font=new Font("Segoe UI",10);ClientSize=new Size(620,425);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
   Label info=new Label{Text="Creates a separate copy. The original is kept and nothing is uploaded.\nDetects complete VIN-shaped strings; known values improve matching.",AutoSize=false};info.SetBounds(20,15,580,48);Controls.Add(info);
   AddField("Known VIN (optional)",vin,76);AddField("Computer name to mask",computer,124);AddField("Adapter serial (optional)",serial,172);computer.Text=Environment.MachineName;
   notes.Text="Remove all step notes";notes.Checked=true;notes.SetBounds(20,226,580,28);Controls.Add(notes);
   descriptions.Text="Remove adapter description and device label";descriptions.Checked=true;descriptions.SetBounds(20,256,580,28);Controls.Add(descriptions);
   var warning=new Label{Text="Names/serials: exact printable text, 3–128 characters. The PC name above is this computer; change it for an imported capture. Split/encoded identifiers and security exchanges may remain. Review the resulting report.",AutoSize=false};warning.SetBounds(20,297,580,65);Controls.Add(warning);
   var create=new Button{Text="Create copy + report",DialogResult=DialogResult.OK};create.SetBounds(20,374,290,34);Controls.Add(create);
   var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel};cancel.SetBounds(325,374,275,34);Controls.Add(cancel);AcceptButton=create;CancelButton=cancel;
  }
  void AddField(string title,TextBox field,int y){var label=new Label{Text=title};label.SetBounds(20,y+3,205,26);Controls.Add(label);field.SetBounds(228,y,372,28);field.MaxLength=128;Controls.Add(field);}
 }
 public static class SanitizeReportDialog {
  public static DialogResult Show(IWin32Window owner,SanitizeReport report,bool upload){
   using(var form=new Form{Text=upload?"Review sanitization before upload":"Sanitization report",Font=new Font("Segoe UI",10),ClientSize=new Size(680,560),MinimumSize=new Size(550,450),StartPosition=FormStartPosition.CenterParent}){
    var text=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=(upload?"Send this reviewed copy privately to OpenSAAB? Nothing is sent until you choose Upload this copy.\r\n\r\n":"")+report.Summary()};form.Controls.Add(text);
    var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=50,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};form.Controls.Add(row);
    var close=new Button{Text=upload?"Cancel":"Close",DialogResult=DialogResult.Cancel,Width=125,Height=32};row.Controls.Add(close);form.CancelButton=close;
    if(upload){var send=new Button{Text="Upload this copy",DialogResult=DialogResult.OK,Width=190,Height=32};row.Controls.Add(send);}
    form.ActiveControl=close;return form.ShowDialog(owner);
   }
  }
 }
}
