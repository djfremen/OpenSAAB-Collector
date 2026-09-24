Thanks for offering to help. You can keep Windows 8.1 if Tech2Win and your Mongoose driver already work there; there is no need to upgrade just to record the USB traffic. For that OS, Wireshark 4.0.17 is the last compatible Wireshark release, used with USBPcap.

Let's start small: begin a capture of the Mongoose USB device before opening Tech2Win, launch Tech2Win, select your usual Mongoose driver, then read the VIN. If that looks good, continue to ECM information and read the DTCs. Please note the menu path and approximate times, your exact Mongoose model/driver version, and the car's year/model/engine. No need to clear codes or request security access for this first recording.

Stop and save the capture locally. Please don't attach the raw file publicly because it can contain your VIN and other private data; we'll arrange a private transfer. A short startup/VIN sample first will let us confirm we're recording the right device before asking you to do more.

I'm adapting OpenSAAB-Collector for this, with a separate capture-only workflow that leaves your existing driver alone. One correction to my earlier wording: the trace gives us valuable evidence to build and test against, but I can't promise a single file contains everything required for a working Android driver.

Reference: https://www.wireshark.org/docs/relnotes/wireshark-4.0.17.html
USBPcap: https://desowin.org/usbpcap/
