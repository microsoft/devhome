// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.ViewModels;

public partial class CreateDevDriveSummaryInformationViewModel : ObservableRecipient, ISummaryInformationViewModel
{
    public bool HasContent => false;
}
