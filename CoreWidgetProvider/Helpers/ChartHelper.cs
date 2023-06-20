// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text;
using System.Xml.Linq;

namespace CoreWidgetProvider.Helpers;

internal class ChartHelper
{
    public enum ChartType
    {
        CPU,
        GPU,
        Mem,
        Net,
    }

    private static readonly string LightGrayBoxStyle = "fill:none;stroke:lightgrey;stroke-width:1";

    private static readonly string CPULineStyle = "fill:none;stroke:rgb(57,184,227);stroke-width:1";
    private static readonly string GPULineStyle = "fill:none;stroke:rgb(222,104,242);stroke-width:1";
    private static readonly string MemLineStyle = "fill:none;stroke:rgb(92,158,250);stroke-width:1";
    private static readonly string NetLineStyle = "fill:none;stroke:rgb(245,98,142);stroke-width:1";

    private static readonly string FillStyle = "fill:url(#gradientId);stroke:transparent";

    private static readonly string CPUBrushStop1Style = "stop-color:rgb(57,184,227);stop-opacity:0.4";
    private static readonly string CPUBrushStop2Style = "stop-color:rgb(0,86,110);stop-opacity:0.25";

    private static readonly string GPUBrushStop1Style = "stop-color:rgb(222,104,242);stop-opacity:0.4";
    private static readonly string GPUBrushStop2Style = "stop-color:rgb(125,0,138);stop-opacity:0.25";

    private static readonly string MemBrushStop1Style = "stop-color:rgb(92,158,250);stop-opacity:0.4";
    private static readonly string MemBrushStop2Style = "stop-color:rgb(0,34,92);stop-opacity:0.25";

    private static readonly string NetBrushStop1Style = "stop-color:rgb(245,98,142);stop-opacity:0.4";
    private static readonly string NetBrushStop2Style = "stop-color:rgb(130,0,47);stop-opacity:0.25";

    private const int MaxChartValues = 30;

    private static readonly object _lock = new ();

    public static string CreateImageUrl(List<float> chartValues, ChartType type)
    {
        var chartStr = CreateChart(chartValues, type);
        var bytes = Encoding.UTF8.GetBytes(chartStr);
        var b64String = Convert.ToBase64String(bytes);
        return "data:image/svg+xml;base64," + b64String;
    }

    public static string CreateChart(List<float> chartValues, ChartType type)
    {
        /* // Values to use for testing when a static image is desired.
        chartValues.Clear();
        chartValues = new List<float>
        {
            10, 30, 20, 40, 30, 50, 40, 60, 50, 100,
            10, 30, 20, 40, 30, 50, 40, 60, 50, 70,
            0, 30, 20, 40, 30, 50, 40, 60, 50, 70,
        }; */

        var height = 102;
        var width = 264;

        var chartDoc = new XDocument();

        lock (_lock)
        {
            // The SVG is made of three shapes:
            // * a colored line, plotting the points on the graph
            // * a transparent line, outlining the gradient under the graph
            // * a grey box, outlining the entire image
            // The SVG also contains a definition for the fill gradient.
            var svgElement = CreateBlankSvg(height, width);

            // Create the line that will show the points on the graph.
            var lineElement = new XElement("polyline");
            var points = TransformPointsToLine(chartValues, out var startX, out var finalX);
            lineElement.SetAttributeValue("points", points.ToString());
            lineElement.SetAttributeValue("style", GetLineStyle(type));

            // Create the line that will contain the gradient fill.
            TransformPointsToLoop(points, startX, finalX);
            var fillElement = new XElement("polyline");
            fillElement.SetAttributeValue("points", points.ToString());
            fillElement.SetAttributeValue("style", FillStyle);

            // Add the gradient definition and the three shapes to the svg.
            svgElement.Add(CreateGradientDefinition(type));
            svgElement.Add(fillElement);
            svgElement.Add(lineElement);
            svgElement.Add(CreateBorderBox(height, width));

            chartDoc.Add(svgElement);
        }

        return chartDoc.ToString();
    }

    private static XElement CreateBlankSvg(int height, int width)
    {
        var svgElement = new XElement("svg");
        svgElement.SetAttributeValue("height", height);
        svgElement.SetAttributeValue("width", width);
        return svgElement;
    }

    private static XElement CreateGradientDefinition(ChartType type)
    {
        var defsElement = new XElement("defs");
        var gradientElement = new XElement("linearGradient");

        // Vertical gradients are created when x1 and x2 are equal and y1 and y2 differ.
        gradientElement.SetAttributeValue("x1", "0%");
        gradientElement.SetAttributeValue("x2", "0%");
        gradientElement.SetAttributeValue("y1", "0%");
        gradientElement.SetAttributeValue("y2", "100%");
        gradientElement.SetAttributeValue("id", "gradientId");

        string stop1Style;
        string stop2Style;
        switch (type)
        {
            case ChartType.GPU:
                stop1Style = GPUBrushStop1Style;
                stop2Style = GPUBrushStop2Style;
                break;
            case ChartType.Mem:
                stop1Style = MemBrushStop1Style;
                stop2Style = MemBrushStop2Style;
                break;
            case ChartType.Net:
                stop1Style = NetBrushStop1Style;
                stop2Style = NetBrushStop2Style;
                break;
            case ChartType.CPU:
            default:
                stop1Style = CPUBrushStop1Style;
                stop2Style = CPUBrushStop2Style;
                break;
        }

        var stop1 = new XElement("stop");
        stop1.SetAttributeValue("offset", "0%");
        stop1.SetAttributeValue("style", stop1Style);

        var stop2 = new XElement("stop");
        stop2.SetAttributeValue("offset", "95%");
        stop2.SetAttributeValue("style", stop2Style);

        gradientElement.Add(stop1);
        gradientElement.Add(stop2);
        defsElement.Add(gradientElement);

        return defsElement;
    }

    private static XElement CreateBorderBox(int height, int width)
    {
        var boxElement = new XElement("rect");
        boxElement.SetAttributeValue("height", height);
        boxElement.SetAttributeValue("width", width);
        boxElement.SetAttributeValue("style", LightGrayBoxStyle);
        return boxElement;
    }

    private static string GetLineStyle(ChartType type)
    {
        var lineStyle = type switch
        {
            ChartType.CPU => CPULineStyle,
            ChartType.GPU => GPULineStyle,
            ChartType.Mem => MemLineStyle,
            ChartType.Net => NetLineStyle,
            _ => CPULineStyle,
        };

        return lineStyle;
    }

    private static StringBuilder TransformPointsToLine(List<float> chartValues, out int startX, out int finalX)
    {
        var points = new StringBuilder();

        var pxBtwnPoints = 9;

        // The X value where the graph starts must be adjusted so that the graph is right-aligned.
        // The max available width of the widget is 268. Since there is a 1 px border around the chart, the width of the chart's line must be <=266.
        // To create a chart of exactly the right size, we'll have 30 points with 9 pixels in between:
        // index 0            1                      2 - 262                          263
        // 1 px left border + 1 px for first point + 29 segments * 9 px per segment + 1 px right border = 264 pixels total in width.

        // When the chart doesn't have all points yet, move the chart over to the right by increasing the starting X coordinate.
        // For a chart with only 1 point, the svg will not render a polyline.
        // For a chart with 2 points, starting X coordinate ==  1 + (30 -  2) * 9 == 1 + 28 * 9 == 1 + 252 == 253
        // For a chart with 30 points, starting X coordinate == 1 + (30 - 30) * 9 == 1 +  0 * 9 == 1 +   0 ==   1
        startX = 1 + ((MaxChartValues - chartValues.Count) * pxBtwnPoints);
        finalX = startX;
        foreach (var origY in chartValues)
        {
            points.Append(finalX + "," + (101 - origY) + " ");
            finalX += pxBtwnPoints;
        }

        // Remove the trailing space.
        if (points.Length > 0)
        {
            points.Remove(points.Length - 1, 1);
            finalX -= pxBtwnPoints;
        }

        return points;
    }

    private static void TransformPointsToLoop(StringBuilder points, int startX, int finalX)
    {
        // Close the loop.
        // Add a point at the most recent X value that corresponds with y = 0
        points.Append(" " + finalX + ",101 ");

        // Add a point at the start of the chart that corresponds with y = 0
        points.Append(startX + ",101");
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
