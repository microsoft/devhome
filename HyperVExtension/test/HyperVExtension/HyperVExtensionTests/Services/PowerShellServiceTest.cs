// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common.Extensions;
using HyperVExtension.Helpers;
using HyperVExtension.Services;
using HyperVExtension.UnitTest.Mocks;

namespace HyperVExtension.UnitTest.HyperVExtensionTests.Services;

[TestClass]
public class PowerShellServiceTest : HyperVExtensionTestsBase
{
    private readonly PSCustomObjectMock _powerShellSessionReturnObject = new();

    [TestMethod]
    public void PowerShellServiceCanRunASingleCommand()
    {
        var powerShellService = TestHost.GetService<IPowerShellService>();
        var expectedDate = "11/30/2023";
        var propertyName = "Date";
        _powerShellSessionReturnObject.Date = expectedDate;
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(_powerShellSessionReturnObject); });

        // Uses the Get-Date cmdlet to produce a string in the format MM/dd/yyyy
        // PowerShell Equivalent: Get-Date -Date "November 30 2023" -Format "MM/dd/yyyy"
        var commandLineStatements = new StatementBuilder()
            .AddCommand("Get-Date")
            .AddParameter("Date", "November 30 2023")
            .AddParameter("Format", @"MM/dd/yyyy")
            .Build();

        // Act
        var result = powerShellService.Execute(commandLineStatements.First());

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.CommandOutputErrorMessage?.Length > 0);
        var helper = new PsObjectHelper(result.PsObjects.First());
        var actualValue = helper.MemberNameToValue<string>(propertyName);
        Assert.AreEqual(expectedDate, actualValue);
    }

    [TestMethod]
    public void PowerShellServiceCanPipeMultipleStatements()
    {
        // Arrange
        var powerShellService = TestHost.GetService<IPowerShellService>();
        var expectedValueTimeZone = "Pacific Standard Time";
        var propertyName = "StandardName";
        _powerShellSessionReturnObject.StandardName = expectedValueTimeZone;
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(_powerShellSessionReturnObject); });

        // Use the Get TimeZone object and pass it as input through piping to the
        // Select-Object cmdlet to get the StandardName property.
        // PowerShell Equivalent:  Get-TimeZone -Id PST |  Select-Object -Property StandardName
        var commandLineStatements = new StatementBuilder()
            .AddCommand("Get-TimeZone")
            .AddParameter("Id", "PST")
            .AddCommand("Select-Object")
            .AddParameter("Property", @"StandardName")
            .Build();

        // Act
        var result = powerShellService.Execute(commandLineStatements, PipeType.PipeOutput);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.CommandOutputErrorMessage?.Length > 0);
        var helper = new PsObjectHelper(result.PsObjects.First());
        var actualValue = helper.MemberNameToValue<string>(propertyName);
        Assert.AreEqual(expectedValueTimeZone, actualValue);
    }

    [TestMethod]
    public void PowerShellServiceReturnsFailureErrors()
    {
        // Arrange
        var powerShellService = TestHost.GetService<IPowerShellService>();
        var expectedValueError = "Attempted to divide by zero.";
        SetupPowerShellSessionInvokeResults()
            .Returns(() => { return CreatePSObjectCollection(_powerShellSessionReturnObject); });

        // For every call to the PowerShellService's execute method, two calls to PowerShellSession.GetErrorMessages() is expected.
        SetupPowerShellSessionErrorMessages()
            .Returns(() => { return expectedValueError; })
            .Returns(() => { return expectedValueError; });

        // Create a script, make sure its culture language is english and attempt to
        // divide 1 by 0.
        var commandLineStatements = new StatementBuilder()
            .AddScript(
                "[cultureinfo]::CurrentUICulture = 'en-US';" +
                " 1 / 0",
                true)
            .Build();

        // Act
        var result = powerShellService.Execute(commandLineStatements.First());

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.CommandOutputErrorMessage?.Length > 0);
        var actualValue = result.CommandOutputErrorMessage;
        Assert.AreEqual(expectedValueError, actualValue);
    }
}
