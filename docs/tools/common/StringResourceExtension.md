# String resource extension
This markup extension allows to bind a string resource to a control's property.

## Example
```xml
<UserControl
  ...
  xmlns:mu="using:DevHome.Common.Markups">

  <!-- Localized add button: [ Add ] -->
  <Button Content="{mu:StringResource Name=AddApplication, Source=SetupFlow}" />

</UserControl
```