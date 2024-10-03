// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using DevHome.Common.Contracts;
using DevHome.Common.Models.ExtensionJsonData;
using DevHome.Common.Services;
using Json.Schema;
using Moq;
using Windows.Storage;
using static DevHome.Common.Helpers.CommonConstants;

namespace DevHome.Test.FunctionalTests;

[TestClass]
public class ExtensionJsonTests
{
    [TestMethod]
    public async Task ExtensionJsonIsValidatedBySchema()
    {
        string absoluteSchemaPath = Path.Combine(Directory.GetCurrentDirectory(), LocalExtensionJsonSchemaRelativeFilePath);
        Assert.IsTrue(File.Exists(absoluteSchemaPath), $"The schema file '{absoluteSchemaPath}' does not exist.");

        string absoluteExtensionJsonPath = Path.Combine(Directory.GetCurrentDirectory(), LocalExtensionJsonRelativeFilePath);
        Assert.IsTrue(File.Exists(absoluteExtensionJsonPath), $"The extension json file '{absoluteExtensionJsonPath}' does not exist.");

        var jsonSchema = await File.ReadAllTextAsync(absoluteSchemaPath);
        var schema = JsonSchema.FromText(jsonSchema, ExtensionJsonSerializerOptions);

        var extensionJson = await File.ReadAllTextAsync(absoluteExtensionJsonPath);
        var jsonNode = JsonNode.Parse(extensionJson);
        var options = new EvaluationOptions
        {
            OutputFormat = OutputFormat.Hierarchical,
        };
        var validationResult = schema.Evaluate(jsonNode, options);

        Assert.IsTrue(validationResult.IsValid);
    }
}
