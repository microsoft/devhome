// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.DevDiagnostics.Contracts.ViewModels;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.DevDiagnostics.Services;

// Similar to DevHome.Services.NavigationService
internal sealed class DDNavigationService : INavigationService
{
    private readonly IPageService pageService;
    private object? lastParameterUsed;
    private Frame? frame;
    private string? defaultPage;

    public object? LastParameterUsed => lastParameterUsed;

    public event NavigatedEventHandler? Navigated;

    public Frame? Frame
    {
        get
        {
            if (frame == null)
            {
                var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
                frame = barWindow?.GetFrame();
                if (frame is not null)
                {
                    RegisterFrameEvents();
                }
            }

            return frame;
        }

        set
        {
            UnregisterFrameEvents();
            frame = value;
            RegisterFrameEvents();
        }
    }

    public string DefaultPage
    {
        get => defaultPage ?? typeof(AppDetailsPageViewModel).FullName ?? string.Empty;
        set => defaultPage = value;
    }

    [MemberNotNullWhen(true, nameof(Frame), nameof(frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    [MemberNotNullWhen(true, nameof(Frame), nameof(frame))]
    public bool CanGoForward => Frame != null && Frame.CanGoForward;

    public DDNavigationService(IPageService pageService)
    {
        this.pageService = pageService;
    }

    private void RegisterFrameEvents()
    {
        if (frame != null)
        {
            frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (frame != null)
        {
            frame.Navigated -= OnNavigated;
        }
    }

    public bool GoBack()
    {
        if (CanGoBack)
        {
            var vmBeforeNavigation = GetPageViewModel(frame);
            frame.GoBack();
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }

            return true;
        }

        return false;
    }

    public bool GoForward()
    {
        if (CanGoForward)
        {
            var vmBeforeNavigation = GetPageViewModel(frame);
            frame.GoForward();
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }

            return true;
        }

        return false;
    }

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        var pageType = pageService.GetPageType(pageKey);

        if (frame != null && (frame.Content?.GetType() != pageType || (parameter != null && !parameter.Equals(lastParameterUsed))))
        {
            frame.Tag = clearNavigation;
            var vmBeforeNavigation = GetPageViewModel(frame);
            var navigated = frame.Navigate(pageType, parameter);
            if (navigated)
            {
                lastParameterUsed = parameter;
                if (vmBeforeNavigation is INavigationAware navigationAware)
                {
                    navigationAware.OnNavigatedFrom();
                }
            }

            return navigated;
        }

        return false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var clearNavigation = (bool)frame.Tag;
            if (clearNavigation)
            {
                frame.BackStack.Clear();
            }

            if (GetPageViewModel(frame) is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.Parameter);
            }

            Navigated?.Invoke(sender, e);
        }
    }

    public static object? GetPageViewModel(Frame frame)
    {
        return frame.Content?.GetType().GetProperty("_viewModel")?.GetValue(frame.Content, null);
    }
}
