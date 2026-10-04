# OpenSAAB Collector 0.5.5: USB hub device selection

A diagnostic adapter connected through an external USB hub could be missing from
the device picker in 0.5.3. Collector excluded every USBPcap entry with a parent,
including real devices behind hubs. The corrected parser retains enabled numeric
physical-device addresses and excludes disabled composite display children. The
same parser checks the selected device again before capture starts.

Select **MongoosePro GM II** itself, rather than **Generic USB Hub**, a mouse or
keyboard. Keep the existing hub, adapter driver, Tech2Win and USBPcap installation.
Start capture before opening Tech2Win, record a short initialization/VIN attempt,
then stop, review, optionally sanitize a copy and explicitly upload when ready.

A contributor's reported Windows 8 Pro x32 recording uploaded successfully but
contained only six hub control records. It does not establish Mongoose diagnostic
success or a VIN result. The upload is retained privately.

Regression checks cover adapters behind one or two hubs, direct connections,
separate root buses, disabled composite children and invalid USB addresses. The
Windows build runs these alongside the existing bundle and sanitizer checks.
The server's ten validation tests remain required. A fresh Windows 8 Pro x32 /
Mongoose hardware capture is still pending.

Unsigned developer preview. This release corrects device selection and does not
claim a completed Mongoose diagnostic backend. Existing capture, local review,
sanitization and explicit upload behavior are retained.
