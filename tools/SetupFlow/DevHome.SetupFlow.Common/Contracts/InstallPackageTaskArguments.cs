// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;

/// <summary>
/// Class representing an install package task's arguments passed to the elevated process.
/// </summary>
/// <remarks>
/// <code>ElevatedProcess.exe --package-id id --package-catalog catalog</code>
/// </remarks>
public class InstallPackageTaskArguments : ITaskArguments
{
    private const string PackageIdArg = "--package-id";
    private const string PackageCatalogArg = "--package-catalog";

    /// <summary>
    /// Gets or sets the package id
    /// </summary>
    public string PackageId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the package catalog name
    /// </summary>
    public string CatalogName
    {
        get; set;
    }

    /// <summary>
    /// Try to read and parse argument list into an object.
    /// </summary>
    /// <param name="argumentList">Argument list</param>
    /// <param name="index">Index to start reading arguments from</param>
    /// <param name="result">Output object</param>
    /// <returns>True if reading arguments succeeded. False otherwise.</returns>
    public static bool TryReadArguments(IList<string> argumentList, ref int index, out InstallPackageTaskArguments result)
    {
        result = null;

        // --package-id <id>        --package-catalog <catalog>
        // [  index   ] [index + 1] [   index + 2   ] [index + 3]
        const int TaskArgListCount = 4;
        if (index + TaskArgListCount <= argumentList.Count &&
            argumentList[index] == PackageIdArg &&
            argumentList[index + 2] == PackageCatalogArg)
        {
            result = new InstallPackageTaskArguments
            {
                PackageId = argumentList[index + 1],
                CatalogName = argumentList[index + 3],
            };
            index += TaskArgListCount;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Create a list of arguments from this object.
    /// </summary>
    /// <returns>List of argument strings from this object</returns>
    public List<string> ToArgumentList()
    {
        return new ()
        {
            PackageIdArg, PackageId,         // --package-id <id>
            PackageCatalogArg, CatalogName,  // --package-catalog <catalog>
        };
    }
}
