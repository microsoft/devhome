# Set up environments Tests
If your code affects the environment tool page or directly affects the "set up an environment flow", please manually verify these scenarios still work.
There is shared code between the environments page and both the create environment and set up environment flows in Machine configuration

## Scenarios
Please make sure to verify all these scenarios.

### In a fresh Win 10/ Win 11 VM verify (if not already done so in another scenario):
1. Navigation to the "set up environments" flow in the Machine configuration page for the first time should show error info bar with button to add user to the Hyper-V admin group and enable Hyper-V.
    1. Clicking button in infobar shows UAC and clicking yes add user to the Hyper-V admin group and enables Hyper-V
    1. A warning info bar should appear on the bottom right of the page requesting the user to reboot.
    1. Clicking the sync button regardless of whether the user closes the warning info bar or not should regenerate the warning infobar requesting to reboot
    1. Clicking the reboot button reboots machine
    1. After logging in again no warning info bar is shown
    1. After logging in a "Call to action" button is shown directing the user to the extensions library if no extensions are installed that support environments
    1. After logging in a "Call to action" button is shown directing the user to create a new environment if extensions are installed that support environments but no environments have been created.
    
#### Make sure Hyper-V extension is enabled and verify the following
1. Users can navigate to Machine configuration > Setup an environment and select only one environment.
1. Users can sort environments by ascending and descending order by name
1. Users can filter environments by providers. 
    1. Clicking a provider in the providers combo box only shows environments for that specific provider
1. Clicking the "All" option in the providers combo box shows all environments from all providers.
1. The "Create environment" button redirects the user to the create environment flow in Machine configuration
1. Typing the name of an environment in the filter box in the environments page filters the environments by name. E.g if you have two environments and one is named "test" and the other "prod". When you type "te" only the 
environment with the name "test" should appear in the UI.
1. Performing an operation e.g starting a stopped environment in the environments tool page should also change the state of the environment card in the set up environment flows initial page.
1. Users can click the next button only after selecting an environment in the setup environments initial page. The next page should be the clone repository page.
1. Users can add a repository via a url in the clone repositories page and click next to move to application management page
1. Users can add an application in the application management page and click next to move to the review page
1. Users can view the selected environment, repository to be cloned and the application to be installed on the review page
1. Users can start the set up after clicking they agree and is redirected to the loading page.
#### Setting up Hyper-V VM with Dev Setup Agent.
1. Note, once configuration starts Dev Home will send our DevSetupAgent service and other binaries over to the VM this shouldn't take more than a minute or two. However, we also update WinGet on the VM as well. This only happens the first time we set up a VM, so if this feels like its taking too long before you see progress updates in the UI, file a bug just in case.

##### Scenario 1: User is not logged into VM
1. An adaptive card should be shown in the loading pages "action center" requesting for the users VM credentials. (VM should already have a with username and password)
1. If prompted  only 3 attempts allowed before configuration fails. Test that providing correct credentials removes adaptive card and you are
not prompted with another adaptive card
1. Once correct credentials are received, the user should receive another adaptive card requesting that they log into the VM
1. Once the user is logged into the VM, the setup should start. Confirm that within 2 minutes configuration progress is presented in the UI
1. If there are no errors, the user should be redirected to the summary page where they can see a list of repositories cloned and apps installed
1. Confirm the repositories were cloned to the correct place and apps now installed on the VM
##### Scenario 2: User is logged into VM (Locked screen)
1. An adaptive card requesting that they log into the VM should be seen in the loading pages "action center"
1. Once the user is logged into the VM, the Setup should start and our binaries should be transferred onto the VM. Confirm that within 2 minutes configuration progress is presented in the UI
1. If there are no errors, the user should be redirected to the summary page where they can see a list of repositories cloned and apps installed
1. Confirm the repositories were cloned to the correct place and apps now installed on the VM
##### Scenario 3: User is logged into VM (active session)
1. Confirm that within 2 minutes configuration progress is presented in the UI
1. If there are no errors, the user should be redirected to the summary page where they can see a list of repositories cloned and apps installed
1. Confirm the repositories were cloned to the correct place and apps now installed on the VM