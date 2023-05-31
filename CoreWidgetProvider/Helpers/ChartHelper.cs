// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Drawing;
using System.Drawing.Drawing2D;

namespace CoreWidgetProvider.Helpers;

internal class ChartHelper
{
    private static readonly Color DarkGrayColor = Color.FromArgb(0, 213, 224, 247);

    private static readonly Pen CPUChartPen = new (Color.FromArgb(255, 57, 184, 227));
    private static readonly Pen GPUChartPen = new (Color.FromArgb(255, 222, 104, 242));
    private static readonly Pen MemChartPen = new (Color.FromArgb(255, 92, 158, 250));
    private static readonly Pen NetChartPen = new (Color.FromArgb(255, 245, 98, 142));
    private static readonly Pen DiskChartPen = new (Color.FromArgb(255, 33, 139, 139));

    private static readonly Brush CPUBrush100 = new LinearGradientBrush(new Point(150, 5), new Point(150, 95), Color.FromArgb(105, 57, 184, 227), Color.FromArgb(65, 0, 86, 110));
    private static readonly Brush CPUBrush50 = new LinearGradientBrush(new Point(150, 50), new Point(150, 95), Color.FromArgb(105, 57, 184, 227), Color.FromArgb(65, 0, 86, 110));
    private static readonly Brush CPUBrush20 = new LinearGradientBrush(new Point(150, 77), new Point(150, 95), Color.FromArgb(105, 57, 184, 227), Color.FromArgb(65, 0, 86, 110));

    private static readonly Brush GPUBrush100 = new LinearGradientBrush(new Point(150, 5), new Point(150, 95), Color.FromArgb(105, 222, 104, 242), Color.FromArgb(65, 125, 0, 138));
    private static readonly Brush GPUBrush50 = new LinearGradientBrush(new Point(150, 50), new Point(150, 95), Color.FromArgb(105, 222, 104, 242), Color.FromArgb(65, 125, 0, 138));
    private static readonly Brush GPUBrush20 = new LinearGradientBrush(new Point(150, 77), new Point(150, 95), Color.FromArgb(105, 222, 104, 242), Color.FromArgb(65, 125, 0, 138));

    private static readonly Brush MemBrush100 = new LinearGradientBrush(new Point(150, 5), new Point(150, 95), Color.FromArgb(105, 92, 158, 250), Color.FromArgb(65, 0, 34, 92));
    private static readonly Brush MemBrush50 = new LinearGradientBrush(new Point(150, 50), new Point(150, 95), Color.FromArgb(105, 92, 158, 250), Color.FromArgb(65, 0, 34, 92));
    private static readonly Brush MemBrush20 = new LinearGradientBrush(new Point(150, 77), new Point(150, 95), Color.FromArgb(105, 92, 158, 250), Color.FromArgb(65, 0, 34, 92));

    private static readonly Brush NetBrush100 = new LinearGradientBrush(new Point(150, 5), new Point(150, 95), Color.FromArgb(105, 245, 98, 142), Color.FromArgb(65, 130, 0, 47));
    private static readonly Brush NetBrush50 = new LinearGradientBrush(new Point(150, 50), new Point(150, 95), Color.FromArgb(105, 245, 98, 142), Color.FromArgb(65, 130, 0, 47));
    private static readonly Brush NetBrush20 = new LinearGradientBrush(new Point(150, 77), new Point(150, 95), Color.FromArgb(105, 245, 98, 142), Color.FromArgb(65, 130, 0, 47));

    private static readonly Brush DiskBrush100 = new LinearGradientBrush(new Point(150, 5), new Point(150, 95), Color.FromArgb(105, 33, 139, 139), Color.FromArgb(65, 31, 108, 108));
    private static readonly Brush DiskBrush50 = new LinearGradientBrush(new Point(150, 50), new Point(150, 95), Color.FromArgb(105, 33, 139, 139), Color.FromArgb(65, 31, 108, 108));
    private static readonly Brush DiskBrush20 = new LinearGradientBrush(new Point(150, 77), new Point(150, 95), Color.FromArgb(105, 33, 139, 139), Color.FromArgb(65, 31, 108, 108));

    private const int MaxChartValues = 30;

    private static readonly object _lock = new ();

    public static string CreateImageUrl(List<float> chartValues, string type)
    {
        var bytes = CreateChart(chartValues, type);
        var b64String = Convert.ToBase64String(bytes);
        return "data:image/png;base64," + b64String;
    }

    public static byte[] CreateChart(List<float> chartValues, string type)
    {
        var width = 268;
        var height = 100;
        var bitmap = new Bitmap(width, height);
        var points = new List<PointF>();
        var chartPen = CPUChartPen;
        var brush20 = CPUBrush20;
        var brush50 = CPUBrush50;
        var brush100 = CPUBrush100;

        using (var g = Graphics.FromImage(bitmap))
        {
            float minHeight = 95;
            var startChartX = 5 + ((30 - chartValues.Count) * 10);

            for (var pointIndex = chartValues.Count - 1; pointIndex >= 0; pointIndex--)
            {
                points.Add(new PointF(startChartX + (10 * pointIndex), 95 - (chartValues[pointIndex] / 100 * 90)));
                minHeight = Math.Min(minHeight, 95 - (chartValues[pointIndex] / 100 * 90));
            }

            lock (_lock)
            {
                using var darkGreyBrush = new SolidBrush(DarkGrayColor);
                g.FillRectangle(darkGreyBrush, 0, 0, width - 1, height - 1);
                g.DrawRectangle(Pens.LightGray, 0, 0, width - 1, height - 1);

                if (chartValues.Count >= 2)
                {
                    if (type == "gpu")
                    {
                        chartPen = GPUChartPen;
                        brush20 = GPUBrush20;
                        brush50 = GPUBrush50;
                        brush100 = GPUBrush100;
                    }
                    else if (type == "mem")
                    {
                        chartPen = MemChartPen;
                        brush20 = MemBrush20;
                        brush50 = MemBrush50;
                        brush100 = MemBrush100;
                    }
                    else if (type == "net")
                    {
                        chartPen = NetChartPen;
                        brush20 = NetBrush20;
                        brush50 = NetBrush50;
                        brush100 = NetBrush100;
                    }
                    else if (type == "disk")
                    {
                        chartPen = DiskChartPen;
                        brush20 = DiskBrush20;
                        brush50 = DiskBrush50;
                        brush100 = DiskBrush100;
                    }

                    points.Add(new PointF(startChartX, 95));
                    points.Add(new PointF(295, 95));
                    points.Add(new PointF(295, 95 - (chartValues.Last() / 100 * 90)));

                    if (minHeight >= 77)
                    {
                        g.FillPolygon(brush20, points.ToArray());
                    }
                    else if (minHeight >= 50)
                    {
                        g.FillPolygon(brush50, points.ToArray());
                    }
                    else
                    {
                        g.FillPolygon(brush100, points.ToArray());
                    }

                    for (var pointIndex = 0; pointIndex < points.Count - 4; pointIndex++)
                    {
                        g.DrawLine(chartPen, points[pointIndex].X, points[pointIndex].Y, points[pointIndex + 1].X, points[pointIndex + 1].Y);
                    }
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
