// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.SetupFlow.Utilities;

namespace DevHome.SetupFlow.UnitTest;

/// <summary>
/// Test class for the DevDriveEnumToLocalizedStringConverter which takes a DevDriveValidationResult
/// and converts it to a localized string within the setup flow Resources.resw file.
/// </summary>
[TestClass]
public class DevDriveEnumToLocalizedStringConverterTest : BaseSetupFlowTest
{
    public DevDriveEnumToLocalizedStringConverter Converter => new(StringResource.Object);

    // These are results that are not localized and are just used in the code.
    public List<DevDriveValidationResult> ResultsToIgnore => new()
    {
        DevDriveValidationResult.Successful,
        DevDriveValidationResult.ObjectWasNull,
    };

    [TestMethod]
    public void DevDriveEnumsToLocalizedString()
    {
        foreach (DevDriveValidationResult validationResult in Enum.GetValues(typeof(DevDriveValidationResult)))
        {
            if (!ResultsToIgnore.Contains(validationResult))
            {
                var converterResultString = Converter.Convert(nameof(validationResult), null, null, string.Empty);
                var expectedLocalizedString = StringResource.Object.GetLocalized("DevDrive" + nameof(validationResult));
                Assert.AreEqual(expectedLocalizedString, converterResultString);
            }
        }
    }
}
