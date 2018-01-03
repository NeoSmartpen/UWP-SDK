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
using Windows.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page, INotifyPropertyChanged
	{
		private ProgressDialog _progressDialog = new ProgressDialog();
		private StorageFile mFile;
		public ObservableCollection<NColor> colors;
		public enum AppStatus
		{
			Disconnected,
			Connected
		}

		public enum PenProfileType
		{
			NONE,
			CreateProfile,
			DeleteProfile,
			ProfileInfo,
			WriteProfileValue,
			ReadProfileValue,
			DeleteProfileValue
		}
		private PenProfileType currentProfileType;

		#region Properties
		private bool isSearching = false;
		public bool IsSearching
		{
			get
			{
				return isSearching;
			}
			set
			{
				isSearching = value;
				btnSearch.Content = isSearching ? "Stop" : "Search";
			}
		}

		private AppStatus currentStatus;

		public AppStatus CurrentStatus
		{
			get
			{
				return currentStatus;
			}
			set
			{
				currentStatus = value;
				NotifyPropertyChanged();
			}
		}

		private string currentMacAddress;
		public string CurrentMacAddress { get { return currentMacAddress; } set { currentMacAddress = value; NotifyPropertyChanged(); } }

		private Visibility profilePasswordVisibility;
		public Visibility ProfilePasswordVisibility { get { return profilePasswordVisibility; } set { profilePasswordVisibility = value; NotifyPropertyChanged(); } }
		private Visibility profileKeyVisibility;
		public Visibility ProfileKeyVisibility { get { return profileKeyVisibility; } set { profileKeyVisibility = value; NotifyPropertyChanged(); } }
		private Visibility profileValueVisibility;
		public Visibility ProfileValueVisibility { get { return profileValueVisibility; } set { profileValueVisibility = value; NotifyPropertyChanged(); } }
		//private string profileName;
		//public string ProfileName { get { return profileName; } set { profileName = value; NotifyPropertyChanged(); } }
		//private string profilePassword;
		//public string ProfilePassword { get { return profilePassword; } set { profilePassword = value; NotifyPropertyChanged(); } }
		private string profileKey;
		public string ProfileKey { get { return profileKey; } set { profileKey = value; NotifyPropertyChanged(); } }
		private string profileValue;
		public string ProfileValue { get { return profileValue; } set { profileValue = value; NotifyPropertyChanged(); } }
		public string outputConsole { get; set; }
		public string OutputConsole { get { return outputConsole; } set { outputConsole = value + Environment.NewLine; NotifyPropertyChanged(); } }
		#endregion

        public MainPage()
        {
            this.InitializeComponent();

			//ApplicationView.PreferredLaunchViewSize = new Size(1024, 1024);
			//ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

			currentProfileType = PenProfileType.NONE;
			cbPenProfileType.ItemsSource = Enum.GetValues(typeof(PenProfileType)).Cast<PenProfileType>().Where(x => x != PenProfileType.NONE).ToList();
			cbPenProfileType.SelectedIndex = 0;

			InitPenClient();
			InitColor();
            InitRenderer();

			CurrentMacAddress = string.Empty;
			CurrentStatus = AppStatus.Disconnected;
			ClearKeyValuePenProfile();
        }

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

        private void cbAutoPoweroffTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = e.AddedItems[0] as ComboBoxItem;

            short numValue = -1;

            bool result = Int16.TryParse(selectedItem.Content as string, out numValue);

            _controller?.SetAutoPowerOffTime(numValue);
        }


        private async void ShowPasswordForm( int tryCount, int maxCount )
        {
            var dialog1 = new PasswordInputDialog();

            dialog1.Title = "Input your password (" + tryCount + "/" + maxCount + ")";

            await dialog1.ShowAsync();

            _controller?.InputPassword(dialog1.Text);
        }

		private void ClearKeyValuePenProfile()
		{
			ProfileKey = string.Empty;
			ProfileValue = string.Empty;
		}

		/***
		 *  UI EVENTS 
		 */
		#region UI Events
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

			PenInformation selected = lvDevices.SelectedItem as PenInformation;

			if (selected == null)
			{
				await ShowMessage("Select your device");
			}
			else
			{
				try
				{
					bool result = await _client.Connect(selected);

					if (!result)
					{
						await ShowMessage("Connection is failure");
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("conection exception : " + ex.Message);
					Debug.WriteLine("conection exception : " + ex.StackTrace);
				}
			}

			btnConnect.IsEnabled = true;
		}

		private void btnDisconnect_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			_client.Disconnect();
		}

		private void lvDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PenInformation selected = lvDevices.SelectedItem as PenInformation;

            if ( selected == null )
            {
                return;
            }

			//tbMacAddress.Text = selected.MacAddress;
			CurrentMacAddress = selected.MacAddress;
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

        private async void btnDownload_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
			OfflineDataInfo d = lvOfflineDataList.SelectedItem as OfflineDataInfo;

			if (d == null)
			{
				await ShowMessage("Select Offline Data Item");
			}

			_controller?.RequestOfflineData(d.Section, d.Owner, d.Note);
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
		private void SliderThickness_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			var slider = sender as Slider;
			ChangeThinknessLevel((int)(slider.Value - 1));
		}

		private void cbPenProfileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if ( e.AddedItems.Count == 1 )
			{
				var type = (PenProfileType)e.AddedItems[0];
				if (currentProfileType != type)
				{
					currentProfileType = type;

					switch (type)
					{
						case PenProfileType.CreateProfile:
							ProfilePasswordVisibility = Visibility.Visible;
							ProfileKeyVisibility = Visibility.Collapsed;
							ProfileValueVisibility = Visibility.Collapsed;
							break;
						case PenProfileType.DeleteProfile:
							ProfilePasswordVisibility = Visibility.Visible;
							ProfileKeyVisibility = Visibility.Collapsed;
							ProfileValueVisibility = Visibility.Collapsed;
							break;
						case PenProfileType.ProfileInfo:
							ProfilePasswordVisibility = Visibility.Collapsed;
							ProfileKeyVisibility = Visibility.Collapsed;
							ProfileValueVisibility = Visibility.Collapsed;
							break;
						case PenProfileType.ReadProfileValue:
							ProfilePasswordVisibility = Visibility.Collapsed;
							ProfileKeyVisibility = Visibility.Visible;
							ProfileValueVisibility = Visibility.Collapsed;
							break;
						case PenProfileType.WriteProfileValue:
							ProfilePasswordVisibility = Visibility.Visible;
							ProfileKeyVisibility = Visibility.Visible;
							ProfileValueVisibility = Visibility.Visible;
							break;
						case PenProfileType.DeleteProfileValue:
							ProfilePasswordVisibility = Visibility.Visible;
							ProfileKeyVisibility = Visibility.Visible;
							ProfileValueVisibility = Visibility.Collapsed;
							break;
					}
				}
			}
		}

		private void ButtonProfileExecute_Click(object sender, RoutedEventArgs e)
		{
			if (currentProfileType != PenProfileType.NONE)
			{
				if (_controller.IsSupportPenProfile())
				{
					try
					{
						switch (currentProfileType)
						{
							case PenProfileType.CreateProfile:
								{
									_controller?.CreateProfile(PEN_PROFILE_TEST_NAME, PEN_PROFILE_TEST_PASSWORD);
								}
								break;
							case PenProfileType.DeleteProfile:
								{
									_controller?.DeleteProfile(PEN_PROFILE_TEST_NAME, PEN_PROFILE_TEST_PASSWORD);
								}
								break;
							case PenProfileType.ProfileInfo:
								{
									_controller?.GetProfileInfo(PEN_PROFILE_TEST_NAME);
								}
								break;
							case PenProfileType.ReadProfileValue:
								{
									if (string.IsNullOrEmpty(ProfileKey))
									{
										OutputConsole += currentProfileType + " Can not execute without key";
										return;
									}
									_controller?.ReadProfileValues(PEN_PROFILE_TEST_NAME, new string[] { ProfileKey });
								}
								break;
							case PenProfileType.WriteProfileValue:
								{
									if (string.IsNullOrEmpty(ProfileKey))
									{
										OutputConsole += currentProfileType + " Can not execute without key";
										return;
									}
									if (string.IsNullOrEmpty(ProfileValue))
									{
										OutputConsole += currentProfileType + " Can not execute without value";
										return;
									}
									var value = System.Text.Encoding.UTF8.GetBytes(ProfileValue);
									_controller?.WriteProfileValues(PEN_PROFILE_TEST_NAME, PEN_PROFILE_TEST_PASSWORD, new string[] { ProfileKey }, new byte[][] { value });
								}
								break;
							case PenProfileType.DeleteProfileValue:
								{
									_controller?.DeleteProfileValues(PEN_PROFILE_TEST_NAME, PEN_PROFILE_TEST_PASSWORD, new string[] { profileKey });
								}
								break;
						}
					}
					catch (Exception exp)
					{
						OutputConsole += exp.Message;
					}
				}
				else
				{
					OutputConsole += "This Firmware is not supported pen profile";
				}
			}
			else
			{
				OutputConsole += "Execute type error";
			}
		}

		private void txtPenProfileOutput_TextChanged(object sender, TextChangedEventArgs e)
		{
			var textBox1 = sender as TextBox;
			var grid = (Grid)VisualTreeHelper.GetChild(textBox1, 0);
			for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
			{
				object obj = VisualTreeHelper.GetChild(grid, i);
				if (!(obj is ScrollViewer)) continue;
				((ScrollViewer)obj).ChangeView(0.0f, ((ScrollViewer)obj).ExtentHeight, 1.0f);
				break;
			}
		}
		#endregion

		/***
		 * Implements NotifyProperty
		 */
		#region NotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		#endregion
	}
}