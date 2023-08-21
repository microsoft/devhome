// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class InstallTaskDefinition : ITaskDefinition
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

    public static bool TryReadArguments(IList<string> tasksDefinitionArgumentList, ref int index, out InstallTaskDefinition result)
    {
        result = null;
        const int taskArgListCount = 4;
        if (index + taskArgListCount <= tasksDefinitionArgumentList.Count &&
            tasksDefinitionArgumentList[index] == _packageIdArg &&
            tasksDefinitionArgumentList[index + 2] == _packageCatalogArg)
        {
            result = new InstallTaskDefinition
            {
                PackageId = tasksDefinitionArgumentList[index + 1],
                CatalogName = tasksDefinitionArgumentList[index + 3],
            };
            index += taskArgListCount;
            return true;
        }

        return false;
    }

    public List<string> ToArgumentList()
    {
        return new ()
        {
            _packageIdArg, PackageId,
            _packageCatalogArg, CatalogName,
        };
    }
}
