// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.Common.Helpers;

public static class AdaptiveCardHelpers
{
    // convert base64 string to image that can be used in a imageIcon control
    public static ImageIcon ConvertBase64StringToImageSource(string base64String)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64String);
            using var ms = new MemoryStream(bytes);
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(ms.AsRandomAccessStream());
            var icon = new ImageIcon() { Source = bitmapImage };
            return icon;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Failed to load image icon", ex);
            return new ImageIcon();
        }
    }
}
