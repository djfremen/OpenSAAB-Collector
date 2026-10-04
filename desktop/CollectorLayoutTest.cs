using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenSaab.Collector;

// Real Windows UI layout tests. The test-only subclass suppresses OnShown so
// showing the collector cannot invoke its production USB refresh handler.
// Capture workers, installers, keyboard launchers and network are not invoked.
public static class CollectorLayoutTest {
 sealed class TestCollectorForm:CollectorForm {protected override void OnShown(EventArgs e){}}
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 static Control Find(Control parent,string name){var found=parent.Controls.Find(name,true);Require(found.Length==1,"Expected one control: "+name);return found[0];}
 static IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
 static void Layout(Control control){control.PerformLayout();foreach(Control child in control.Controls)Layout(child);}
 static void Settle(Form form){for(int i=0;i<8;i++){Layout(form);Application.DoEvents();}}
 static void Resize(Form form,Size size){form.ClientSize=size;if(!form.Visible)form.Show();Settle(form);}
 static string Geometry(Form form,Control control){
  string value=" window="+form.ClientSize;
  for(Control current=control;current!=null && current!=form;current=current.Parent)value+=" ["+current.Name+"/"+current.GetType().Name+" bounds="+current.Bounds+" client="+current.ClientRectangle+"]";
  return value;
 }
 static void Snapshot(Form form,string label){
  using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),label+".png"),ImageFormat.Png);}
 }
 static void ButtonsFit(Form form){
  foreach(var control in Descendants(form))if(control is Button){
   Require(control.Height>=44,"Button touch target too short: "+control.Name);
   Require(control.Width>=120,"Button touch target too narrow: "+control.Name);
   Require(control.Left>=0 && control.Right<=control.Parent.ClientSize.Width,"Button clipped horizontally: "+control.Name+Geometry(form,control));
   Require(control.Top>=0 && control.Bottom<=control.Parent.ClientSize.Height,"Button clipped vertically: "+control.Name+Geometry(form,control));
  }
 }
 static void Reach(Form form,Panel viewport,string name){
  var button=Find(form,name);string priorFocus=form.ActiveControl==null?"none":form.ActiveControl.Name;
  viewport.ScrollControlIntoView(button);
  var immediate=viewport.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
  // Match interacting with the target. Otherwise explicitly laying out every
  // container may scroll back to the previously focused field instead.
  if(button.Enabled)Require(button.Focus(),"Could not focus visible action: "+name);
  Settle(form);
  var bounds=viewport.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
  string geometry=Geometry(form,button)+" viewport="+viewport.ClientRectangle+" beforeSettle="+immediate+" translated="+bounds+" scroll="+viewport.AutoScrollPosition+" extent="+viewport.AutoScrollMinSize+" display="+viewport.DisplayRectangle+" previousFocus="+priorFocus+" currentFocus="+(form.ActiveControl==null?"none":form.ActiveControl.Name);
  Require(immediate.Top>=0 && immediate.Bottom<=viewport.ClientSize.Height && immediate.Left>=0 && immediate.Right<=viewport.ClientSize.Width,"ScrollControlIntoView did not expose action: "+name+geometry);
  Require(bounds.Top>=0 && bounds.Bottom<=viewport.ClientSize.Height,"Scrolled action remains out of view: "+name+geometry);
  Require(bounds.Left>=0 && bounds.Right<=viewport.ClientSize.Width,"Scrolled action exceeds viewport: "+name+geometry);
 }
 static void ScrollbarBottom(Form form,Panel viewport){
  Find(form,"adapterModels").Focus();Settle(form);
  // The same WM_VSCROLL/SB_BOTTOM path as a native scrollbar command, with an
  // earlier field still focused. Do not substitute ScrollControlIntoView.
  SendMessage(viewport.Handle,0x0115,new IntPtr(7),IntPtr.Zero);Application.DoEvents();
  var donate=Find(form,"donate");var bounds=viewport.RectangleToClient(donate.RectangleToScreen(donate.ClientRectangle));
  Require(viewport.ClientRectangle.Contains(bounds),"Native scrollbar cannot reach donation action"+Geometry(form,donate)+" translated="+bounds+" scroll="+viewport.AutoScrollPosition+" extent="+viewport.AutoScrollMinSize+" display="+viewport.DisplayRectangle);
 }
 static void MainLayout(){
  using(var form=new TestCollectorForm()){
   var timer=(Timer)typeof(CollectorForm).GetField("timer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);timer.Stop();
   try{
    var model=(ComboBox)Find(form,"adapterModels");var device=(ComboBox)Find(form,"devices");var start=(Button)Find(form,"start");
    Require(model.DropDownStyle==ComboBoxStyle.DropDownList && device.DropDownStyle==ComboBoxStyle.DropDownList,"Selectors permit arbitrary values");
    Require(model.Items.Count==5 && model.SelectedIndex==0,"Required model placeholder missing");
    string[] expected={"Chipsoft","MDI","Mongoose","Nano"};for(int i=0;i<expected.Length;i++)Require((string)model.Items[i+1]==expected[i],"Model catalog changed");
    Require(!start.Enabled && !((CheckBox)Find(form,"captureConsent")).Checked,"Default capture must be unselected and unacknowledged");
    model.SelectedIndex=1;Require(!start.Enabled,"Model alone enables capture");
    device.Items.Add(new UsbDevice{Hub=@"\\.\USBPcap1",Address=1,Label="Synthetic adapter for UI test"});device.SelectedIndex=0;
    Require(start.Enabled,"Choosing physical device after model leaves capture disabled");
    model.SelectedIndex=0;Require(!start.Enabled,"Placeholder enables capture");
    device.SelectedIndex=-1;model.SelectedIndex=4;Require(!start.Enabled,"Missing physical device enables capture");
    model.SelectedIndex=0;device.SelectedIndex=0;Require(!start.Enabled,"Device alone enables capture");
    model.SelectedIndex=2;Require(start.Enabled,"Choosing model after physical device leaves capture disabled");
    var setBusy=typeof(CollectorForm).GetMethod("SetBusy",BindingFlags.Instance|BindingFlags.NonPublic);setBusy.Invoke(form,new object[]{true});
    Require(!start.Enabled && !device.Enabled && !model.Enabled,"Busy capture controls remain enabled");setBusy.Invoke(form,new object[]{false});
    foreach(var size in new[]{new Size(800,600),new Size(360,360)}){
     Resize(form,size);var viewport=(Panel)Find(form,"collectorViewport");viewport.AutoScrollPosition=Point.Empty;Settle(form);Snapshot(form,"collector-layout-"+size.Width+"x"+size.Height);ButtonsFit(form);
     Require(viewport.AutoScroll && !viewport.HorizontalScroll.Visible,"Main layout needs horizontal scrolling window="+form.ClientSize+" viewport="+viewport.ClientRectangle+" scroll="+viewport.AutoScrollPosition);
     Require(Find(form,"adapterDetails").Height>=44 && Find(form,"stepNote").Height>=44,"Text entry targets too short");
     ScrollbarBottom(form,viewport);
     foreach(string name in new[]{"start","stop","sanitize","viewReport","upload","donate"})Reach(form,viewport,name);
     Snapshot(form,"collector-layout-actions-"+size.Width+"x"+size.Height);
     if(size.Width==360)Require(Find(form,"stop").Top>Find(form,"start").Top,"Narrow action row failed to wrap");
    }
   }finally{timer.Dispose();}
  }
 }
 static void SanitizeLayout(){
  using(var form=new SanitizeDialog()){
   Require(form.Options.RemoveNotes && form.Options.RemoveDescriptions,"Sanitization defaults changed");
   Require(form.Options.ComputerName==Environment.MachineName,"Computer-name default changed");
   foreach(var size in new[]{new Size(800,600),new Size(360,360)}){
    Resize(form,size);ButtonsFit(form);var viewport=(Panel)Find(form,"sanitizeViewport");
    Require(viewport.AutoScroll && !viewport.HorizontalScroll.Visible,"Sanitize layout needs horizontal scrolling window="+form.ClientSize+" viewport="+viewport.ClientRectangle+" scroll="+viewport.AutoScrollPosition);
    Reach(form,viewport,"createSanitizedCopy");Reach(form,viewport,"cancelSanitization");
   }
  }
  var create=typeof(SanitizeReportDialog).GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic);
  foreach(bool upload in new[]{false,true})using(var form=(Form)create.Invoke(null,new object[]{new SanitizeReport(),upload})){
   Require(form.AcceptButton==null,"Enter must not authorize upload");
   foreach(var size in new[]{new Size(800,600),new Size(360,360)}){
    Resize(form,size);ButtonsFit(form);
    foreach(string name in upload?new[]{"closeReport","uploadReviewedCopy"}:new[]{"closeReport"}){
     var button=Find(form,name);var bounds=form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
     Require(form.ClientRectangle.Contains(bounds),"Report action is outside the window: "+name+Geometry(form,button)+" translated="+bounds);
    }
   }
  }
 }
 [STAThread]public static int Main(){
  try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);MainLayout();SanitizeLayout();Console.WriteLine("Collector layout and adapter selection tests passed; no hardware or network used.");return 0;}
  catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}
 }
}
