// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Xaml.Interactivity;

namespace DevHome.Common.Behaviors;

/// <summary>
/// A behavior that renders http and https hyperlinks in a <see cref="TextBlock"/>.
/// </summary>
public partial class RenderWebHyperlinksBehavior : Behavior<TextBlock>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetInlineCollection(ToInline(AssociatedObject.Text));
    }

    private List<Inline> ToInline(string text)
    {
        List<Inline> elements = [];
        if (string.IsNullOrEmpty(text))
        {
            return elements;
        }

        foreach (var part in WebUrlRegex().Split(text))
        {
            if (Uri.IsWellFormedUriString(part, UriKind.Absolute))
            {
                elements.Add(CreateHyperlink(part));
            }
            else
            {
                elements.Add(CreateRun(part));
            }
        }

        return elements;
    }

    /// <summary>
    /// Create a <see cref="Run"/> element.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <returns>The <see cref="Run"/> element.</returns>
    protected Run CreateRun(string text) => new() { Text = text };

    /// <summary>
    /// Create a <see cref="Hyperlink"/> element.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <returns>The <see cref="Hyperlink"/> element.</returns>
    protected Hyperlink CreateHyperlink(string text)
    {
        Hyperlink hyperlink = new()
        {
            NavigateUri = new Uri(text),
        };
        hyperlink.Inlines.Add(CreateRun(text));
        return hyperlink;
    }

    /// <summary>
    /// Set the <see cref="TextBlock.Inlines"/> collection.
    /// </summary>
    /// <param name="inlineCollection">The collection of <see cref="Inline"/> elements.</param>
    protected void SetInlineCollection(IEnumerable<Inline> inlineCollection)
    {
        AssociatedObject.Inlines.Clear();
        foreach (var inline in inlineCollection)
        {
            AssociatedObject.Inlines.Add(inline);
        }
    }

    [GeneratedRegex(@"(https?://[^\s]+)")]
    private static partial Regex WebUrlRegex();
}
