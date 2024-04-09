// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Security;
using System.Text;
using HyperVExtension.Exceptions;
using HyperVExtension.Services;

namespace HyperVExtension.Helpers;

/// <summary>
/// A helper class to deploy DevSetupAgent service to a VM using PowerShell Direct service.
/// </summary>
public class DevSetupAgentDeploymentHelper
{
    // Architectures returned by Win32_Processor.Architecture (Win32_Processor WMI class)
    private enum ProcessorArchitecture : ushort
    {
        X86 = 0,
        MIPS = 1,
        Alpha = 2,
        PowerPC = 3,
        ARM = 5,
        IA64 = 6,
        X64 = 9,
        ARM64 = 12,
    }

    private readonly IPowerShellService _powerShellService;
    private readonly string _vmId;
    private readonly Lazy<string> _script = new(() => LoadScript());

    public DevSetupAgentDeploymentHelper(IPowerShellService powerShellService, string vmId)
    {
        _powerShellService = powerShellService;
        _vmId = vmId;
    }

    public void DeployDevSetupAgent(string userName, SecureString password)
    {
        var credential = new PSCredential(userName, password);
        var session = GetSessionObject(credential);
        var architecture = GetVMArchitechture(session);
        var sourcePath = GetSourcePath(architecture);

        var deployDevSetupAgentStatement = new StatementBuilder()
                .AddScript(_script.Value, false)
                .AddCommand("Install-DevSetupAgent")
                .AddParameter("VMId", _vmId)
                .AddParameter("Session", session)
                .AddParameter("Path", sourcePath)
                .Build();

        // TODO: Subscribe for PowerShell events to get the progress of the deployment like:
        ////  ps.Streams.Information.DataAdded += (sender, e) =>
        ////  ps.Streams.Error.DataAdded += (sender, e) =>
        ////  ps.Streams.Verbose.DataAdded += (sender, e) =>
        ////  ps.Streams.Warning.DataAdded += (sender, e) =>
        ////  ps.Streams.Progress.DataAdded += (sender, e) =>
        var result = _powerShellService.Execute(deployDevSetupAgentStatement, PipeType.DontClearBetweenStatements);
        if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
        {
            throw new DevSetupAgentDeploymentException(
                $"Unable to deploy DevSetupAgent service to VM with Id: {_vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
        }
    }

    public virtual string GetSourcePath(ushort architecture)
    {
        if ((architecture == (ushort)ProcessorArchitecture.X64) || (architecture == (ushort)ProcessorArchitecture.X86))
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevSetupAgent_x86.zip");
        }
        else if (architecture == (ushort)ProcessorArchitecture.ARM64)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevSetupAgent_arm64.zip");
        }
        else
        {
            throw new DevSetupAgentDeploymentException(
                $"Unable to deploy DevSetupAgent service to VM with Id: {_vmId} due to unsupported architecture: {architecture}");
        }
    }

    public PSObject GetSessionObject(PSCredential credential)
    {
        var newSessionCommand = new StatementBuilder()
            .AddCommand("New-PSSession")
            .AddParameter("VMId", _vmId)
            .AddParameter("Credential", credential)
            .Build();

        var result = _powerShellService.Execute(newSessionCommand, PipeType.None);
        if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
        {
            throw new DevSetupAgentDeploymentSessionException(
                $"Unable to create remote session for VM with Id: {_vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
        }

        return result!.PsObjects.FirstOrDefault()!;
    }

    private ushort GetVMArchitechture(PSObject session)
    {
        var getVMArchitechtureCommand = new StatementBuilder()
                       .AddCommand("Invoke-Command")
                       .AddParameter("Session", session)
                       .AddParameter("ScriptBlock", ScriptBlock.Create("(Get-CIMInstance -Class win32_processor).Architecture"))
                       .Build();

        var result = _powerShellService!.Execute(getVMArchitechtureCommand, PipeType.None);
        if (!string.IsNullOrEmpty(result.CommandOutputErrorMessage))
        {
            throw new DevSetupAgentDeploymentException(
                               $"Unable to get VM architecture for VM with Id: {_vmId} due to PowerShell error: {result.CommandOutputErrorMessage}");
        }

        var psObject = result.PsObjects.FirstOrDefault();
        if (psObject == null)
        {
            throw new DevSetupAgentDeploymentException(
                               $"Unable to get VM architecture for VM with Id: {_vmId} due to PowerShell error: No result returned");
        }

        return (ushort)psObject.BaseObject;
    }

    private static string LoadScript()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "HyperVExtension", "Scripts", "DevSetupAgent.ps1");
        return File.ReadAllText(path, Encoding.Default) ?? throw new FileNotFoundException(path);
    }
}
