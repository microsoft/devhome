// ----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ----------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.DevHome.Stub.Helper;
using Microsoft.DevHome.Stub.Properties;
using Microsoft.DevHome.Stub.ViewModel;

namespace Microsoft.DevHome.Stub
{
    public class MainViewModel : ViewModelBase
    {
        private readonly string _protocolHandlerArguments;
        private ErrorViewModel _error;
        private bool _hasError;

        private double _progress;
        private string _progressText;
        private UpgradeHelper _upgradeHelper;
        private UpgradeState _upgradeState;

        internal MainViewModel(string protocolHandlerArguments)
        {
            _protocolHandlerArguments = protocolHandlerArguments;
            _upgradeHelper = new UpgradeHelper(_protocolHandlerArguments);

            InstallCommand = new RelayCommand(Install);
            TitleText = string.Format(Stubs.InfoHeader, AppName);
            ReadyToInstallText = string.Format(Stubs.ProgressReadyText, null, AppName);
            ErrorTitleText = string.Format(Stubs.ErrorHeader, AppName);
            InstallText = string.Format(Stubs.InstallButtonLabel, AppName);
        }

        public string InstallText
        {
            get;
        }

        public UpgradeState UpgradeState
        {
            get => _upgradeState;
            set
            {
                _upgradeState = value;
                OnPropertyChanged();
            }
        }

        public string AppName { get; } = "Dev Home";

        public string TitleText
        {
            get;
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand InstallCommand
        {
            get;
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public string ReadyToInstallText
        {
            get;
        }

        public string ErrorTitleText
        {
            get;
        }

        public ErrorViewModel Error
        {
            get => _error;
            set
            {
                _error = value;
                OnPropertyChanged();
            }
        }

        public void Initialize()
        {
            TraceLogger.Instance.LogResponsive();
            SubscribeToUpgradeHelperEvents();
            _upgradeHelper.StartUpgrade();
        }

        private void Install(object obj)
        {
            _upgradeHelper = null;
            _upgradeHelper = new UpgradeHelper(_protocolHandlerArguments);
            SubscribeToUpgradeHelperEvents();
            _upgradeHelper?.StartUpgrade();
        }

        private void SubscribeToUpgradeHelperEvents()
        {
            if (_upgradeHelper == null)
            {
                return;
            }

            _upgradeHelper.ProgressChanged -= UpgradeHelper_ProgressChanged;
            _upgradeHelper.UpgradeStateChanged -= UpgradeHelper_UpgradeStateChanged;

            _upgradeHelper.ProgressChanged += UpgradeHelper_ProgressChanged;
            _upgradeHelper.UpgradeStateChanged += UpgradeHelper_UpgradeStateChanged;
        }

        private void UpgradeHelper_ProgressChanged(object sender, double progress)
        {
            Progress = progress * 100;

            switch (_upgradeHelper.State)
            {
                case UpgradeState.Downloading:
                    ProgressText = string.Format(Stubs.DownloadingUpdates, Progress.ToString());
                    break;
                case UpgradeState.Deploying:
                    ProgressText = string.Format(Stubs.ApplyingUpdates, Progress.ToString());
                    break;
            }
        }

        public async void RunSimulation()
        {
            HasError = false;

            var rand = new Random(DateTime.Now.Millisecond);

            UpgradeHelper_UpgradeStateChanged(this, UpgradeState.Stopped);

            await Task.Delay(rand.Next(3000, 5000));
            UpgradeHelper_UpgradeStateChanged(this, UpgradeState.BlockedStoreError);

            await Task.Delay(rand.Next(3000, 5000));
            UpgradeHelper_UpgradeStateChanged(this, UpgradeState.NetworkError);

            await Task.Delay(rand.Next(3000, 5000));
            UpgradeHelper_UpgradeStateChanged(this, UpgradeState.OtherError);

            await Task.Delay(rand.Next(3000, 5000));
            UpgradeHelper_UpgradeStateChanged(this, UpgradeState.InProgress);

            await Task.Delay(rand.Next(3000, 5000));

            var progress = 0d;

            while (progress < 1d)
            {
                progress = Math.Min(1.0, progress + (0.01 * rand.Next(1, 20)));
                UpgradeHelper_ProgressChanged(this, progress);

                if (progress < 0.7)
                {
                    UpgradeHelper_UpgradeStateChanged(this, UpgradeState.Downloading);
                    ProgressText = string.Format(Stubs.DownloadingUpdates, Progress.ToString());
                }
                else
                {
                    UpgradeHelper_UpgradeStateChanged(this, UpgradeState.Deploying);
                    ProgressText = string.Format(Stubs.ApplyingUpdates, Progress.ToString());
                }

                await Task.Delay(rand.Next(100, 5000));
            }
        }

        private void UpgradeHelper_UpgradeStateChanged(object sender, UpgradeState state)
        {
            UpgradeState = state;

            switch (state)
            {
                case UpgradeState.Stopped:
                    HasError = false;
                    break;
                case UpgradeState.InProgress:
                    Progress = 0;
                    HasError = false;
                    break;
                case UpgradeState.Downloading:
                    HasError = false;
                    break;
                case UpgradeState.Deploying:
                    HasError = false;
                    break;
                case UpgradeState.NetworkError:
                case UpgradeState.OtherError:
                case UpgradeState.BlockedStoreError:
                    {
                        UpdateErrorDetails();
                        HasError = true;
                        break;
                    }
            }
        }

        private void UpdateErrorDetails()
        {
            string uri;
            string errorLinkText;

            switch (UpgradeState)
            {
                case UpgradeState.NetworkError:
                    uri = "ms-settings:network";
                    errorLinkText = Stubs.ErrorNoNetworkDetail;
                    break;
                case UpgradeState.BlockedStoreError:
                    uri = "https://go.microsoft.com/fwlink/?linkid=2128688";
                    errorLinkText = Stubs.ErrorGroupPolicyDetail;
                    break;
                default:
                    uri = "windows-feedback:?contextid=1128";
                    errorLinkText = Stubs.ErrorOtherDetail;
                    break;
            }

            var openingBracketPosition = errorLinkText.IndexOf('[');
            var closingBracketPosition = errorLinkText.IndexOf(']');

            if (openingBracketPosition == -1 || closingBracketPosition == -1)
            {
                throw new ArgumentException("missing [ or ]");
            }

            var prefixText = errorLinkText.Substring(0, openingBracketPosition);
            var linkText = errorLinkText.Substring(openingBracketPosition + 1, closingBracketPosition - openingBracketPosition - 1);
            var suffixText = errorLinkText.Substring(closingBracketPosition + 1);

            Error = new ErrorViewModel(prefixText, suffixText, linkText, uri);
        }
    }
}
