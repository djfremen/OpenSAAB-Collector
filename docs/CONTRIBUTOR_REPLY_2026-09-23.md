Thanks for offering to help. We now have a simple OpenSAAB Collector preview:

https://github.com/djfremen/OpenSAAB-Collector/releases/tag/v0.5.0-preview.1

Download OpenSAAB-Collector.exe and run it as administrator. If USBPcap is missing,
click Set up USBPcap, complete its installer and restart Windows. You do not need
Wireshark. Keep your existing Mongoose driver and working Tech2Win installation.

Select your adapter in Collector, accept the private upload notice, and click
Start capture before opening Tech2Win. Then launch Tech2Win, select your Mongoose
adapter and read the VIN. Add step notes if possible. Click Stop & upload when
finished; Collector sends the capture privately to OpenSAAB and confirms receipt.
It keeps a local copy and offers Retry saved upload if the connection fails.

Let's start with initialization and VIN. Once we verify that recording, we can
collect ECM information and DTC reads. No need to clear codes, program anything
or request security access for the first sample.

This is tested on Windows 10 with Chipsoft. It is built using APIs available on
Windows 8.1, but your Windows 8.1/Mongoose combination is still untested. There is
no need to upgrade your working diagnostic computer just for this first trial.
If Collector cannot run there, we can use the manual Wireshark 4.0.17 workflow.
The Collector executable is currently unsigned.

Please don't attach raw captures publicly: they can contain your VIN and other
private information. A recording helps us build and test adapter support; one
file cannot guarantee a complete Android driver.

Instructions: https://github.com/djfremen/OpenSAAB-Collector/blob/main/desktop/README.md
Privacy: https://github.com/djfremen/OpenSAAB-Collector/blob/main/docs/CAPTURE_PRIVACY.md
