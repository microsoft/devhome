//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------

namespace Microsoft.PowerAutomateDesktop.Stub.Helper
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Windows.ApplicationModel;
	using Windows.Foundation;
	using Windows.Management.Deployment;
	using Windows.Networking.Connectivity;
	using Windows.Services.Store;
	using Windows.UI.Xaml.Media.Animation;

	internal class UpgradeHelper : IUpgradeHelper
	{
		private double progress;
		private string _protocolHandlerArguments = null;
		private UpgradeState state;
		private IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> upgradeOperation;

		public event EventHandler<double> ProgressChanged;
		public event EventHandler<UpgradeState> UpgradeStateChanged;

		public UpgradeHelper(string protocolHandlerArguments = null)
		{
			_protocolHandlerArguments = protocolHandlerArguments;
			this.progress = 0;
			this.state = UpgradeState.Stopped;
		}

		public double Progress
		{
			get
			{
				return this.progress;
			}

			private set
			{
				if (this.progress != value)
				{
					this.progress = value;
					this.ProgressChanged?.Invoke(this, this.progress);
				}
			}
		}


		public UpgradeState State
		{
			get
			{
				return this.state;
			}

			private set
			{
				if (this.state != value)
				{
					this.state = value;

					string newState = null;
					switch (value)
					{
						case UpgradeState.InProgress:
							newState = "InProgress";
							break;
						case UpgradeState.NetworkError:
							newState = "NetworkError";
							break;
						case UpgradeState.BlockedStoreError:
							newState = "BlockedStoreError";
							break;
						case UpgradeState.OtherError:
							newState = "OtherError";
							break;
						case UpgradeState.Stopped:
							newState = "Stopped";
							break;
						case UpgradeState.Downloading:
						case UpgradeState.Deploying:
							// no op
							break;
					}

					TraceLogger.Instance.LogStateChanged(newState);
					this.UpgradeStateChanged?.Invoke(this, this.state);
				}
			}
		}

		public void StartUpgrade()
		{
#pragma warning disable CS4014
			this.UpgradeAsync();
#pragma warning restore CS4014
		}

		private async Task UpgradeAsync()
		{
			Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;
			this.State = UpgradeState.InProgress;

			try
			{
				var packageFamilyName = Package.Current.Id.FamilyName;
				var packageManager = new PackageManager();
				packageManager.SetPackageStubPreference(packageFamilyName, PackageStubPreference.Full);

				var allUsers = await Windows.System.User.FindAllAsync();
				var l = allUsers.ToList();

				var store = StoreContext.GetDefault();
				TraceLogger.Instance.LogAcquiringPackages();

				var allPackages = await store.GetAppAndOptionalStorePackageUpdatesAsync();
				if (allPackages == null)
				{
					throw new Exception("GetAppAndOptionalStorePackageUpdatesAsync returned null.");
				}

				var updates = new List<StorePackageUpdate>();
				foreach (var pkg in allPackages)
				{
					if (!pkg.Package.IsOptional)
					{
						updates.Add(pkg);
					}
				}

				if (store.CanSilentlyDownloadStorePackageUpdates)
				{
					TraceLogger.Instance.LogStartingSilentUpdate();
					upgradeOperation = store.TrySilentDownloadAndInstallStorePackageUpdatesAsync(updates);
				}
				else
				{
					TraceLogger.Instance.LogStartingInteractiveUpdate();
					upgradeOperation = store.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);
				}

				upgradeOperation.Progress = (_ /*sender*/, args) =>
				{
					if (args.PackageFamilyName == packageFamilyName)
					{
						switch (args.PackageUpdateState)
						{
							case StorePackageUpdateState.Canceled:
								this.State = UpgradeState.Stopped;
								break;
							case StorePackageUpdateState.Completed:
							case StorePackageUpdateState.Pending:
								// TODO handle these
								break;
							case StorePackageUpdateState.Downloading:
								this.State = UpgradeState.Downloading;
								break;
							case StorePackageUpdateState.Deploying:
								this.State = UpgradeState.Deploying;
								break;
							case StorePackageUpdateState.ErrorLowBattery:
							case StorePackageUpdateState.ErrorWiFiRecommended:
							case StorePackageUpdateState.ErrorWiFiRequired:
							case StorePackageUpdateState.OtherError:
								this.State = UpgradeState.OtherError;
								break;
						}

						this.Progress = args.PackageDownloadProgress;
					}
				};
			}
			catch (Exception e)
			{
				TraceLogger.Instance.LogUpgradeError(e);
				var errorState = UpgradeState.OtherError;

				if (unchecked((uint)e.HResult) == 0x80240438) // WU_E_PT_ENDPOINT_UNREACHABLE
				{
					errorState = UpgradeState.NetworkError;
				}
				else if (unchecked((uint)e.HResult) == 0x8024500c) // WU_E_REDIRECTOR_CONNECT_POLICY - Error when updates are blocked by policy
				{
					errorState = UpgradeState.BlockedStoreError;
				}
				else
				{
					var internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
					if (internetConnectionProfile == null ||
						internetConnectionProfile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.InternetAccess)
					{
						errorState = UpgradeState.NetworkError;
					}
				}

				this.State = errorState;
			}
		}

		private void SystemEvents_SessionEnding(object sender, Win32.SessionEndingEventArgs e)
		{
			this.Progress = 999;
			Thread.Sleep(300);
			NativeMethods.RegisterApplicationRestart(_protocolHandlerArguments, 1 | 2 | 8 /* only restart on patch */);
		}

		private static class NativeMethods
		{
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, int Flags);
		}
	}
}
