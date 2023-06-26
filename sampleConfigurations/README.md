## Using the sample configurations

Sample configurations have been provided for various GitHub repositories. These configurations ideally should be placed in a `.configurations` folder in the root of the project directory. Some DSC resources may have parameters that allow you to pass in a relative file path. The reserved variable `$(WinGetConfigRoot)` can be used to specify the full path of the configuration file. An example of how to use that variable with a relative file path is shown below:

```yaml
    - resource: Microsoft.VisualStudio.DSC/VSComponents
      dependsOn:
      directives:
        description: Install required VS workloads from .vsconfig file
        allowPrerelease: true
      settings:
        productId: Microsoft.VisualStudio.Product.Community
        channelId: VisualStudio.17.Release
        vsConfigFile: '${WinGetConfigRoot}\..\.vsconfig'
```