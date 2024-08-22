# Dev Insights Tests
If your code affects Project Ironsides, please manually verify these scenarios.

## Scenarios
Please make sure to verify all of these scenarios. These apply to both Windows 10 and Windows 11.

### First Run and First Setup Experience
1. Dev Insights: On Dev/Canary/Preview toggle Dev Insights experimental feature on/off/on again to enable startup task
1. Dev Insights: Reboot and invoking Dev Insights with Win-F12 should launch it immediately.

### Core Feature (Feature is Enabled and Configured)
1. Dev Insights: Attach to foreground app - Launch any app and Hit Win-F12, Validate app that is in foreground was selected in Dev Insights
1. Dev Insights: Close Bar Window, Hit Win-F12. Validate app that is in foreground was selected in Dev Insights.
1. Dev Insights Dock - Undock: Dock button should make Dev Insights vertical and dock to right side of the window
1. Dev Insights Dock - Undock: Hitting Dock button again should undock Dev Insights
1. Dev Insights Dock - Undock: Dragging Dev Insights window in vertival mode to right hand side of attached app should make Dev Insights dock to right side.
1. Dev Insights WinLogs: Remove yourself from Performance Log Users from lsusrmgr.ms, Log out and log back in to Windows
1. Dev Insights WinLogs: Validate that ETW logs are unchecked by default and checking them triggers a UAC prompt
1. Dev Insights WinLogs: Accept UAC prompt, Log Out and log back in, ETW logs should be checked by default.
1. Dev Insights WinLogs: Attach to an application and validate logs are working
1. Dev Insights WinLogs: Crash the app/end task from taskmanager and validate logs
1. Dev Insights WinLogs: ETW logs check box works. Example scenario - attach to notepad and close it.
1. Dev Insights WinLogs: Debug output check-box works. Example scenario - Attach to DevHome, go to Widgets page and add widgets like CPU and GPU
1. Dev Insights ProcessList Page: Filter text box works both for Dev InsightsDs and process name
1. Dev Insights ProcessList Page: Click on any process and confirm Dev Insights attaches to that process.
1. Dev Insights ProcessList Page: After attaching to new process, verify that other pages have relevant info related to newly attached process.
1. Dev Insights System Process/Admin Process: Attach Dev Insights to Task Manager or other system process, verify that AppDetails and Modules page show "Run As Admin" button
1. Dev Insights System Process/Admin Process: Clicking on "Run As Admin" button should work.