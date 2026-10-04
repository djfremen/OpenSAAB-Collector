using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenSaab.Collector {
 public class UsbDevice {
  public string Hub;public int Address;public string Label;
  public override string ToString(){return Hub.Replace(@"\\.\","")+" — "+Label;}

  public static List<UsbDevice> ParseConfig(string hub,string config) {
   var devices=new List<UsbDevice>();
   foreach(string line in config.Split('\n')) {
    // USBPcap uses numeric values for physical devices, including devices
    // behind external hubs. Display-only composite children use N_M values
    // and enabled=false. A parent field alone must not exclude an adapter.
    // https://github.com/desowin/usbpcap/blob/1.5.4.0/USBPcapCMD/enum.c
    var match=Regex.Match(line,@"^value \{arg=\d+\}\{value=(\d+)\}\{display=([^}]+)\}");
    int address;
    if(match.Success && !line.Contains("{enabled=false}") &&
       int.TryParse(match.Groups[1].Value,out address) && address>=1 && address<=127)
     devices.Add(new UsbDevice{Hub=hub,Address=address,Label=match.Groups[2].Value});
   }
   return devices;
  }
 }
}
