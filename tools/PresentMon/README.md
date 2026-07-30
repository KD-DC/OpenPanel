# PresentMon Runtime

OpenPanel's optional Gaming widget uses the official PresentMon command-line
collector. The executable is not committed to the repository.

Run `scripts\setup-presentmon.ps1` to download the pinned x64 release and verify
its SHA-256 checksum. Builds copy the executable into `Tools\PresentMon.exe`
when it is present. Without it, OpenPanel continues to run and the Gaming
widget's Start button remains disabled.

The collector starts only after the user presses Start. Pressing Stop or exiting
OpenPanel kills the process tree and terminates the named ETW session.
