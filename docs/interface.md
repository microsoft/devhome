# Interface definition

```cs
public abstract class ToolPage : Page
{
    public abstract string ShortName { get; }
}
```

# Runtime logic

The Dev Home framework will look at all types in its assembly for any inheriting from ToolPage:

On a found type, the framework will use:
  - [`ShortName`](#ShortName) to get the name of the tool,

# Method definition

This section contains a more detailed description of each of the interface methods.

## ShortName

```cs
public abstract string ShortName { get; }
```

Returns the name of the tool.  This is used for the navigation menu text.

# Code organization

### [`toolpage.cs`](/Common/ToolPage.cs)
Contains the interface definition for Dev Home tools.