using Neosmartpen.Net;
using Neosmartpen.Net.Bluetooth;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace SampleApp
{
    public sealed partial class MainPage
	{
		private static readonly string PEN_PROFILE_TEST_NAME = "neolab_t";
		private static readonly byte[] PEN_PROFILE_TEST_PASSWORD = { 0x3E, 0xD5, 0x95, 0x25, 0x06, 0xF7, 0x83, 0xDD };

        private GenericBluetoothPenClient _client;

        private PenController _controller;
		private void InitPenClient()
		{
            // create PenController instance.
            _controller = new PenController();

            // Create BluetoothPenClient instance. and bind PenController.
            // BluetoothPenClient is implementation of bluetooth function.
            _client = new GenericBluetoothPenClient(_controller);

            // bluetooth advertisement event
            _client.onStopSearch += _client_onStopSearch;
            _client.onUpdatePenController += _client_onUpdatePenController;
            _client.onAddPenController += _client_onAddPenController;

			// pen controller event
            _controller.PenStatusReceived += MController_PenStatusReceived;
            _controller.Connected += MController_Connected;
            _controller.Disconnected += MController_Disconnected;
            _controller.Authenticated += MController_Authenticated;
            _controller.DotReceived += MController_DotReceived;
            _controller.PasswordRequested += MController_PasswordRequested;
            _controller.OfflineDataListReceived += MController_OfflineDataListReceived;

            _controller.AutoPowerOffTimeChanged += MController_AutoPowerOffTimeChanged;
            _controller.AutoPowerOnChanged += MController_AutoPowerOnChanged;

            _controller.BatteryAlarmReceived += MController_BatteryAlarmReceived;
            _controller.RtcTimeChanged += MController_RtcTimeChanged;
            _controller.SensitivityChanged += MController_SensitivityChanged;
            _controller.PasswordChanged += MController_PasswordChanged;
            _controller.BeepSoundChanged += MController_BeepSoundChanged;
			_controller.PenColorChanged += MController_PenColorChanged;

            _controller.OfflineDataDownloadStarted += MController_OfflineDataDownloadStarted;
            _controller.OfflineStrokeReceived += MController_OfflineStrokeReceived;
            _controller.OfflineDownloadFinished += MController_OfflineDownloadFinished;

            _controller.FirmwareInstallationStarted += MController_FirmwareInstallationStarted;
            _controller.FirmwareInstallationStatusUpdated += MController_FirmwareInstallationStatusUpdated;
            _controller.FirmwareInstallationFinished += MController_FirmwareInstallationFinished;

			_controller.PenProfileReceived += Mcontroller_PenProfileReceived;
		}

        #region Bluetooth Advertisement Event

        private async void _client_onAddPenController(IPenClient sender, PenInformation args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => lvDevices.Items.Add(args));
        }

        private async void _client_onUpdatePenController(IPenClient sender, PenUpdateInformation args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var item = lvDevices.Items.Where(p => (p as PenInformation)?.Id == args.Id);
                if (item != null)
                {
                    PenInformation penInformation = item as PenInformation;
                    if (penInformation != null)
                    {
                        penInformation.Update(args);
                    }
                }
            });
        }

        private async void _client_onStopSearch(IPenClient sender, Windows.Devices.Bluetooth.BluetoothError args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => IsSearching = false);
        }
		#endregion

		#region Pen Event 
		private async void MController_FirmwareInstallationStarted(IPenClient sender, object args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                _progressDialog.Title = "Firmware Installation";
                await _progressDialog.ShowAsync();
            });
        }

        private async void MController_FirmwareInstallationFinished(IPenClient sender, SimpleResultEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ()=> _progressDialog.Hide() );
        }

        private async void MController_FirmwareInstallationStatusUpdated(IPenClient sender, ProgressChangeEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => _progressDialog.Update(args.AmountDone, args.Total));
        }

        private async void MController_OfflineDataDownloadStarted(IPenClient sender, object args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                _progressDialog.Title = "Offline Data Downloading";
                await _progressDialog.ShowAsync();
            });
        }

        private async void MController_OfflineDownloadFinished(IPenClient sender, SimpleResultEventArgs args)
        {
            _controller.RequestOfflineDataList();
            ShowToast(args.Result ? "Offline data download is complete." : "Offline data download failed.");
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => _progressDialog.Hide());
        }

        private void MController_PasswordChanged(IPenClient sender, SimpleResultEventArgs args)
        {
            ShowToast("Changing password is " + (args.Result ? "complete" : "failure"));
        }

        private void MController_SensitivityChanged(IPenClient sender, SimpleResultEventArgs args)
        {
            _controller?.RequestPenStatus();
        }

        private void MController_RtcTimeChanged(IPenClient sender, SimpleResultEventArgs args)
        {
            _controller?.RequestPenStatus();
        }

        private void MController_BeepSoundChanged(IPenClient sender, SimpleResultEventArgs args)
        {
			if (isTest)
				return;
            _controller?.RequestPenStatus();
        }
		private void MController_PenColorChanged(IPenClient sender, SimpleResultEventArgs args)
		{
            _controller?.RequestPenStatus();
		}

        private void MController_BatteryAlarmReceived(IPenClient sender, BatteryAlarmReceivedEventArgs args)
        {
            _controller?.RequestPenStatus();
        }

        private void MController_AutoPowerOnChanged(IPenClient sender, SimpleResultEventArgs args)
        {
			if (isTest)
				return;
            _controller?.RequestPenStatus();
        }

        private void MController_AutoPowerOffTimeChanged(IPenClient sender, SimpleResultEventArgs args)
        {
			if (isTest)
				return;
            _controller?.RequestPenStatus();
        }

        private async void MController_PenStatusReceived(IPenClient sender, PenStatusReceivedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {

                if (isTest)
                    return;

                cbPenCapPowerControl.IsChecked = args.PenCapPower;

                foreach (ComboBoxItem item in cbAutoPoweroffTime.Items)
                {
                    short numValue = -1;

                    bool result = Int16.TryParse(item.Content as string, out numValue);

                    if (args.AutoShutdownTime == numValue)
                    {
                        cbAutoPoweroffTime.SelectedItem = item;
                        continue;
                    }
                }

                cbPowerOnByPenTip.IsChecked = args.AutoPowerOn;
                cbBeepSound.IsChecked = args.Beep;
                cbOfflineData.IsChecked = args.UseOfflineData;

                pbPower.Maximum = 100;
                pbPower.Value = args.Battery;
                pbStorage.Maximum = 100;
                pbStorage.Value = args.UsedMem;

                if (sender.PenController?.Protocol == Protocols.V1)
                {
                    txtPenName.Text = args.ModelName;
                }
            });
        }

        private async void MController_OfflineDataListReceived(IPenClient sender, OfflineDataListReceivedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                lvOfflineDataList.Items.Clear();

                foreach (var item in args.OfflineNotes)
                {
                    lvOfflineDataList.Items.Add(item);
                }
            });
        }

        private async void MController_PasswordRequested(IPenClient sender, PasswordRequestedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                ShowPasswordForm(args.RetryCount, args.ResetCount);
            });

            //you can input password immediately, please refer to below code.
            //mController.InputPassword("0000");
        }

        private async void MController_DotReceived(IPenClient sender, DotReceivedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                ProcessDot(args.Dot);
            });
        }

        private async void MController_Authenticated(IPenClient sender, object args)
        {
            _controller.RequestPenStatus();
            _controller.AddAvailableNote();
            _controller.RequestOfflineDataList();

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                // MController_Connected에 있으니 비밀번호 입력창이 뜰때 연결끊김
                // 펜 세팅값으로 넣어줘야 할듯
                cbColor.SelectedIndex = cbColor.Items.Count - 1;
                CurrentStatus = AppStatus.Connected;

                ShowToast("Device is connected");
            });
        }

        private async void MController_Disconnected(IPenClient sender, object args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                ToggleControls(this.Content, false);
                CurrentStatus = AppStatus.Disconnected;
                _progressDialog?.Hide();
            });

            ShowToast("Device is disconnected");
        }

        public float MaxForce = 0f;

        private async void MController_Connected(IPenClient sender, ConnectedEventArgs args)
		{
            MaxForce = args.MaxForce;

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                ToggleControls(this.Content, true);

                if (sender.PenController?.Protocol == Protocols.V1)
                {
                    textBox.Text = String.Format("Firmware Version : {0}", args.FirmwareVersion);
                }
                else
                {
                    textBox.Text = String.Format("Mac : {0}\r\n\r\nName : {1}\r\n\r\nSubName : {2}\r\n\r\nFirmware Version : {3}\r\n\r\nProtocol Version : {4}", args.MacAddress, args.DeviceName, args.SubName, args.FirmwareVersion, args.ProtocolVersion);
                    txtPenName.Text = args.SubName;
                }
            });

            _client.StopLEAdvertisementWatcher();
		}

		private PenProfileReceivedEventArgs lastArgs;
		private void Mcontroller_PenProfileReceived(IPenClient sender, PenProfileReceivedEventArgs args)
		{
			if (args.Result == PenProfileReceivedEventArgs.ResultType.Failed)
			{
				OutputConsole += "PenProfile Failed";
				return;
			}

			if (isTest)
			{
				lastArgs = args;
				autoResetEvent.Set();
				return;
			}
			switch (args.Type)
			{
				case PenProfileReceivedEventArgs.PenProfileType.Create:
					CreatePenProfile(args);
					break;
				case PenProfileReceivedEventArgs.PenProfileType.Delete:
					DeletePenProfile(args);
					break;
				case PenProfileReceivedEventArgs.PenProfileType.Info:
					PenProfileInfo(args);
					break;
				case PenProfileReceivedEventArgs.PenProfileType.ReadValue:
					ReadProfileValue(args);
					ClearKeyValuePenProfile();
					break;
				case PenProfileReceivedEventArgs.PenProfileType.WriteValue:
					WriteProfileValue(args);
					ClearKeyValuePenProfile();
					break;
				case PenProfileReceivedEventArgs.PenProfileType.DeleteValue:
					DeleteProfileValue(args);
					ClearKeyValuePenProfile();
					break;
			}
		}

		private void CreatePenProfile(PenProfileReceivedEventArgs penProfileReceivedEventArgs)
		{
			switch(penProfileReceivedEventArgs.Status)
			{
				case PenProfile.PROFILE_STATUS_SUCCESS:
					OutputConsole += $"Create Success:{penProfileReceivedEventArgs.ProfileName}";
					break;
				case PenProfile.PROFILE_STATUS_FAILURE:
					OutputConsole += $"Create Failure:{penProfileReceivedEventArgs.ProfileName}";
					break;
				case PenProfile.PROFILE_STATUS_EXIST_PROFILE_ALREADY:
					OutputConsole += $"Already existed profile name:{penProfileReceivedEventArgs.ProfileName}";
					break;
				case PenProfile.PROFILE_STATUS_NO_PERMISSION:
					OutputConsole += "Permission Denied. Check your password";
					break;
				default:
					OutputConsole += "Create Error " + penProfileReceivedEventArgs.Status;
					break;
			}
		}

		private void DeletePenProfile(PenProfileReceivedEventArgs penProfileReceivedEventArgs)
		{
			switch (penProfileReceivedEventArgs.Status)
			{
				case PenProfile.PROFILE_STATUS_SUCCESS:
					OutputConsole += $"Delete Success:{penProfileReceivedEventArgs.ProfileName}";
					ClearKeyValuePenProfile();
					break;
				case PenProfile.PROFILE_STATUS_FAILURE:
					OutputConsole += $"Delete Failure:{penProfileReceivedEventArgs.ProfileName}";
					break;
				case PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE:
					OutputConsole += $"Do not exist profile:{penProfileReceivedEventArgs.ProfileName}";
					break;
				case PenProfile.PROFILE_STATUS_NO_PERMISSION:
					OutputConsole += "Permission Denied. Check your password";
					break;
				default:
					OutputConsole += "Delete error " + penProfileReceivedEventArgs.Status;
					break;
			}
		}

		private void PenProfileInfo(PenProfileReceivedEventArgs penProfileReceivedEventArgs)
		{
			switch (penProfileReceivedEventArgs.Status)
			{
				case PenProfile.PROFILE_STATUS_SUCCESS:
					{
						var args = penProfileReceivedEventArgs as PenProfileInfoEventArgs;
						System.Text.StringBuilder strs = new System.Text.StringBuilder();
						strs.Append($"Total Section Count : {args.TotalSectionCount}");
						strs.Append(Environment.NewLine);
						strs.Append($"Section Size : {args.SectionSize}");
						strs.Append(Environment.NewLine);
						strs.Append($"Using Section Count : {args.UseSectionCount}");
						strs.Append(Environment.NewLine);
						strs.Append($"using Key count : {args.UseKeyCount}");
						OutputConsole += strs.ToString();
					}
					break;
				case PenProfile.PROFILE_STATUS_FAILURE:
					OutputConsole += $"Get Info Failure:{penProfileReceivedEventArgs.ProfileName}";
					break;
				case PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE:
					OutputConsole += $"Do not exist profile:{penProfileReceivedEventArgs.ProfileName}";
					break;
				default:
					OutputConsole += "Info Error " + penProfileReceivedEventArgs.Status;
					break;
			}
		}

		private void ReadProfileValue(PenProfileReceivedEventArgs penProfileReceivedEventArgs)
		{
			var args = penProfileReceivedEventArgs as PenProfileReadValueEventArgs;
			foreach(var value in args.Data)
			{
				switch(value.Status)
				{
					case PenProfile.PROFILE_STATUS_SUCCESS:
						OutputConsole += $"key : {value.Key}, Value : {System.Text.Encoding.UTF8.GetString(value.Data)}";
						break;
					case PenProfile.PROFILE_STATUS_FAILURE:
						OutputConsole += $"Read value Failure:key[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE:
						OutputConsole += $"Do not exist profile:{penProfileReceivedEventArgs.ProfileName}";
						break;
					case PenProfile.PROFILE_STATUS_NO_EXIST_KEY:
						OutputConsole += $"Do not exist key:[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_NO_PERMISSION:
						OutputConsole += "Permission Denied. Check your password";
						break;
					default:
						OutputConsole += "Read value Error " + penProfileReceivedEventArgs.Status;
						break;
				}
			}
		}

		private void WriteProfileValue(PenProfileReceivedEventArgs penProfileReceivedEventArgs)
		{
			var args = penProfileReceivedEventArgs as PenProfileWriteValueEventArgs;
			foreach(var value in args.Data)
			{
				switch(value.Status)
				{
					case PenProfile.PROFILE_STATUS_SUCCESS:
						OutputConsole += $"Write Success key:[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_FAILURE:
						OutputConsole += $"Write value Failure key:[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE:
						OutputConsole += $"Do not exist profile:{penProfileReceivedEventArgs.ProfileName}";
						break;
					case PenProfile.PROFILE_STATUS_NO_EXIST_KEY:
						OutputConsole += $"Do not exist key:[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_NO_PERMISSION:
						OutputConsole += "Permission Denied. Check your password";
						break;
					default:
						OutputConsole += "Write value Error " + penProfileReceivedEventArgs.Status;
						break;
				}
			}
		}

		private void DeleteProfileValue(PenProfileReceivedEventArgs penProfileReceivedEventArgs)
		{
			var args = penProfileReceivedEventArgs as PenProfileDeleteValueEventArgs;
			foreach(var value in args.Data)
			{
				switch(value.Status)
				{
					case PenProfile.PROFILE_STATUS_SUCCESS:
						OutputConsole += $"Delete Success key:[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_FAILURE:
						OutputConsole += $"Delete value Failure key:[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE:
						OutputConsole += $"Do not exist profile:{penProfileReceivedEventArgs.ProfileName}";
						break;
					case PenProfile.PROFILE_STATUS_NO_EXIST_KEY:
						OutputConsole += $"Do not exist key:[{value.Key}]";
						break;
					case PenProfile.PROFILE_STATUS_NO_PERMISSION:
						OutputConsole += "Permission Denied. Check your password";
						break;
					default:
						OutputConsole += "Delete value Error " + penProfileReceivedEventArgs.Status;
						break;
				}
			}
		}
		#endregion
	}
}
