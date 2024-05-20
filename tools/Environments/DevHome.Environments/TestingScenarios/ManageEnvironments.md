# Manage environments Tests
If your code affects the Environments tool page, please manually verify these scenarios.

## Scenarios
Please make sure to verify all these scenarios.

### In a fresh Win 10/ Win 11 VM verify (if not already done so in another scenario):
1. Navigation to the environments page for the first time should show error info bar with button to add user to the Hyper-V admin group and enable Hyper-V.
    1. Clicking button in infobar shows UAC and clicking yes add user to the Hyper-V admin group and enables Hyper-V
        1. A warning info bar should appear on the bottom right of the page requesting the user to reboot.
        1. Clicking the sync button regardless of whether the user closes the warning info bar or not should regenerate the warning infobar requesting to reboot
        1. Clicking the reboot button reboots machine
        1. After logging in again no warning info bar is shown
        1. After logging in a "Call to action" button is shown directing the user to the extensions library if no extensions are installed that support environments
        1. After logging in a "Call to action" button is shown directing the user to create a new environment if extensions are installed that support environments but no environments have been created.

#### Management of an extension
We'll use the Hyper-V as an example as this extensions comes with Dev Home. But for any other extension, you can test the basics of launching, starting and stopping. For these confirm that
the environment indeterminate progress bar is not active indefinitely (more than a few minutes to be sure as it is extension specific and appears based on the state of the environment). For example
the Dev Box provider can take a minute or two to fully start, but shouldn't take 5 to 10 minutes.

1. Users can see their virtual machines when Navigating to the Environments tool page for the first time after being added to Hyper-V admin group while hyper-V feature is enabled
1. Users can navigate to the extensions library and turn the Hyper-V extension on/off
1. When Hyper-V extension is toggled off and the user clicks the sync button in the environment page no Hyper-V environments should appear
1. Hyper-V extension allows users to launch into their VMs by clicking the launch button
1. Hyper-V extension allows users to start their VMs by clicking the start button in the menu flyout of the launch split button (This should only be available when VM in stopped, paused or saved state)
1. Hyper-V extension allows users to stop their VMs by clicking the stop button in the menu flyout of the launch split button (This should only be available when VM in running state)
1. Hyper-V extension allows users to pause their VMs by clicking the pause button in the menu flyout of the launch split button (This should only be available when VM in running state)
1. Hyper-V extension allows users to save the VMs state by clicking the save button in the menu flyout of the launch split button (This should only be available when VM in running state)
1. Hyper-V extension allows users to restart their VMs by clicking the restart button in the menu flyout of the three dots button (This should only be available when VM in running state)
1. Hyper-V extension allows users to delete their VMs by clicking the delete button in the menu flyout of the three dots button (This should only be available when VM in stopped or saved state)
    1. Users see a content dialog to confirm their intention to delete the environment
    1. Cancelling content dialog should do nothing
    1. Confirming and deleting the environment should remove it from the environments page once it is done.
1. Hyper-V extension allows users to resume their VMs state from a paused state by clicking the resume button in the menu flyout of the launch split button (This should only be available when VM in paused state)
1. Hyper-V extension allows users to turn off their VMs by clicking the terminate button in the menu flyout of the launch split button (This should only be available when VM in running or paused state)
1. Hyper-V extension allows users to create a checkpoint for their VMs by clicking the checkpoint button in the menu flyout of the launch split button (This should only be available when VM in running, stopped and saved states)
1. Properties of the environment cards are updated after an operation succeeds
1. Performing any operation listed above on from the environment card does not end in a progress bar that does not stop when the operation is complete
1. Users can sort environments by ascending and descending order by name
1. Users can filter environments by providers. 
    1. Clicking a provider in the providers combo box only shows environments for that specific provider
1. Clicking the "All" option in the providers combo box shows all environments from all providers.
1. The "Create environment" button redirects the user to the create environment flow in Machine configuration
1. Typing the name of an environment in the filter box in the environments page filters the environments by name. E.g if you have two environments and one is named "test" and the other "prod". When you type "te" only the 
environment with the name "test" should appear in the UI.