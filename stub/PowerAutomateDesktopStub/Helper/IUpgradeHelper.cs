//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------

namespace Microsoft.PowerAutomateDesktop.Stub.Helper
{
	using System;

	internal interface IUpgradeHelper
	{
		void StartUpgrade();

		double Progress { get; }

		UpgradeState State { get; }

		event EventHandler<UpgradeState> UpgradeStateChanged;

		event EventHandler<double> ProgressChanged;
	}
}
