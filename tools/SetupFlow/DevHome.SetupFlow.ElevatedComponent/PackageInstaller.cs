// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.ElevatedComponent;

public sealed class PackageInstaller
{
    public void Install(string packageId, string catalogId)
    {
        InstallAsTask(packageId, catalogId).Wait();
    }

    private async Task InstallAsTask(string packageId, string catalogId)
    {
        Console.WriteLine($"Installing {packageId} from {catalogId}");
        try
        {
            var factory = new ElevatedWindowsPackageManagerFactory();
            var pm = factory.CreatePackageManager();
            var catalogs = pm.GetPackageCatalogs();
            PackageCatalogReference? catalog = default;
            for (var i = 0; i < catalogs.Count; i++)
            {
                if (string.Equals(catalogs[i].Info.Id, catalogId, StringComparison.OrdinalIgnoreCase))
                {
                    catalog = catalogs[i];
                    break;
                }
            }

            if (catalog is null)
            {
                Console.WriteLine("Catalog not found");
            }

            var connectResult = catalog!.Connect();
            if (connectResult.Status != ConnectResultStatus.Ok)
            {
                Console.WriteLine($"Failed to connect {connectResult.Status}");
                return;
            }

            var filter = factory.CreatePackageMatchFilter();
            filter.Field = PackageMatchField.Id;
            filter.Value = packageId;
            var findOptions = factory.CreateFindPackagesOptions();
            findOptions.Filters.Add(filter);

            var findResult = connectResult.PackageCatalog.FindPackages(findOptions);
            if (findResult.Status != FindPackagesResultStatus.Ok)
            {
                Console.WriteLine($"Failed to find {findResult.Status}");
                return;
            }

            var installOptions = factory.CreateInstallOptions();

            Console.WriteLine("Starting install");
            var result = await pm.InstallPackageAsync(findResult.Matches[0].CatalogPackage, installOptions);

            Console.WriteLine($"Finished with status{result.Status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
