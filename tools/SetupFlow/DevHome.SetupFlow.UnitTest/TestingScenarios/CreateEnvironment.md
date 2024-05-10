# Create environment Tests
If your code affects the environment tool page or directly affects the "Create environment flow", please manually verify these scenarios still work.
There is shared code between the environments page and both the create environment and set up environment flows in Machine configuration

## Scenarios
Please make sure to verify all these scenarios.

### In a fresh Win 10/ Win 11 VM verify (if not already done so in another scenario):
1. Navigation to the "set up environments" flow in the Machine configuration page for the first time should show error info bar with button to add user to the Hyper-V admin group and enable Hyper-V.
    1. Clicking button in infobar shows UAC and clicking yes add user to the Hyper-V admin group and enables Hyper-V
    1. A Warning info bar should appear requesting the user to reboot.
    1. Clicking the sync button regardless of whether the user closes the warning info bar or not should regenerate the warning infobar requesting to reboot
    1. Clicking the reboot button reboots machine
    1. After logging in again no warning info bar is shown
    1. After logging in a "Call to action" button is shown directing the user to the extensions library if no extensions are installed that support environments
    1. After logging in a "Call to action" button is shown directing the user to create a new environment if extensions are installed that support environments but no environments have been created.

#### Make sure Hyper-V extension is enabled and verify the following
1. Users can navigate to Machine configuration > Create environment and the Hyper-V provider appears and can be selected
1. Users can click the next button after selecting Hyper-V and an adaptive card with a name and list of operating system images appear.
1. Users can type a name into the text box
1. Users can click the more info button on an item to launch a content dialog that displays information about the image
1. Users can select an operating system image and select next
1. Users can review their selected options in the review page
1. Users can select and invoke the "Create environment button"
1. Users are redirected to the machine configuration summary page after creation is started.
1. Users can click the done button to go back to the machine configuration page and then click the "environments" tool page to view the progress of the creation in an environment card
    1. Users can also click the "Go to environments page" button to be redirected to the environments tool page to view the progress of the creation in an environment card
### Once creation is started via the create environment flow
1. In the environments tool page, once creation is complete the UI should stop the progress bar and the environment card should be updated with the latest state of the environment. (For Hyper-V the state should be off)
1. If not already done so, navigate to [Managing Environments](tools/Environments/DevHome.Environments/TestingScenarios/ManageEnvironments.md) to perform the managing environment steps.


