# RenderWebHyperlinksBehavior behavior
A behavior that can be attached to a `TextBlock` control to render `http[s]` hyperlinks in the text. The behavior uses a regular expression to find hyperlinks in the text and converts them to clickable hyperlinks.

## Example
```xml
<TextBlock Text="This is a hyperlink: https://www.microsoft.com">
    <i:Interaction.Behaviors>
        <behaviors:RenderWebHyperlinksBehavior />
    </i:Interaction.Behaviors>
</TextBlock>
```