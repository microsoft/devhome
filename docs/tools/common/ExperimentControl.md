# Experiment control 
Control to show different content based on the experiment key. By default, the default content is shown. If the experiment is enabled, the experiment content is shown.

## Example
```xml
<StackPanel>
  <ExperimentControl ExperimentKey="MyExperiment">

    <!-- If Experiment("MyExperiment") is disabled, the default content is shown -->
    <ExperimentControl.DefaultContent>
      <Button>Button is shown if experiment is disabled</Button>
    </ExperimentControl.DefaultContent>

    <!-- If Experiment("MyExperiment") is enabled, the experiment content is shown -->
    <ExperimentControl.ExperimentContent>
      <TextBlock Text="Text block is shown if experiment is enabled" />
    </ExperimentControl.ExperimentContent>

  </ExperimentControl>
</StackPanel>
```
|