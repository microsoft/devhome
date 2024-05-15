# AddCreateButton
Create a button with a + symbol and the given text content. Used for buttons that add or create something.

## Usage
### Example
#### HelloWorldDialog.xaml
```xml
  <views:AddCreateButton
      AutomationProperties.AutomationId="AddRepositoriesButton"
      x:Uid="MainPage_RepoReview_AddRepository"
      Command="{x:Bind AddRepoCommand}" />
```
