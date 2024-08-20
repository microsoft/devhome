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
        SetInlineCollection(CreateInlines(AssociatedObject.Text));
    }

    /// <summary>
    /// Create a collection of <see cref="Inline"/> elements.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The collection of <see cref="Inline"/> elements.</returns>
    private List<Inline> CreateInlines(string text)
    {
        List<Inline> elements = [];
        if (string.IsNullOrEmpty(text))
        {
            return elements;
        }

        foreach (var part in WebUrlRegex().Split(text))
        {
            if (Uri.TryCreate(part, UriKind.Absolute, out var uri))
            {
                elements.Add(CreateHyperlink(part, uri));
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
    private Run CreateRun(string text) => new() { Text = text };

    /// <summary>
    /// Create a <see cref="Hyperlink"/> element.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="uri">The URI to navigate to.</param>
    /// <returns>The <see cref="Hyperlink"/> element.</returns>
    private Hyperlink CreateHyperlink(string text, Uri uri)
    {
        Hyperlink hyperlink = new()
        {
            NavigateUri = uri,
        };
        hyperlink.Inlines.Add(CreateRun(text));
        return hyperlink;
    }

    /// <summary>
    /// Set the <see cref="TextBlock.Inlines"/> collection.
    /// </summary>
    /// <param name="inlineCollection">The collection of <see cref="Inline"/> elements.</param>
    private void SetInlineCollection(IEnumerable<Inline> inlineCollection)
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
