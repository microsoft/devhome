# Utilities Tests
If your code affects Utilities, please manually verify these scenarios.

## Scenarios
Please make sure to verify all of these scenarios. These apply to both Windows 10 and Windows 11.

#### Core
1. User can launch utilities from Utilities page
1. User can launch utilities from Utilities page as admin
1. Strings should be localized in all three utilities

#### Hosts File Editor
1. Hosts File Editor: User should not be allowed to add entry, enable/disable toggles on existing entry in Hosts File Editor if not running as admin
1. Hosts File Editor: User should be allowed to add entry, enable/disable toggles on existing entry in Hosts File Editor if running as admin
1. Hosts File Editor: Its Settings should persist and work

#### Registry Preview
1. Registry Preview: User should be able to open registry file via Open button and visualize registry file
1. Registry Preview: Notepad should open whenc "edit" button is clicked
1. Registry Preview: RegEdit should open when "Open Registry Editor" is clicked
1. Registry Preview: Selecting key from the visualized tree and clicking on "open key" should  open that key in RegEdit

#### Environment Variables
1. Environment Variables: User should not be able to add "system" variable if not running as admin
1. Environment Variables: User should be able to add/edit user/system environment variable
1. Environment Variables: Adding/Editing env variable from Windows should show "variabled have been modified, please reload" banner and button in Environment Variables Editor