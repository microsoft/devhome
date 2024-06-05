// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Used to shuttle information between RepoConfigView and AddRepoDialog.
/// Specifically this contains all information needed to clone repositories to a user's machine.
/// </summary>
public partial class CloningInformation : ObservableObject, IEquatable<CloningInformation>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CloningInformation));

    // Use git icons for the generic provider.
    private static readonly BitmapImage LightGit = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/GitLight.png"));

    private static readonly BitmapImage DarkGit = new(new Uri("ms-appx:///DevHome.SetupFlow/Assets/GitDark.png"));

    /// <summary>
    /// Gets a value indicating whether the repo is a private repo.  If changed to "IsPublic" the
    /// values of the converters in the views need to change order.
    /// </summary>
    public bool IsPrivate => RepositoryToClone.IsPrivate;

    /// <summary>
    /// Gets or sets the repository the user wants to clone.
    /// </summary>
    public IRepository RepositoryToClone
    {
        get; set;
    }

    /// <summary>
    /// Full path the user wants to clone the repository.
    /// </summary>
    [ObservableProperty]
    private DirectoryInfo _cloningLocation;

    /// <summary>
    /// Gets or sets the account that owns the repository.
    /// </summary>
    public IDeveloperId OwningAccount
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the name of the repository provider that the user used to log into their account.
    /// </summary>
    public string ProviderName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the repository is to be cloned on a Dev Drive.
    /// </summary>
    public bool CloneToDevDrive
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether used specifically for telemetry to get insights if repos are being cloned to an existing devdrive.
    /// </summary>
    public bool CloneToExistingDevDrive
    {
        get; set;
    }

    [ObservableProperty]
    private BitmapImage _repositoryTypeIcon;

    private BitmapImage GetGitIcon(ElementTheme theme)
    {
        BitmapImage gitIcon;
        if (theme == ElementTheme.Dark)
        {
            gitIcon = DarkGit;
        }
        else
        {
            gitIcon = LightGit;
        }

        RepositoryTypeIcon = gitIcon;

        return gitIcon;
    }

    /// <summary>
    /// Sets RepositoryTypeIcon according to the theme.
    /// </summary>
    /// <param name="theme">The theme to use to determine the icon.</param>
    /// <remarks>
    /// This is for monochrome icons only.
    /// Additionally, this assumes the icon coming from the extension is colored for the dark theme.
    /// If the provider is null (Generic repo) or if the icon is null, the git icon is used instead.
    /// </remarks>
    public void SetIcon(ElementTheme theme)
    {
        // RepositoryProvider can be null in the case of URL cloning.
        if (RepositoryProvider == null || RepositoryProvider.Icon == null)
        {
            RepositoryTypeIcon = GetGitIcon(theme);
            return;
        }

        BitmapDecoder decoder;

        try
        {
            decoder = BitmapDecoder.CreateAsync(RepositoryProvider.Icon.OpenReadAsync().AsTask().Result).AsTask().Result;
        }
        catch (Exception e)
        {
            _log.Error(e, e.Message);
            RepositoryTypeIcon = GetGitIcon(theme);
            return;
        }

        // Get the pixel data as a byte array
        PixelDataProvider pixelData = decoder.GetPixelDataAsync().AsTask().Result;
        var pixels = pixelData.DetachPixelData();

        // Assume that the icon is colored for dark mode.
        if (theme != ElementTheme.Dark)
        {
            // Loop through the pixels and toggle black to white and vice versa
            // Assuming the pixel format is BGRA8
            for (var i = 0; i < pixels.Length; i += 4)
            {
                // Get the color components of the pixel
                var b = pixels[i];
                var g = pixels[i + 1];
                var r = pixels[i + 2];

                // Check if the pixel is black or white
                if (r == 0 && g == 0 && b == 0)
                {
                    // Change black to white
                    pixels[i] = 255;
                    pixels[i + 1] = 255;
                    pixels[i + 2] = 255;
                }
                else if (r == 255 && g == 255 && b == 255)
                {
                    // Change white to black
                    pixels[i] = 0;
                    pixels[i + 1] = 0;
                    pixels[i + 2] = 0;
                }
            }
        }

        var reversedStream = new InMemoryRandomAccessStream();
        var encoder = BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, reversedStream).AsTask().Result;
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, decoder.PixelWidth, decoder.PixelHeight, decoder.DpiX, decoder.DpiY, pixels);
        encoder.FlushAsync().AsTask().Wait();

        RepositoryTypeIcon.SetSource(reversedStream);
    }

    /// <summary>
    /// Gets or sets a value indicating the alias associated with the Dev drive in the form of
    /// "Drive label" (Drive letter:) [Size GB/TB]. E.g Dev Drive (D:) [50.0 GB]
    /// </summary>
    [ObservableProperty]
    private string _cloneLocationAlias;

    /// <summary>
    /// Gets the repo name and formats it for the Repo Review view.
    /// </summary>
    public string RepositoryId => $"{RepositoryToClone.DisplayName ?? string.Empty}";

    /// <summary>
    /// Gets the repository in a [owning account name]\[reponame] style
    /// </summary>
    public string RepositoryOwnerAndName => Path.Join(RepositoryToClone.OwningAccountName ?? string.Empty, RepositoryToClone.DisplayName);

    /// <summary>
    /// Gets the clone path the user wants to clone the repo to.
    /// </summary>
    public string ClonePath
    {
        get
        {
            var path = CloningLocation.FullName;

            if (RepositoryToClone != null)
            {
                path = Path.Join(path, RepositoryToClone.DisplayName);
            }

            return path;
        }
    }

    /// <summary>
    /// Gets or sets the name of the button that allows a user to edit the clone path of a repository.
    /// This name can't be static because each button name needs to be unique.  Because each name needs to be unique
    /// the name is stored here so it can be set at the time when a unique name can be made.
    /// </summary>
    public string EditClonePathAutomationName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the string that the narrator should say when the repo is selected in the config screen.
    /// </summary>
    public string RepoConfigAutomationName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the name of the button that allows a user to remove the repository from being cloned.
    /// This name can't be static because each button name needs to be unique.  Because each name needs to be unique
    /// the name is stored here so it can be set at the time when a unique name can be made.
    /// </summary>
    public string RemoveFromCloningAutomationName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the provider to use to clone the repository
    /// </summary>
    public IRepositoryProvider RepositoryProvider
    {
        get => _repositoryProvider;

        set
        {
            var iconStream = value.Icon.OpenReadAsync().AsTask().Result;

            RepositoryTypeIcon = new BitmapImage();
            RepositoryTypeIcon.SetSource(iconStream);

            _repositoryProvider = value;
        }
    }

    private IRepositoryProvider _repositoryProvider;

    public string RepositoryProviderDisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(_repositoryProviderDisplayName))
            {
                try
                {
                    // This can be null in the case of URL cloning.
                    _repositoryProviderDisplayName = RepositoryProvider?.DisplayName ?? "git";
                }
                catch (Exception e)
                {
                    _log.Error(e, e.Message);
                }
            }

            return _repositoryProviderDisplayName;
        }
    }

    private string _repositoryProviderDisplayName = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloningInformation"/> class.
    /// Public constructor for XAML view to construct a CloningInformation
    /// </summary>
    public CloningInformation()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloningInformation"/> class.
    /// </summary>
    /// <param name="repoToClone">The repo to clone</param>
    public CloningInformation(IRepository repoToClone)
    {
        RepositoryToClone = repoToClone;
    }

    /// <summary>
    /// Compares two CloningInformations for equality.
    /// </summary>
    /// <param name="other">The CloningInformation to compare to.</param>
    /// <returns>True if equal.</returns>
    /// <remarks>
    /// ProviderName, and RepositoryToClone are used for equality.
    /// </remarks>
    public bool Equals(CloningInformation other)
    {
        if (other == null)
        {
            return false;
        }

        return ProviderName.Equals(other.ProviderName, StringComparison.OrdinalIgnoreCase) &&
            RepositoryToClone.OwningAccountName.Equals(other.RepositoryToClone.OwningAccountName, StringComparison.OrdinalIgnoreCase) &&
            RepositoryToClone.DisplayName.Equals(other.RepositoryToClone.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as CloningInformation);
    }

    public override int GetHashCode()
    {
        return (ProviderName + RepositoryToClone.DisplayName).GetHashCode();
    }
}
