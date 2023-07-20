## Dev Home UI Testing
### How to run UI tests?
1. From the command prompt, start the Windows Application Driver service:
```cmd
"C:\Program Files\Windows Application Driver\WinAppDriver.exe"
```
2. From Visual Studio, navigate to the "Test Explorer" tab and locate the `DevHome.UITest` set of tests.
3. Select a test and run it.
    - Note: Once the test starts, avoid interacting with your machine (e.g. move mouse, use keyboard) to allow the test to navigate the Dev Home app and execute the test.

### How to configure the application settings for a Dev Home release?
Application settings are a set of configuration properties loaded from `appsettings.*.json` and are available at runtime to the test methods.
For a developer release of Dev Home, the default configuration from `appsettings.json` are used. For Canary and Prod, a
separate application settings JSON file (`appsettings.canary.json` or `appsettings.prod.json`) is available
and its values will overwrite the ones from the default `appsettings.json` file.

Here's an example of the default `appsettings.json`:
```json
{
    "WindowsApplicationDriverUrl": "http://127.0.0.1:4723",
    "PackageFamilyName": "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe",
    "Widget": {
        "IdPrefix": "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe",
        "Provider": "CoreWidgetProvider"
    }
}
```

### Run tests on a different release of Dev Home (Canary, Prod, Dev)
Edit the `Test.runsettings` file and set the appropriate value for `AppSettingsMode`. In the example below `AppSettingsMode` is set to `canary`,
therefore running a UI test will load the default application settings in addition to the canary application settings (latter JSON values overwrite the former JSON values) which will then start
a new instance of 'Dev Home (Canary)'.
```xml
  <TestRunParameters>
      <!--
        Run settings parameters:
        * AppSettingsMode:
            - 'local': Run ui tests using appsettings.json
            - 'canary': Run ui test using appsettings.json and appsettings.canary.json
            - 'prod': Run ui test using appsettings.json and appsettings.prod.json
        -->
      <Parameter name="AppSettingsMode" value="canary" />
  </TestRunParameters>
```
