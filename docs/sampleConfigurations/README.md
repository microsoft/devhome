# Sample configurations

Sample configurations are available in the [winget-dsc](https://github.com/microsoft/winget-dsc/tree/main/samples) repository.

## Using the sample configurations

Download the *.dsc.yaml files to your local system. They can be executed in Dev Home via the "Machine configuration" section. They can also be executed by running `winget configure <path to configuration file>`.

Several DSC resources may require running in administrator mode. If the configuration is leveraging the [WinGet DSC resource](https://www.powershellgallery.com/packages/Microsoft.WinGet.DSC) to install packages, there are also limitations in some cases specific to the installers that may either require or prohibit installation in administrative context.

## Create your own

Writing YAML is a pain. To help you get started creating your own, there is a [sample tool](https://github.com/microsoft/winget-create/blob/main/Tools/WingetCreateMakeDSC.ps1) for authoring in the winget-create repo. It currently only supports adding apps, but give it a try and contribute to make it better!
