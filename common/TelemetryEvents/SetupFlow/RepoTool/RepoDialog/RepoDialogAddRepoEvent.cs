// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

public enum AddKind
{
    URL,
    Account,
}

[EventData]
public class RepoDialogAddRepoEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public AddKind AddKind { get; private set; }

    public CloneLocationKind CloneLocationKind { get; private set; }

    public int NumberOfReposAdded { get; private set; }

    public string ProviderName { get; private set; }

    public bool IsNewDevDrive { get; private set; }

    public bool DidUserCustomizeNewDevDrive { get; private set; }

    private RepoDialogAddRepoEvent()
    {
        ProviderName = string.Empty;
    }

    public static RepoDialogAddRepoEvent AddWithLocalPath(AddKind addKind, int numberOfRepos, string providerName)
    {
        return new RepoDialogAddRepoEvent
        {
            AddKind = addKind,
            CloneLocationKind = CloneLocationKind.LocalPath,
            NumberOfReposAdded = numberOfRepos,
            ProviderName = providerName,
        };
    }

    public static RepoDialogAddRepoEvent AddWithDevDrive(AddKind addKind, int numberOfRepos, string providerName, bool isNewDevDrive, bool isDriveCustomized)
    {
        return new RepoDialogAddRepoEvent
        {
            AddKind = addKind,
            CloneLocationKind = CloneLocationKind.LocalPath,
            NumberOfReposAdded = numberOfRepos,
            ProviderName = providerName,
            IsNewDevDrive = isNewDevDrive,
            DidUserCustomizeNewDevDrive = isDriveCustomized,
        };
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace
    }
}
