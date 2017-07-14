using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Neosmartpen.Net;
using Neosmartpen.Net.Bluetooth;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
	{
        private BluetoothPenClient _client;

        private PenController _controller;

        private ProgressDialog _progressDialog = new ProgressDialog();

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(1024, 768);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            // create PenController instance.
            _controller = new PenController();

            // Create BluetoothPenClient instance. and bind PenController.
            // BluetoothPenClient is implementation of bluetooth function.
            _client = new BluetoothPenClient(_controller);

			// bluetooth watcher event
			_client.onAddPenController += MClient_onAddPenController;
			_client.onRemovePenController += MClient_onRemovePenController;
			_client.onStopSearch += MClient_onStopSearch;
			_client.onUpdatePenController += MClient_onUpdatePenController;

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

			InitColor();
            InitRenderer();
        }

		#region Watcher Event
		private async void MClient_onUpdatePenController(BluetoothPenClient sender, PenUpdateInformation args)
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

		private async void MClient_onStopSearch(BluetoothPenClient sender, Windows.Devices.Bluetooth.BluetoothError args)
		{
			Debug.WriteLine("Watcher finidhed");
			await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => IsSearching = false );
		}

		private async void MClient_onRemovePenController(BluetoothPenClient sender, PenUpdateInformation args)
		{
			await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				var item = lvDevices.Items.Where(p => (p as PenInformation)?.Id == args.Id);
				if (item != null)
				{
					PenInformation penInformation = item as PenInformation;
					if (penInformation != null)
					{
						lvDevices.Items.Remove(penInformation);
					}
				}
			});
		}

		private async void MClient_onAddPenController(BluetoothPenClient sender, PenInformation args)
		{
			await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => lvDevices.Items.Add(args) );
		}
		#endregion

		private async void MController_FirmwareInstallationStarted(IPenClient sender, object args)
        {
            _progressDialog.Title = "Firmware Installation";
            await _progressDialog.ShowAsync();
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
            _progressDialog.Title = "Offline Data Downloading";
            await _progressDialog.ShowAsync();
        }

        private async void MController_OfflineDownloadFinished(IPenClient sender, SimpleResultEventArgs args)
        {
            _controller.RequestOfflineDataList();
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => _progressDialog.Hide());
        }

        private void ToggleControls( object obj, bool toggle )
        {
            if (obj is Panel)
            {
                Panel control = obj as Panel;

                foreach (var item in control.Children)
                {
                    ToggleControls(item, toggle);
                }
            }
            else
            {
                Control control = obj as Control;

                if (control!=null && control.Tag != null && control.Tag as string == "CanControl")
                {
                    control.IsEnabled = toggle;
                }
            }
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
            _controller?.RequestPenStatus();
        }

        private void MController_AutoPowerOffTimeChanged(IPenClient sender, SimpleResultEventArgs args)
        {
            _controller?.RequestPenStatus();
        }

        private void MController_PenStatusReceived(IPenClient sender, PenStatusReceivedEventArgs args)
        {
            cbPenCapPowerControl.IsChecked = args.PenCapPower;
            
            foreach ( ComboBoxItem item in cbAutoPoweroffTime.Items )
            {
                short numValue = -1; 
                    
                bool result = Int16.TryParse(item.Content as string, out numValue);

                if ( args.AutoShutdownTime == numValue)
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
            cbFSRStep.SelectedIndex = args.PenSensitivity - 1;
        }

        private void MController_OfflineDataListReceived(IPenClient sender, OfflineDataListReceivedEventArgs args)
        {
            lvOfflineDataList.Items.Clear();

            foreach ( var item in args.OfflineNotes )
            {
                lvOfflineDataList.Items.Add(item);
            }
        }

        private void MController_PasswordRequested(IPenClient sender, PasswordRequestedEventArgs args)
        {
            ShowPasswordForm( args.RetryCount, args.ResetCount );

            //you can input password immediately, please refer to below code.
            //mController.InputPassword("0000");
        }

        private void MController_DotReceived(IPenClient sender, DotReceivedEventArgs args)
        {
			ProcessDot(args.Dot);
        }

        private void MController_Authenticated(IPenClient sender, object args)
        {
            _controller.RequestPenStatus();
            _controller.AddAvailableNote();
            _controller.RequestOfflineDataList();

            // MController_Connected에 있으니 비밀번호 입력창이 뜰때 연결끊김
            // 펜 세팅값으로 넣어줘야 할듯
            cbColor.SelectedIndex = cbColor.Items.Count - 1;

            ShowToast("Device is connected");
        }

        private async void MController_Disconnected(IPenClient sender, object args)
        {
            ToggleControls(this.Content, false);

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                _progressDialog?.Hide();
                btnConnect.Content = "Connect";
            });
 
            ShowToast("Device is disconnected");
        }

        private void MController_Connected(IPenClient sender, ConnectedEventArgs args)
        {
            ToggleControls( this.Content, true );

            if ( args.DeviceName == null ) 
            {
                textBox.Text = String.Format("Firmware Version : {0}", args.FirmwareVersion);
            }
            else
            {
                textBox.Text = String.Format("Mac : {0}\r\n\r\nName : {1}\r\n\r\nSubName : {2}\r\n\r\nFirmware Version : {3}\r\n\r\nProtocol Version : {4}", args.MacAddress, args.DeviceName, args.SubName, args.FirmwareVersion, args.ProtocolVersion);
            }

			_client.StopWatcher();
		}

		public bool IsSearching
		{
			get
			{
				return isSearching;
			}
			set
			{
				isSearching = value;
				if (isSearching == true)
					btnSearch.Content = "Stop";
				else
					btnSearch.Content = "Search";

			}
		}
		private bool isSearching = false;

        private void btnSearch_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
			if ( isSearching == false )
			{
				lvDevices.Items.Clear();

				_client.StartWatcher();

				IsSearching = true;
			}
			else
			{
				_client.StopWatcher();
			}
        }


        private async void btnSearchPaired_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _client.StopWatcher();

            (sender as Button).IsEnabled = false;

            List<PenInformation> result = await _client.FindPairedDevices();

            lvDevices.Items.Clear();

            foreach (PenInformation item in result)
            {
            	lvDevices.Items.Add(item);
            }

            (sender as Button).IsEnabled = true;
        }


        private async void btnConnect_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Button btnConnect = (sender as Button);

            btnConnect.IsEnabled = false;

            if ( btnConnect.Content as string == "Disconnect" )
            {
                _client.Disconnect();

                btnConnect.Content = "Connect";

				_client.StopWatcher();
            }
            else
            {
                PenInformation selected = lvDevices.SelectedItem as PenInformation;

                if ( selected == null )
                {
                    await ShowMessage("Select your device");
                }
                else
                {
                    try
                    {
                        bool result = await _client.Connect(selected);

                        if ( !result )
                        {
                            await ShowMessage("Connection is failure");
                        }
                        else
                        {
                            btnConnect.Content = "Disconnect";
                        }
                    }
                    catch ( Exception ex )
                    {
                        Debug.WriteLine("conection exception : " + ex.Message);
                        Debug.WriteLine("conection exception : " + ex.StackTrace);
                    }
                }
            }

            btnConnect.IsEnabled = true;
        }

        private IAsyncOperation<IUICommand> ShowMessage(string text)
        {
            var dialog = new MessageDialog(text);
            return dialog.ShowAsync();
        }

        private void ShowToast(string text)
        {
            ToastNotificationManager.History.Clear();
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(text));
            IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode).SetAttribute("duration", "short");
            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void lvDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PenInformation selected = lvDevices.SelectedItem as PenInformation;

            if ( selected == null )
            {
                return;
            }

            tbMacAddress.Text = selected.MacAddress;
        }

        private void cbControl_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetCheckBox(sender, true);
        }

        private void cbControl_Unchecked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetCheckBox(sender, false);
        }

        private void SetCheckBox(object sender, bool enable)
        {
            try
            {
                CheckBox checkbox = sender as CheckBox;

                switch (checkbox.Name)
                {
                    case "cbPenCapPowerControl":
                        _controller?.SetPenCapPowerOnOffEnable(enable);
                        break;

                    case "cbPowerOnByPenTip":
                        _controller?.SetAutoPowerOnEnable(enable);
                        break;

                    case "cbBeepSound":
                        _controller?.SetBeepSoundEnable(enable);
                        break;

                    case "cbHover":
                        _controller?.SetHoverEnable(enable);
                        break;

                    case "cbOfflineData":
                        _controller?.SetOfflineDataEnable(enable);
                        break;
                }
            }
            catch ( Exception ex )
            {
                //ShowToast("오류가 발생했습니다.");

                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);

                _controller?.RequestPenStatus();
            }
        }

        private void cbAutoPoweroffTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = e.AddedItems[0] as ComboBoxItem;

            short numValue = -1;

            bool result = Int16.TryParse(selectedItem.Content as string, out numValue);

            _controller?.SetAutoPowerOffTime(numValue);
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ToggleControls(this.Content, false);
        }

        private async void btnSubmitPassword_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if ( txtOldPassword.Text.Equals("") && txtNewPassword.Text.Equals(""))
            {
                await ShowMessage("Input your password");
                return;
            }

            if (txtNewPassword.Text.Equals("0000") )
            {
                await ShowMessage("0000 is not allowed");
                return;
            }

            Debug.WriteLine("btnSubmitPassword_Click");

			string oldPass = txtOldPassword.Text;
            string newPass = txtNewPassword.Text;

            _controller?.SetPassword(oldPass, newPass);

			txtOldPassword.Text = "";
			txtNewPassword.Text = "";
        }

        private async void btnFirmwareUpdate_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if ( txtFirmwareFile.Text.Equals("") )
            {
                await ShowMessage("Select firmware binary file");
                txtFirmwareFile.Focus(Windows.UI.Xaml.FocusState.Pointer);
                return;
            }

            if ( txtFirmwareVersion.Text.Equals("") )
            {
                await ShowMessage("Input version of firmware");
                txtFirmwareVersion.Focus(Windows.UI.Xaml.FocusState.Keyboard);
                return;
            }

            _controller?.RequestFirmwareInstallation(mFile, txtFirmwareVersion.Text );
        }

        StorageFile mFile;

        private async void txtFirmwareFile_GotFocus(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            // Clear previous returned file name, if it exists, between iterations of this scenario
            tb.Text = string.Empty;

            txtFirmwareVersion.Focus(Windows.UI.Xaml.FocusState.Keyboard);

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("._v_");
            openPicker.FileTypeFilter.Add(".bin");

            StorageFile file = await openPicker.PickSingleFileAsync();

            if ( file != null )
            {
                // Application now has read/write access to the picked file
                tb.Text = file.Path;
                mFile = file;
            }
            else
            {
                tb.Text = string.Empty;
                mFile = null;
            }
        }

        private async void ShowPasswordForm( int tryCount, int maxCount )
        {
            var dialog1 = new PasswordInputDialog();

            dialog1.Title = "Input your password (" + tryCount + "/" + maxCount + ")";

            await dialog1.ShowAsync();

            _controller?.InputPassword(dialog1.Text);
        }

        private async void btnDownload_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
			OfflineDataInfo d = lvOfflineDataList.SelectedItem as OfflineDataInfo;

			if (d == null)
			{
				await ShowMessage("Select Offline Data Item");
			}

			_controller?.RequestOfflineData(d.Section, d.Owner, d.Note);
		}

		public ObservableCollection<NColor> colors;

		public void InitColor()
		{
			if (colors == null)
				colors = new ObservableCollection<NColor>();

			colors.Clear();

			int size = NColor.AllColor.Length;
			for (int i = 0; i < size; ++i)
			{
				colors.Add(new NColor(i));
			}
		}

		private void cbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var color = cbColor.SelectedItem as NColor;
			if (color == null)
				return;
			_controller?.SetColor(color.Color);
			_color = color.RealColor;
		}

        private void cbFSRStep_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem newItem = e.AddedItems[0] as ComboBoxItem;

            short value = Int16.Parse((string)newItem.Content);

            _controller?.SetSensitivity(value);
        }

        private async void btnDeletePaired_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            PenInformation selected = lvDevices.SelectedItem as PenInformation;
            await _client.UnPairing(selected);
            lvDevices.Items.Remove(lvDevices.SelectedItem);
        }
    }
}