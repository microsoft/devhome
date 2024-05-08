// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.ApplicationModel;

namespace HyperVExtension.DevSetupEngine;

public static class PackageVersionExtensions
{
    public static bool LessThan(this PackageVersion version, ushort major, ushort minor, ushort build, ushort revision)
    {
        return LessThan(version, new PackageVersion(major, minor, build, revision));
    }

    public static bool LessThan(this PackageVersion version, PackageVersion other)
    {
        if (version.Major > other.Major)
        {
            return false;
        }
        else if (version.Major < other.Major)
        {
            return true;
        }

        if (version.Minor > other.Minor)
        {
            return false;
        }
        else if (version.Minor < other.Minor)
        {
            return true;
        }

        if (version.Build > other.Build)
        {
            return false;
        }
        else if (version.Build < other.Build)
        {
            return true;
        }

        if (version.Revision >= other.Revision)
        {
            return false;
        }

        return true;
    }
}
