// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class InstallTaskDefinition : TaskDefinition
{
    private const string _packageIdArg = "--package-id";
    private const string _packageCatalogArg = "--package-catalog";

    public string PackageId
    {
        get; set;
    }

    public string CatalogName
    {
        get; set;
    }

    public static InstallTaskDefinition ReadCliArgument(string[] args, ref int index)
    {
        const int length = 4;
        if (index + length <= args.Length &&
            args[index] == _packageIdArg &&
            args[index + 2] == _packageCatalogArg)
        {
            var result = new InstallTaskDefinition
            {
                PackageId = args[index + 1],
                CatalogName = args[index + 3],
            };
            index += length;
            return result;
        }

        return null;
    }

    public override string ToCliArgument()
    {
        return $"{_packageIdArg} \"{PackageId}\" {_packageCatalogArg} \"{CatalogName}\"";
    }
}
