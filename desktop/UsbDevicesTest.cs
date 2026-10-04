using System;
using System.Linq;
using OpenSaab.Collector;

class UsbDevicesTest {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static int Main() {
  // Format emitted by USBPcap 1.5.4.0: enabled numeric entries are physical
  // devices; N_M children are disabled descriptive nodes. The external hub
  // and adapter remain separate capture addresses even on the same root bus.
  string config="arg {number=5}{call=--devices}{type=multicheck}\r\n"+
   "value {arg=5}{value=3}{display=[3] Generic USB Hub}{enabled=true}\r\n"+
   "value {arg=5}{value=4}{display=[4] USB Mouse}{enabled=true}{parent=3}\r\n"+
   "value {arg=5}{value=5}{display=[5] USB Composite Device}{enabled=true}{parent=3}\r\n"+
   "value {arg=5}{value=5_1}{display=USB Keyboard}{enabled=false}{parent=5}\r\n"+
   "value {arg=5}{value=5_2}{display=HID Keyboard Device}{enabled=false}{parent=5_1}\r\n"+
   "value {arg=5}{value=6}{display=[6] MongoosePro GM II}{enabled=true}{parent=3}\r\n"+
   "value {arg=5}{value=7}{display=[7] Generic USB Hub}{enabled=true}{parent=3}\r\n"+
   "value {arg=5}{value=8}{display=[8] Chipsoft Pro}{enabled=true}{parent=7}\r\n"+
   "value {arg=5}{value=9}{display=[9] Direct adapter}{enabled=true}\r\n"+
   "value {arg=5}{value=10}{display=Disabled entry}{enabled=false}{parent=3}\r\n";
  var devices=UsbDevice.ParseConfig(@"\\.\USBPcap1",config);
  Check(devices.Select(d=>d.Address).SequenceEqual(new[]{3,4,5,6,7,8,9}),"Physical devices behind one or two hubs must remain selectable; disabled nodes must be omitted");
  var adapter=devices.Single(d=>d.Address==6);
  Check(adapter.Hub==@"\\.\USBPcap1" && adapter.Label=="[6] MongoosePro GM II","Adapter label/address must stay separate from the hub");
  Check(adapter.ToString()=="USBPcap1 — [6] MongoosePro GM II","Picker must display the adapter");
  var otherBus=UsbDevice.ParseConfig(@"\\.\USBPcap2","value {arg=5}{value=6}{display=[6] Other adapter}{enabled=true}\n");
  Check(otherBus.Count==1 && otherBus[0].Hub!=adapter.Hub,"USB addresses on different root buses are distinct");
  Check(UsbDevice.ParseConfig(adapter.Hub,"value {arg=5}{value=0}{display=Root hub}\nvalue {arg=5}{value=128}{display=Invalid device}\nvalue {arg=5}{value=999999999999999}{display=Overflow}\n").Count==0,"Invalid addresses cannot reach capture");
  Check(UsbDevice.ParseConfig(adapter.Hub,"").Count==0,"Empty configuration");
  Console.WriteLine("USB device picker regression checks passed");return 0;
 }
}
