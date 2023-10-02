// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

namespace DevHome.SetupFlow.Controls;

/// <summary>
/// Setup shell class used by the setup flow pages to ensure a consistent
/// end-to-end page layout.
/// </summary>
[ContentProperty(Name = nameof(SetupShellContent))]
public sealed partial class SetupShell : UserControl
{
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object SetupShellContent
    {
        get => (object)GetValue(SetupShellContentProperty);
        set => SetValue(SetupShellContentProperty, value);
    }

    public SetupFlowOrchestrator Orchestrator
    {
        get => (SetupFlowOrchestrator)GetValue(OrchestratorProperty);
        set => SetValue(OrchestratorProperty, value);
    }

    public bool UseOrchestratorTitle => string.IsNullOrEmpty(Title);

    public SetupShell()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(nameof(Title), typeof(string), typeof(SetupShell), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.RegisterAttached(nameof(Description), typeof(string), typeof(SetupShell), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SetupShellContentProperty = DependencyProperty.RegisterAttached(nameof(SetupShellContent), typeof(object), typeof(SetupShell), new PropertyMetadata(null));
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.RegisterAttached(nameof(Header), typeof(object), typeof(SetupShell), new PropertyMetadata(null));
    public static readonly DependencyProperty OrchestratorProperty = DependencyProperty.RegisterAttached(nameof(Orchestrator), typeof(SetupFlowOrchestrator), typeof(SetupShell), new PropertyMetadata(null));

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Focus on the first focusable element inside the shell content
        var element = FocusManager.FindFirstFocusableElement(ShellContent);
        if (element != null)
        {
            await FocusManager.TryFocusAsync(element, FocusState.Programmatic).AsTask();
        }
    }
}
