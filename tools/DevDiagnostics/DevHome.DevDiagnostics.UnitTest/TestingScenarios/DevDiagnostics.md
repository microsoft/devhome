# Dev Diagnostics Tests
If your code affects Dev Diagnostics, please manually verify these scenarios.

## Scenarios
Please make sure to verify all of these scenarios. These apply to both Windows 10 and Windows 11.

### First Run and First Setup Experience
1. Dev Diagnostics: On Dev/Canary/Preview toggle Dev Diagnostics experimental feature on/off/on again to enable startup task
1. Dev Diagnostics: Reboot and invoking Dev Diagnostics with Win-F12 should launch it immediately.

### Core Feature (Feature is Enabled and Configured)
1. Dev Diagnostics: Attach to foreground app - Launch any app and Hit Win-F12, Validate app that is in foreground was selected in Dev Diagnostics
1. Dev Diagnostics: Close Bar Window, Hit Win-F12. Validate app that is in foreground was selected in Dev Diagnostics.
1. Dev Diagnostics WinLogs: Remove yourself from Performance Log Users from lsusrmgr.ms, Log out and log back in to Windows
1. Dev Diagnostics WinLogs: Validate that ETW logs are unchecked by default and checking them triggers a UAC prompt
1. Dev Diagnostics WinLogs: Accept UAC prompt, Log Out and log back in, ETW logs should be checked by default.
1. Dev Diagnostics WinLogs: Attach to an application and validate logs are working.
1. Dev Diagnostics WinLogs: Crash the app/end task from taskmanager and validate logs
1. Dev Diagnostics WinLogs: ETW logs check box works. Example scenario - attach to notepad and close it.
1. Dev Diagnostics WinLogs: Debug output check-box works. Example scenario - Attach to DevHome, go to Widgets page and add widgets like CPU and GPU.
1. .Dev Diagnostics ProcessList Page: Filter text box works both for Dev Diagnostics and process name
1. Dev Diagnostics ProcessList Page: Click on any process and confirm Dev Diagnostics attaches to that process.
1. Dev Diagnostics ProcessList Page: After attaching to new process, verify that other pages have relevant info related to newly attached process.
1. Dev Diagnostics System Process/Admin Process: Attach Dev Diagnostics to Task Manager or other system process, verify that AppDetails and Modules page show "Run As Admin" button
1. Dev Diagnostics System Process/Admin Process: Clicking on "Run As Admin" button should work.