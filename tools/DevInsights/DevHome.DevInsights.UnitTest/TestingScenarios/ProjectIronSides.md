# Project Ironsides Tests
If your code affects Project Ironsides, please manually verify these scenarios.

## Scenarios
Please make sure to verify all of these scenarios. These apply to both Windows 10 and Windows 11.

### First Run and First Setup Experience
1. PI: On Dev/Canary/Preview toggle Dev Home PI experimental feature on/off/on again to enable startup task 
1. PI: Reboot and invoking PI with Win-F12 should launch it immediately.

### Core Feature (Feature is Enabled and Configured)
1. PI: Attach to foreground app - Launch any app and Hit Win-F12, Validate app that is in foreground was selected in PI 
1. PI: Close Bar Window, Hit Win-F12. Validate app that is in foreground was selected in PI.
1. PI Dock - Undock: Dock button should make PI vertical and dock to right side of the window
1. PI Dock - Undock: Hitting Dock button again should undock PI
1. PI Dock - Undock: Dragging PI window in vertival mode to right hand side of attached app should make PI dock to right side.
1. PI WinLogs: Remove yourself from Performance Log Users from lsusrmgr.ms, Log out and log back in to Windows
1. PI WinLogs: Validate that ETW logs are unchecked by default and checking them triggers a UAC prompt
1. PI WinLogs: Accept UAC prompt, Log Out and log back in, ETW logs should be checked by default.
1. PI WinLogs: Attach to an application and validate logs are working
1. PI WinLogs: Crash the app/end task from taskmanager and validate logs
1. PI WinLogs: ETW logs check box works. Example scenario - attach to notepad and close it.
1. PI WinLogs: Debug output check-box works. Example scenario - Attach to DevHome, go to Widgets page and add widgets like CPU and GPU
1. PI ProcessList Page: Filter text box works both for PIDs and process name
1. PI ProcessList Page: Click on any process and confirm PI attaches to that process.
1. PI ProcessList Page: After attaching to new process, verify that other pages have relevant info related to newly attached process.
1. PI System Process/Admin Process: Attach PI to Task Manager or other system process, verify that AppDetails and Modules page show "Run As Admin" button
1. PI System Process/Admin Process: Clicking on "Run As Admin" button should work.