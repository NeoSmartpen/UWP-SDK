using Neosmartpen.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace SampleApp
{
	public sealed partial class MainPage
	{
		private static string[] testKeys =
		{
			"aa",
			"bb",
			"cc",
			"dd",
			"ee"
		};
		private static string dataAA = "AATestisGood";
		private static string dataBB = "neolabneolabnoelab";
		private static string dataCC = "asdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasfd";
		private static float dataDD = 123.456f;
		private static int dataEE = 20180101;
		private static byte[][] testData =
		{
			Encoding.UTF8.GetBytes(dataAA),
			Encoding.UTF8.GetBytes(dataBB),
			Encoding.UTF8.GetBytes(dataCC),
			BitConverter.GetBytes(dataDD),
			BitConverter.GetBytes(dataEE)
		};
		private static Dictionary<string, byte[]> testDictionary = new Dictionary<string, byte[]>();
		public string testConsole { get; set; }
		public string TestConsole { get { return testConsole; } set { testConsole = value + Environment.NewLine; NotifyPropertyChanged(); } }
		private async void ButtonPenProfileTest_Click(object sender, RoutedEventArgs e)
		{
			await Task.Factory.StartNew(() =>
			{
				testDictionary.Clear();
				for (int i = 0; i < testKeys.Length; ++i)
				{
					testDictionary.Add(testKeys[i], testData[i]);
				}
				if (!_controller.IsSupportPenProfile())
				{
					OutputPrint("This pen is not supported pen profile");
					return;
				}
				isTest = true;
				if (PenProfileCreateTest() == false)
				{
					OutputPrint($"Pen Profile Create Test FAILED");
					isTest = false;
					return;
				}

				if (PenProfileInfoTest() == false)
				{
					OutputPrint($"Pen Profile Info Test FAILED");
					isTest = false;
					return;
				}

				if (WriteProfileValueTest() == false)
				{
					OutputPrint($"Pen Profile Write value Test FAILED");
					isTest = false;
					return;
				}

				if (ReadProfileValueTest() == false)
				{
					OutputPrint($"Pen Profile Read value Test FAILED");
					isTest = false;
					return;
				}

				if (DeleteProfileValueTest() == false)
				{
					OutputPrint($"Pen Profile Delete Value Test FAILED");
					isTest = false;
					return;
				}

				if (DeleteProfileTest() == false)
				{
					OutputPrint($"Pen Profile Delete Test FAILED");
					isTest = false;
					return;
				}
				isTest = false;
			});
		}

		private async void OutputPrint(string str)
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				TestConsole += str;
			});
		}

		private bool isTest = false;
		private AutoResetEvent autoResetEvent = new AutoResetEvent(false);

		#region TestSet

		private bool PenProfileCreateTest()
		{
			OutputPrint($"##########################################");
			OutputPrint($"Pen Profile Create Test Start");
			OutputPrint("");
			string name = null;
			byte[] password = null;
			OutputPrint($"Create Profile name null");
			try
			{
				_controller.CreateProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_FAILURE)
				{
					return false;
				}
			}
			catch (ArgumentNullException)
			{
				// success
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = "aaaaaa";
			OutputPrint($"Create Profile password null");
			try
			{
				_controller.CreateProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_FAILURE)
				{
					return false;
				}
			}
			catch (ArgumentNullException)
			{
				// success
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			/***
			 * too long profile name
			 */
			name = "aaaaaaaaa";
			password = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			OutputPrint($"Create Profile {name} : Long name");
			try
			{
				_controller.CreateProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_FAILURE)
				{
					return false;
				}
			}
			catch (ArgumentOutOfRangeException)
			{
				// success
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = "한글테스트";
			OutputPrint($"Create Profile {name} : Long name");
			try
			{
				_controller.CreateProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_FAILURE)
				{
					return false;
				}
			}
			catch (ArgumentOutOfRangeException)
			{
				// Success
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = PEN_PROFILE_TEST_NAME;
			OutputPrint($"Create Profile {name} : Permission Denied Test");
			try
			{
				_controller.CreateProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_NO_PERMISSION)
				{
					return false;
				}
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			/***
			 * success
			 */
			password = PEN_PROFILE_TEST_PASSWORD;
			OutputPrint($"Create Profile {name} : Success");
			try
			{
				_controller.CreateProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_SUCCESS)
				{
					return false;
				}
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			/***
			 * Already exsit
			 */
			OutputPrint($"Create Profile {name} : Already exist");
			try
			{
				_controller.CreateProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_EXIST_PROFILE_ALREADY)
				{
					return false;
				}
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			OutputPrint($"Pen Profile Create Test End");
			return true;
		}

		private bool PenProfileInfoTest()
		{
			OutputPrint($"##########################################");
			OutputPrint($"Pen Profile Info Test Start");
			OutputPrint("");

			string name = "aaaaaaaaa";

			/***
			 * too long name
			 */
			OutputPrint($"Profile Info {name} : long name");
			try
			{
				_controller.GetProfileInfo(name);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_FAILURE)
				{
					return false;
				}
			}
			catch (ArgumentOutOfRangeException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			/***
			 * Do not exist
			 */
			name = "aaaaa";
			OutputPrint($"Profile Info {name} : Do not exist");
			try
			{
				_controller.GetProfileInfo(name);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE)
				{
					return false;
				}
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = PEN_PROFILE_TEST_NAME;
			OutputPrint($"Profile Info {name} : Success");
			try
			{
				_controller.GetProfileInfo(name);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_SUCCESS)
				{
					return false;
				}
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			OutputPrint($"Pen Profile Info Test End");
			return true;
		}
		private bool WriteProfileValueTest()
		{
			OutputPrint($"##########################################");
			OutputPrint($"Pen Profile Write Test");
			OutputPrint("");

			string name = null;
			byte[] password = null;
			string[] keys = null;
			byte[][] data = null;

			#region Null Test
			OutputPrint($"Profile Write name is null");
			try
			{
				_controller.WriteProfileValues(name, password, keys, data);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = "aaaa";
			OutputPrint($"Profile Write password is null");
			try
			{
				_controller.WriteProfileValues(name, password, keys, data);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			password = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			OutputPrint($"Profile Write key is null");
			try
			{
				_controller.WriteProfileValues(name, password, keys, data);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			keys = new string[1];
			OutputPrint($"Profile Write data is null");
			try
			{
				_controller.WriteProfileValues(name, password, keys, data);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}
			#endregion

			keys[0] = "aaaaaaaaaaaaaaaaa";

			data = new byte[keys.Length][];
			data[0] = Encoding.UTF8.GetBytes("test");
			OutputPrint($"Profile Write to {name} : Do not exist");
			try
			{
				_controller.WriteProfileValues(name, password, keys, data);
				autoResetEvent.WaitOne();
				var args = lastArgs as PenProfileWriteValueEventArgs;
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || args.Data[0].Status != PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE)
					return false;
			}
			catch (ArgumentOutOfRangeException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = PEN_PROFILE_TEST_NAME;
			password = PEN_PROFILE_TEST_PASSWORD;
			keys = testKeys;
			data = testData;
			//keys = new string[] { keys[3] };
			//data = new byte[][] { testData[2] };
			OutputPrint($"Profile Write to {name} : ");
			try
			{
				_controller.WriteProfileValues(name, password, keys, data);
				autoResetEvent.WaitOne();
				var args = lastArgs as PenProfileWriteValueEventArgs;
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
				foreach(var d in args.Data)
				{
					if (d.Status != PenProfile.PROFILE_STATUS_SUCCESS)
						return false;
				}
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			OutputPrint($"Profile ReWrite to {name} : ");
			try
			{
				_controller.WriteProfileValues(name, password, keys, data);
				autoResetEvent.WaitOne();
				var args = lastArgs as PenProfileWriteValueEventArgs;
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
				foreach(var d in args.Data)
				{
					if (d.Status != PenProfile.PROFILE_STATUS_SUCCESS)
						return false;
				}
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			OutputPrint($"Pen Profile Write Test End");
			return true;
		}
		private bool ReadProfileValueTest()
		{
			OutputPrint($"##########################################");
			OutputPrint($"Pen Profile Read Value Test");
			OutputPrint("");

			string name = null;
			string[] keys = null;

			#region Null Test
			OutputPrint($"Profile Read name is null");
			try
			{
				_controller.ReadProfileValues(name, keys);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = "aaaaa";
			OutputPrint($"Profile Read data is null");
			try
			{
				_controller.ReadProfileValues(name, keys);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}
			#endregion

			name = PEN_PROFILE_TEST_NAME;
			keys = new string[] { "aaaaaaaaaaaaaaaaa" };
			OutputPrint($"Profile Read data key is too long test : {keys}");
			try
			{
				_controller.ReadProfileValues(name, keys);
				autoResetEvent.WaitOne();
				var args = lastArgs as PenProfileReadValueEventArgs;
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || args.Data[0].Status != PenProfile.PROFILE_STATUS_FAILURE)
					return false;
			}
			catch (ArgumentOutOfRangeException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = PEN_PROFILE_TEST_NAME;
			keys = new string[] { "abcde" };
			OutputPrint($"Profile Read data key is not existed : {keys}");
			try
			{
				_controller.ReadProfileValues(name, keys);
				autoResetEvent.WaitOne();
				var args = lastArgs as PenProfileReadValueEventArgs;
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || args.Data[0].Status != PenProfile.PROFILE_STATUS_NO_EXIST_KEY)
					return false;
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = PEN_PROFILE_TEST_NAME;
			keys = testKeys;
			OutputPrint($"Profile Read data Success");
			try
			{
				_controller.ReadProfileValues(name, keys);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
				var args = lastArgs as PenProfileReadValueEventArgs;
				if (args.Data.Count != testKeys.Length)
					return false;

				foreach (var d in args.Data)
				{
					var value = testDictionary[d.Key];
					if (value.SequenceEqual(d.Data) == false)
					{
						return false;
					}
				}
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			OutputPrint($"Pen Profile Read Value Test End");
			return true;
		}

		private bool DeleteProfileValueTest()
		{
			OutputPrint($"##########################################");
			OutputPrint($"Pen Profile Delete Value Test");
			OutputPrint("");

			string name = null;
			byte[] password = null;
			string[] keys = null;

			#region Null Test
			OutputPrint($"Profile Delete name is null");
			try
			{
				_controller.DeleteProfileValues(name, password, keys);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = "aaaaa";
			OutputPrint($"Profile Delete password is null");
			try
			{
				_controller.DeleteProfileValues(name, password, keys);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			password = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			OutputPrint($"Profile Delete Data is null");
			try
			{
				_controller.DeleteProfileValues(name, password, keys);
				autoResetEvent.WaitOne();
				if (lastArgs.Result != PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
			}
			catch (ArgumentNullException)
			{

			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}
			#endregion


			name = PEN_PROFILE_TEST_NAME;
			password = PEN_PROFILE_TEST_PASSWORD;
			keys = testKeys;
			OutputPrint($"Profile Delete Data Sucess");
			try
			{
				_controller.DeleteProfileValues(name, password, keys);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied)
					return false;
				var args = lastArgs as PenProfileDeleteValueEventArgs;
				foreach (var v in args.Data)
				{
					if (v.Status != PenProfile.PROFILE_STATUS_SUCCESS)
					{
						OutputPrint($"key {v.Key} delete failed");
						return false;
					}
				}
			}
			catch (Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			return true;
		}

		private bool DeleteProfileTest()
		{
			OutputPrint($"##########################################");
			OutputPrint($"Pen Profile Delete Test");
			OutputPrint("");

			string name = null;
			byte[] password = null;

			OutputPrint($"Pen Profile name is null");
			try
			{
				_controller.DeleteProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_FAILURE)
					return false;
			}
			catch(ArgumentNullException)
			{

			}
			catch(Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			name = PEN_PROFILE_TEST_NAME;
			OutputPrint($"Pen Profile password is null");
			try
			{
				_controller.DeleteProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_FAILURE)
					return false;
			}
			catch(ArgumentNullException)
			{

			}
			catch(Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			password = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
			OutputPrint($"Pen Profile delete with unabled password");
			try
			{
				_controller.DeleteProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_NO_PERMISSION)
					return false;
			}
			catch(Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}


			password = PEN_PROFILE_TEST_PASSWORD;
			OutputPrint($"Pen Profile delete");
			try
			{
				_controller.DeleteProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_SUCCESS)
					return false;
			}
			catch(Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			OutputPrint($"Pen Profile delete for not exist");
			try
			{
				_controller.DeleteProfile(name, password);
				autoResetEvent.WaitOne();
				if (lastArgs.Result == PenProfileReceivedEventArgs.ResultType.Falied || lastArgs.Status != PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE)
					return false;
			}
			catch(Exception exp)
			{
				OutputPrint($"exception {exp.Message}");
				return false;
			}

			OutputPrint($"Pen Profile Delete Test End");
			return true;
		}
		#endregion
	}
}
