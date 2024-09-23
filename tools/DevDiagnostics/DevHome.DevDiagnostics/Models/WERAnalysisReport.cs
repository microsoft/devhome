// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using DevHome.DevDiagnostics.Helpers;
using Serilog;

namespace DevHome.DevDiagnostics.Models;

// This class holds the WER analysis from the dump analysis tool
public class WERAnalysisReport
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WERAnalysisReport));

    public string? Analysis { get; private set; }

    public string? FailureBucket { get; private set; }

    public WERAnalysisReport(string rawAnalysis)
    {
        ParseRawAnalysis(rawAnalysis);
    }

    private void ParseRawAnalysis(string rawAnalysis)
    {
        // This is an XML-formated analysis
        try
        {
            StringBuilder analysisText = new StringBuilder();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(rawAnalysis);

            if (doc.DocumentElement is null)
            {
                throw new InvalidDataException();
            }

            // First get the failure bucket ID. This is required.
            XmlNode? failureBucketNode = doc.DocumentElement.SelectSingleNode("/ANALYSIS/FAILURE_BUCKET_ID");
            if (failureBucketNode is null)
            {
                throw new InvalidDataException();
            }

            FailureBucket = failureBucketNode.InnerText;
            analysisText.AppendLine(failureBucketNode.InnerText);
            analysisText.AppendLine();

            // Now try and get the failure sure file and line number. This is not required.
            XmlNode? failureSourceFile = doc.DocumentElement.SelectSingleNode("/ANALYSIS/FA_ADHOC/FAULTING_SOURCE_FILE");
            XmlNode? failureSourceFileLineNumber = doc.DocumentElement.SelectSingleNode("/ANALYSIS/FA_ADHOC/FAULTING_SOURCE_LINE_NUMBER");

            if (!string.IsNullOrEmpty(failureSourceFile?.InnerText))
            {
                // Emit a text block that looks like
                //
                // Failing File: <filename>
                // Line Number: <line number>
                analysisText.AppendLine(string.Format(CultureInfo.InvariantCulture, CommonHelper.GetLocalizedString("DumpAnalysisFailingFileLabel"), failureSourceFile.InnerText));
                if (!string.IsNullOrEmpty(failureSourceFileLineNumber?.InnerText))
                {
                    analysisText.AppendLine(string.Format(CultureInfo.InvariantCulture, CommonHelper.GetLocalizedString("DumpAnalysisLineNumber"), failureSourceFileLineNumber.InnerText)));
                }

                analysisText.AppendLine();
            }

            // Now get the callstack. This is not required.
            XmlNode? callstackframes = doc.DocumentElement.SelectSingleNode("/ANALYSIS/FLP_CTX/FRMS");

            if (callstackframes is not null)
            {
                foreach (XmlNode frame in callstackframes.ChildNodes)
                {
                    XmlNode? frameNumNode = frame.SelectSingleNode("NUM");
                    XmlNode? symbolNode = frame.SelectSingleNode("SYM");
                    XmlNode? srcNode = frame.SelectSingleNode("SRC");
                    XmlNode? offsetNode = frame.SelectSingleNode("OFF");
                    XmlNode? moduleNode = frame.SelectSingleNode("MOD");
                    XmlNode? functionNode = frame.SelectSingleNode("FNC");

                    // Figure out what to print for the function name
                    string functionName = symbolNode?.InnerText ?? string.Empty;
                    if (string.IsNullOrEmpty(functionName) && !string.IsNullOrEmpty(moduleNode?.InnerText) && !string.IsNullOrEmpty(functionNode?.InnerText))
                    {
                        functionName = string.Format(CultureInfo.InvariantCulture, "{0}!{1}", moduleNode.InnerText, functionNode.InnerText);
                    }

                    // And figure out the offset
                    int? offset = CommonHelper.ParseStringToInt(offsetNode?.InnerText ?? string.Empty);
                    string offsetString = string.Empty;

                    if (offset.HasValue && offset != 0)
                    {
                        offsetString = "+" + Convert.ToString((int)offset, 16);
                    }

                    string src = string.Empty;

                    if (!string.IsNullOrEmpty(srcNode?.InnerText))
                    {
                        src = string.Format(CultureInfo.InvariantCulture, "[{0}]", srcNode.InnerText);
                    }

                    if (!string.IsNullOrEmpty(functionName))
                    {
                        analysisText.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0, 3} {1}{2} {3}", frameNumNode?.InnerText, functionName, offsetString, src));
                    }
                }
            }

            Analysis = analysisText.ToString();
        }
        catch
        {
            Analysis = CommonHelper.GetLocalizedString("DumpAnalysisParseFailed");
            _log.Error("Unexpected XML format");
            _log.Error(rawAnalysis);
        }
    }
}
