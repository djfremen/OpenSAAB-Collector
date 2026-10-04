using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace OpenSaab.Collector {
 public partial class CollectorForm {
  void BuildLayout(){
   Text="OpenSAAB Collector "+Bundle.Version;
   Font=new Font("Segoe UI",11);
   BackColor=Color.FromArgb(246,248,251);
   AutoScaleMode=AutoScaleMode.Font;
   ClientSize=new Size(720,520);
   MinimumSize=new Size(360,360);
   StartPosition=FormStartPosition.CenterScreen;
   var content=CollectorTouchUi.ScrollContent(this,"collectorViewport");
   var header=new TableLayoutPanel{ColumnCount=2,RowCount=1,AutoSize=true,Dock=DockStyle.Top,Margin=new Padding(0,0,0,12)};
   header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));
   header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   using(var stream=typeof(CollectorForm).Assembly.GetManifestResourceStream("OpenSAAB.logo.png"))using(var source=Image.FromStream(stream)){
    var logo=new PictureBox{Image=new Bitmap(source),SizeMode=PictureBoxSizeMode.Zoom,AccessibleName="OpenSAAB logo",Size=new Size(64,64),Margin=new Padding(0,0,12,0)};
    header.Controls.Add(logo,0,0);
   }
   using(var stream=typeof(CollectorForm).Assembly.GetManifestResourceStream("OpenSAAB.icon.ico")){Icon=new Icon(stream);}
   var heading=new TableLayoutPanel{ColumnCount=1,AutoSize=true,Dock=DockStyle.Top,Margin=Padding.Empty};
   CollectorTouchUi.Add(heading,CollectorTouchUi.Label("OpenSAAB Collector",18,true));
   CollectorTouchUi.Add(heading,CollectorTouchUi.Label("Record your adapter. Help bring more devices to OpenSAAB.",11,false));
   header.Controls.Add(heading,1,0);CollectorTouchUi.Add(content,header);

   CollectorTouchUi.Add(content,CollectorTouchUi.Label("1   Choose the adapter model",12,true));
   adapterModels.Name="adapterModels";adapterModels.AccessibleName="Required adapter model";
   adapterModels.DropDownStyle=ComboBoxStyle.DropDownList;adapterModels.Font=new Font("Segoe UI",14);
   adapterModels.Dock=DockStyle.Top;adapterModels.Margin=new Padding(0,4,0,12);
   adapterModels.Items.Add("Select adapter model (required)");
   foreach(string model in CaptureIdentity.Models)adapterModels.Items.Add(model);
   adapterModels.SelectedIndexChanged+=delegate{SetBusy(busy);};
   adapterModels.SelectedIndex=0;CollectorTouchUi.Add(content,adapterModels);
   CollectorTouchUi.Add(content,CollectorTouchUi.Label("Your selection labels this capture; it does not verify adapter support.",10,false));
   CollectorTouchUi.Add(content,CollectorTouchUi.Label("2   Select its USB device",12,true));
   devices.Name="devices";devices.AccessibleName="USB capture device";devices.DropDownStyle=ComboBoxStyle.DropDownList;
   devices.Font=new Font("Segoe UI",12);devices.Dock=DockStyle.Fill;devices.SelectedIndexChanged+=delegate{SetBusy(busy);};
   CollectorTouchUi.Button(refresh,"refresh","Refresh",async delegate{await RefreshDevices();});
   CollectorTouchUi.Add(content,CollectorTouchUi.InputRow(devices,refresh,124));
   CollectorTouchUi.Add(content,CollectorTouchUi.Label("Select the adapter itself, not the USB hub, mouse or keyboard.",10,false));

   CollectorTouchUi.Add(content,CollectorTouchUi.Label("Driver version / car model and year (optional)",11,false));
   CollectorTouchUi.TextField(adapter,"adapterDetails","Optional adapter and vehicle details",240);
   var detailsKeyboard=new Button();CollectorTouchUi.Button(detailsKeyboard,"detailsKeyboard","Keyboard",delegate{CollectorTouchUi.ShowKeyboard(this,adapter);});
   CollectorTouchUi.Add(content,CollectorTouchUi.InputRow(adapter,detailsKeyboard,124));
   CollectorTouchUi.Button(setup,"setup","Set up USBPcap",async delegate{await Install();});
   CollectorTouchUi.Add(content,CollectorTouchUi.Actions(setup));
   CollectorTouchUi.Add(content,CollectorTouchUi.Label("Wireshark is not required. First driver install needs a restart.",10,false));
   consent.Name="captureConsent";consent.AccessibleName="Capture privacy acknowledgement";consent.AutoSize=true;consent.Dock=DockStyle.Top;
   consent.MinimumSize=new Size(0,64);consent.Margin=new Padding(0,8,0,12);
   consent.Text="I understand this recording may contain VINs, serials and security data. Captures stay on this computer until I choose Upload and confirm.";
   CollectorTouchUi.Add(content,consent);

   CollectorTouchUi.Button(start,"start","Start capture",async delegate{await StartCapture();});
   CollectorTouchUi.Button(stop,"stop","Stop capture",async delegate{await Finish();});stop.Enabled=false;
   CollectorTouchUi.Button(folder,"folder","Open saved files",delegate{Process.Start(session ?? BaseDir());});
   CollectorTouchUi.Add(content,CollectorTouchUi.Actions(start,stop,folder));
   CollectorTouchUi.Add(content,CollectorTouchUi.Label("Step note (optional)",11,false));
   CollectorTouchUi.TextField(note,"stepNote","Capture step note",400);
   var noteKeyboard=new Button();CollectorTouchUi.Button(noteKeyboard,"noteKeyboard","Keyboard",delegate{CollectorTouchUi.ShowKeyboard(this,note);});
   CollectorTouchUi.Add(content,CollectorTouchUi.InputRow(note,noteKeyboard,124));
   CollectorTouchUi.Button(add,"addNote","Add step note",delegate{AddNote();});add.Enabled=false;
   CollectorTouchUi.Add(content,CollectorTouchUi.Actions(add));
   status.Name="captureStatus";status.AutoSize=true;status.Dock=DockStyle.Top;status.MinimumSize=new Size(0,52);status.Margin=new Padding(0,8,0,8);
   status.Text="Connect the adapter, choose its model, then refresh.";CollectorTouchUi.Add(content,status);
   CollectorTouchUi.Button(sanitize,"sanitize","Sanitize capture",async delegate{await SanitizeSaved();});
   CollectorTouchUi.Button(reportButton,"viewReport","View report",delegate{ReviewSanitization();});
   CollectorTouchUi.Button(retry,"upload","Upload",async delegate{await UploadSaved();});
   CollectorTouchUi.Add(content,CollectorTouchUi.Actions(sanitize,reportButton,retry));
   var donate=new Button();CollectorTouchUi.Button(donate,"donate","Donate · Support OpenSAAB",delegate{ShowSupport();});
   donate.Width=280;CollectorTouchUi.Add(content,CollectorTouchUi.Actions(donate));
   timer.Interval=500;timer.Tick+=async delegate{if(worker!=null && !busy && !finishing){if(worker.HasExited)await Finish();else status.Text="Recording — launch Tech2Win, select your adapter, then read VIN / ECM information / DTCs.\nElapsed: "+(DateTime.UtcNow-captureStarted).ToString(@"mm\:ss")+" (15-minute / 60 MiB limit)";}};timer.Start();
   Shown+=async delegate{await RefreshDevices();};
   FormClosing+=delegate(object sender,FormClosingEventArgs e){if(worker!=null || busy){e.Cancel=true;MessageBox.Show("Finish the capture or current upload before closing. Local files will be retained.",Text);}};
   FormClosed+=delegate{timer.Stop();timer.Dispose();};
  }
 }

 // Standard Windows controls retain mouse, keyboard and touch-generated clicks.
 // All rows can scroll on a short tablet screen; action rows wrap on narrow ones.
 internal static class CollectorTouchUi {
  internal static TableLayoutPanel ScrollContent(Form form,string name){
   var viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Name=name,TabStop=false};form.Controls.Add(viewport);
   // The viewport owns the content's scrolled location. Dock.Top would relayout
   // it at the top again when a nested row changes, undoing the scroll offset.
   var content=new TableLayoutPanel{ColumnCount=1,RowCount=0,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Anchor=AnchorStyles.Top|AnchorStyles.Left,Padding=new Padding(16),Margin=Padding.Empty,Name=name+"Content"};
   content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));viewport.Controls.Add(content);
   bool updating=false;
   Action update=delegate{
    if(updating || viewport.IsDisposed)return;updating=true;
    try{
     // ClientSize already excludes a visible scrollbar. Constrain only width;
     // the full content height defines the vertical scrollbar's real range.
     int width=Math.Max(1,viewport.ClientSize.Width);var constraint=new Size(width,0);
     content.SuspendLayout();
     if(content.MinimumSize!=constraint)content.MinimumSize=constraint;
     if(content.MaximumSize!=constraint)content.MaximumSize=constraint;
     if(content.Width!=width)content.Width=width;
     content.ResumeLayout(true);
     WrapText(content,Math.Max(120,width-content.Padding.Horizontal));content.PerformLayout();
     int height=content.GetPreferredSize(new Size(width,0)).Height;if(content.Height!=height)content.Height=height;
     var extent=new Size(0,content.Height);if(viewport.AutoScrollMinSize!=extent)viewport.AutoScrollMinSize=extent;
    }finally{updating=false;}
   };
   viewport.Layout+=delegate{update();};content.SizeChanged+=delegate{update();};
   return content;
  }
  internal static void Add(TableLayoutPanel content,Control control){
   int row=content.RowCount++;content.RowStyles.Add(new RowStyle(SizeType.AutoSize));content.Controls.Add(control,0,row);
  }
  internal static Label Label(string text,int size,bool bold){return new Label{Text=text,AutoSize=true,Dock=DockStyle.Top,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),Margin=new Padding(0,4,0,4)};}
  internal static void Button(Button button,string name,string text,EventHandler action){
   button.Name=name;button.Text=text;button.AccessibleName=text;button.Size=new Size(190,48);button.MinimumSize=new Size(120,48);button.Margin=new Padding(0,4,8,4);button.Click+=action;
  }
  internal static void TextField(TextBox field,string name,string accessibleName,int limit){
   field.Name=name;field.AccessibleName=accessibleName;field.Multiline=true;field.AcceptsReturn=false;field.Height=48;field.MinimumSize=new Size(0,48);field.MaxLength=limit;field.Dock=DockStyle.Fill;field.Margin=new Padding(0,4,8,4);
  }
  internal static TableLayoutPanel InputRow(Control input,Button action,int actionWidth){
   var row=new TableLayoutPanel{ColumnCount=2,RowCount=1,AutoSize=true,Dock=DockStyle.Top,Margin=new Padding(0,0,0,4)};
   int columnWidth=Math.Max(actionWidth,action.MinimumSize.Width+8);
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,columnWidth));
   input.Margin=new Padding(0,4,8,4);action.Width=columnWidth-8;action.Dock=DockStyle.Fill;
   row.Controls.Add(input,0,0);row.Controls.Add(action,1,0);return row;
  }
  internal static FlowLayoutPanel Actions(params Button[] buttons){
   var row=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,WrapContents=true,FlowDirection=FlowDirection.LeftToRight,Margin=Padding.Empty,Padding=Padding.Empty};
   foreach(var button in buttons)row.Controls.Add(button);return row;
  }
  static void WrapText(Control parent,int width){
   foreach(Control control in parent.Controls){
    if(control is Label || control is CheckBox){var limit=new Size(width,0);if(control.MaximumSize!=limit)control.MaximumSize=limit;}
    if(control.HasChildren)WrapText(control,Math.Max(120,Math.Min(width,control.ClientSize.Width)));
   }
  }
  internal static void ShowKeyboard(IWin32Window owner,TextBox target){
   target.Focus();
   try{
    string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"osk.exe");
    if(!File.Exists(path))throw new FileNotFoundException();
    Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
   }catch(Exception){MessageBox.Show(owner,"Windows could not open its on-screen keyboard. Use Windows Ease of Access to open it, or connect a keyboard.","On-screen keyboard",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  }
 }
}
