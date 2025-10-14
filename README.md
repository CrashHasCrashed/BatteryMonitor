# BatteryMonitor

Made for personal use. Shows battery percentage of devices in Windows system tray.

Razor Deathadder battery percentage can be extracted from a config file from the Synapse application. The log file updates seemingly at random, but at least on boot it updates once. This is enough for me to warn me when to charge my device.

I also use an Antlion Wireless ModMic, which suffers from the same limitations, as there is no way of seeing the battery level. Unfortunately there does not seem to be a log file for this device, so instead I track if any audio is picked up from the microphone and slowly decrease an estimated battery percentage level.

The system tray icons show a different color depending on the battery level.
