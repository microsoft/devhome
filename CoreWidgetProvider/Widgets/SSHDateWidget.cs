// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CoreWidgetProvider.Helpers;
using CoreWidgetProvider.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;
using Newtonsoft.Json.Linq;

namespace CoreWidgetProvider.Widgets;
internal class SSHDateWidget : CoreWidget, IDisposable
{
    private const string NameOf = nameof(SSHDateWidget);
    private readonly string output = string.Empty;
    private readonly string assetPath = string.Empty;

    public LimitedList<float> CpuChartValues { get; set; } = new LimitedList<float>(30);

    private static readonly object LockObject = new ();

    private readonly Process sshProcess;

    private const string ShellPromptPattern = @"\S+@\S+:\S+\$";

    private string capturedOutput;

    private string Host { get; } = "";

    private int Port { get; } = 0;

    private string Username { get; } = "";

    private string Password { get; } = "";

    private Timer? Timer
    {
        get; set;
    }

    private bool IsTimerInit
    {
        get; set;
    }

    public SSHDateWidget()
    {
        var sshCommand = "ssh.exe";
        var sshArguments = @"-tt -p22 ubuntu@hostname"; // Replace with your SSH connection details

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = sshCommand,
            Arguments = sshArguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        sshProcess = new Process
        {
            StartInfo = startInfo,
        };

        capturedOutput = string.Empty; // Variable to store the captured output
    }

    public override void CreateWidget(WidgetContext widgetContext, string state)
    {
        Id = widgetContext.Id;
        Enabled = widgetContext.IsActive;
        UpdateActivityState();
    }

    public override void Activate(WidgetContext widgetContext)
    {
        Enabled = true;
        UpdateActivityState();
    }

    public override void Deactivate(string widgetId)
    {
        Enabled = false;
        UpdateActivityState();
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        Enabled = false;
        SetDeleted();
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => new JsonObject { { "configuring", true } }.ToJsonString(),

            // In case of unknown state default to empty data
            _ => EmptyJson,
        };
    }

    public override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Connecting to {Host}:{Port}...");
        try
        {
            sshProcess.Start();
            sshProcess.BeginOutputReadLine();
            sshProcess.BeginErrorReadLine();
            InitOutput();
        }
        catch (Exception)
        {
            Log.Logger()?.ReportError("Failed connecting, Maybe instance already running?");
            return;
        }

        if (!IsTimerInit)
        {
            Timer = new Timer(ExecuteSSHCommand, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            IsTimerInit = true;
        }

        DataState = WidgetDataState.Okay;
    }

    private void InitOutput()
    {
        sshProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (!Regex.IsMatch(e.Data, ShellPromptPattern))
                {
                    capturedOutput = e.Data;
                    if (capturedOutput.Contains('{'))
                    {
                        var output = capturedOutput;
                        JObject jsonObject = new JObject();
                        jsonObject = JObject.Parse(output.Trim());
                        var uptime = jsonObject.GetValue("uptime");
                        var numOfCores = jsonObject.GetValue("cores");
                        if (jsonObject.TryGetValue("cpuCoreData", out var cpuStats) && cpuStats != null)
                        {
                            var cpuStatValues = cpuStats.ToString().Split(" ");
                            var sum = 0.0;
                            foreach (var cpuStat in cpuStatValues)
                            {
                                if (double.TryParse(cpuStat, out var cpuValue))
                                {
                                    sum += cpuValue;
                                }
                            }

                            if (numOfCores != null)
                            {
                                var average = sum / (double)(numOfCores ?? 1.00);
                                CpuChartValues.Add((float)average);
                                Log.Logger()?.ReportDebug(Name, ShortId, $"Average CPU: {average}");
                                Log.Logger()?.ReportDebug(Name, ShortId, $"CPU Chart Values: {string.Join(",", CpuChartValues)}");
                                var imgUrl = ChartHelper.CreateImageUrl(CpuChartValues, ChartHelper.ChartType.CPU);
                                jsonObject.RemoveAll();
                                jsonObject.Add("cpuStatsGraphUrl", imgUrl);
                                jsonObject.Add("uptime", uptime);
                                jsonObject.Add(nameof(Host), Host);
                                ContentData = jsonObject.ToString();
                                UpdateWidget();
                            }
                        }
                        else
                        {
                            ContentData = "Failed";
                        }
                    }
                }
            }
        };
    }

    private void ExecuteSSHCommand(object? state)
    {
        try
        {
            // var assemblyPath = Assembly.GetExecutingAssembly().Location;
            // var assetDirectory = Path.GetDirectoryName(assemblyPath);
            // if (assetDirectory != null)
            // {
            //    var assetPath = Path.Combine(assetDirectory, "Widgets/Assets/getCPUStats.sh");
            // }

            // string readContents;
            // using (StreamReader streamReader = new StreamReader(assetPath, Encoding.UTF8))
            // {
            //    readContents = streamReader.ReadToEnd();
            // }
            var sshStreamWriter = sshProcess.StandardInput;
            sshStreamWriter.WriteLine("bash back.sh");

            // Log.Logger()?.ReportDebug(Name, ShortId, $"Error: {errorMsg}");

            // Log.Logger()?.ReportDebug(Name, ShortId, $"SSH command output: {ContentData}");
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, ex.Message);
        }
    }

    public override void UpdateActivityState()
    {
        if (Enabled)
        {
            SetActive();
        }
        else
        {
            SetInactive();
        }
    }

    public override void UpdateWidget()
    {
        WidgetUpdateRequestOptions updateOptions = new (Id)
        {
            Data = GetData(Page),
            Template = GetTemplateForPage(Page),
        };

        WidgetManager.GetDefault().UpdateWidget(updateOptions);
    }

    protected override void SetInactive()
    {
        ActivityState = WidgetActivityState.Inactive;
        Page = WidgetPageState.Content;
        UpdateWidget();
        LogCurrentState();
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        Log.Logger()?.ReportDebug("Received page value: " + page);
        return page switch
        {
            WidgetPageState.Content => @"Widgets\Templates\SSHDateWidgetTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\LoadingTemplate.json",
            WidgetPageState.Unknown => @"Widgets\Templates\LoadingTemplate.json",
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null),
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources here
            sshProcess.Dispose();
        }
    }
}
