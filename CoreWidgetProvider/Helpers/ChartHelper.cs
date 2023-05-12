// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Drawing;
using System.Drawing.Drawing2D;

namespace CoreWidgetProvider.Helpers;

internal class ChartHelper
{
    private static readonly Color DarkGrayColor = Color.FromArgb(30, 213, 224, 247);
    private static readonly Pen ChartPen = new (Color.FromArgb(255, 4, 157, 228));

    private static readonly Brush Brush100 = new LinearGradientBrush(new Point(150, 5), new Point(150, 95), Color.FromArgb(105, 4, 157, 228), Color.FromArgb(65, 117, 125, 133));
    private static readonly Brush Brush50 = new LinearGradientBrush(new Point(150, 50), new Point(150, 95), Color.FromArgb(105, 4, 157, 228), Color.FromArgb(65, 117, 125, 133));
    private static readonly Brush Brush20 = new LinearGradientBrush(new Point(150, 77), new Point(150, 95), Color.FromArgb(105, 4, 157, 228), Color.FromArgb(65, 117, 125, 133));

    private const int MaxChartValues = 30;

    public static string CreateImageUrl(List<float> chartValues)
    {
        var bytes = CreateChart(chartValues);
        var b64String = Convert.ToBase64String(bytes);
        return "data:image/png;base64," + b64String;
    }

    public static byte[] CreateChart(List<float> chartValues)
    {
        var width = 268;
        var height = 100;
        var bitmap = new Bitmap(width, height);
        var points = new List<PointF>();
        using (var g = Graphics.FromImage(bitmap))
        {
            float minHeight = 95;
            var startChartX = 5 + ((30 - chartValues.Count) * 10);
            for (var pointIndex = chartValues.Count - 1; pointIndex >= 0; pointIndex--)
            {
                points.Add(new PointF(startChartX + (10 * pointIndex), 95 - (chartValues[pointIndex] / 100 * 90)));
                minHeight = Math.Min(minHeight, 95 - (chartValues[pointIndex] / 100 * 90));
            }

            using var darkGreyBrush = new SolidBrush(DarkGrayColor);
            g.FillRectangle(darkGreyBrush, 0, 0, width - 1, height - 1);
            g.DrawRectangle(Pens.LightGray, 0, 0, width - 1, height - 1);

            if (chartValues.Count >= 2)
            {
                points.Add(new PointF(startChartX, 95));
                points.Add(new PointF(295, 95));
                points.Add(new PointF(295, 95 - (chartValues.Last() / 100 * 90)));

                if (minHeight >= 77)
                {
                    g.FillPolygon(Brush20, points.ToArray());
                }
                else if (minHeight >= 50)
                {
                    g.FillPolygon(Brush50, points.ToArray());
                }
                else
                {
                    g.FillPolygon(Brush100, points.ToArray());
                }

                for (var pointIndex = 0; pointIndex < points.Count - 4; pointIndex++)
                {
                    g.DrawLine(ChartPen, points[pointIndex].X, points[pointIndex].Y, points[pointIndex + 1].X, points[pointIndex + 1].Y);
                }
            }
        }

        var bytes = BitmapToByteArray(bitmap);
        bitmap.Dispose();
        points.Clear();
        GC.Collect();
        return bytes;
    }

    public static byte[] BitmapToByteArray(Bitmap img)
    {
        using var stream = new MemoryStream();
        img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    public static void AddNextChartValue(float value, List<float> chartValues)
    {
        if (chartValues.Count >= MaxChartValues)
        {
            chartValues.RemoveAt(0);
        }

        chartValues.Add(value);
    }
}
