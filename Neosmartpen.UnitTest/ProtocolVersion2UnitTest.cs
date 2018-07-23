using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neosmartpen.Net;
using Neosmartpen.Net.Bluetooth;
using Neosmartpen.Net.Support;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Neosmartpen.UnitTest
{
    [TestClass]
    public class ProtocolVersion2UnitTest
    {
        private static BluetoothPenClient _client;

        private static PenController _controller;

        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        public const int TEST_TIMEOUT = 15000;

        public const string MAC = "9C:7B:D2:EE:E0:21";

        public const string PASSWORD = "1234";

        public const int DEFAULT_SECTION = 3;
        public const int DEFAULT_OWNER = 27;
        public const int DEFAULT_NOTE = 603;

        // Enter the firmware file name and version and copy the file to the C:\Users\...\Pictures folder.
        public const string FIRMWARE_FILENAME = "NWP-F70_1.00.0105._v_";
        public const string FIRMWARE_VERSION= "1.00.0105";

        [TestInitialize]
        public async Task SetUp()
        {
            FinalizeClient();

            await Task.Delay(1000);

            _controller = new PenController();

            _client = new BluetoothPenClient(_controller);

            TypedEventHandler<IPenClient, object> authenticated = new TypedEventHandler<IPenClient, object>((IPenClient sender, object obj) =>
            {
                _autoResetEvent.Set();
            });

            _controller.Authenticated += authenticated;

            TypedEventHandler<IPenClient, PasswordRequestedEventArgs> passwordRequested = new TypedEventHandler<IPenClient, PasswordRequestedEventArgs>((IPenClient sender, PasswordRequestedEventArgs args) =>
            {
                _controller.InputPassword(PASSWORD);
            });

            _controller.PasswordRequested += passwordRequested;

            _client.Connect(MAC).Wait();

            if (!_client.Alive)
            {
                Assert.Fail("connection failed");
                return;
            }

            _autoResetEvent.WaitOne();

            _controller.Authenticated -= authenticated;
            _controller.PasswordRequested -= passwordRequested;
        }

        [TestCleanup]
        public void SetDown()
        {
        }

        private void FinalizeClient()
        {
            if (_client != null && _client.Alive)
            {
                _client.Disconnect();
            }

            _controller = null;
            _client = null;
        }

        #region offline data test

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestOfflineDataList()
        {
            bool result = false;

            _controller.OfflineDataListReceived += (IPenClient sender, OfflineDataListReceivedEventArgs args) =>
            {
                result = true;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.RequestOfflineDataList();
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestOfflineDataRemove()
        {
            bool result = false;

            _controller.OfflineDataRemoved += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.RequestRemoveOfflineData(DEFAULT_SECTION, DEFAULT_OWNER, new int[] { DEFAULT_NOTE });
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestOfflineDataDownload()
        {
            bool result = false;

            _controller.OfflineDownloadFinished += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.RequestOfflineData(DEFAULT_SECTION, DEFAULT_OWNER, DEFAULT_NOTE);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        #endregion

        #region setting

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestAutoPowerOnSetup()
        {
            bool result = false;

            _controller.AutoPowerOnChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetAutoPowerOnEnable(false);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestAutoShutdownTimeSetup()
        {
            bool result = false;

            _controller.AutoPowerOffTimeChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetAutoPowerOffTime(20);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenBeepAndLightSetup()
        {
            bool result = false;

            _controller.BeepAndLightChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.RequestBeepAndLight();
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenBeepSetup()
        {
            bool result = false;

            _controller.BeepSoundChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetBeepSoundEnable(true);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenBtLocalNameSetup()
        {
            bool result = false;

            _controller.BtLocalNameChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetBtLocalName("NeosmartpenLINE");
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenCapPowerOnOffSetup()
        {
            bool result = false;

            _controller.PenCapPowerOnOffChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetPenCapPowerOnOffEnable(true);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenColorSetup()
        {
            bool result = false;

            _controller.PenColorChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetColor(0x00000000);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenDataTransmissionTypeSetup()
        {
            bool result = false;

            _controller.DataTransmissionTypeChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetDataTransmissionType(DataTransmissionType.Event);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenDownSamplingSetup()
        {
            bool result = false;

            _controller.DownSamplingChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetDownSampling(false);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenFscSensitivitySetup()
        {
            bool result = false;

            _controller.FscSensitivityChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetFscSensitivity(1);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenOfflineDataSetup()
        {
            bool result = false;

            _controller.OfflineDataChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetOfflineDataEnable(true);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenSensitivitySetup()
        {
            bool result = false;

            _controller.SensitivityChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetSensitivity(0);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenTimestampSetup()
        {
            bool result = false;

            _controller.RtcTimeChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetRtcTime(Time.GetUtcTimeStamp());
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenUsbModeSetup()
        {
            bool result = false;

            _controller.UsbModeChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            };

            Task.Factory.StartNew(() =>
            {
                _controller.SetUsbMode(UsbMode.Disk);
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenStatus()
        {
            bool result = false;

            _controller.PenStatusReceived += (IPenClient sender, PenStatusReceivedEventArgs args) =>
            {
                result = true;
                _autoResetEvent.Set();
            };
                
            Task.Factory.StartNew(() =>
            {
                _controller.RequestPenStatus();
            });

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result);
        }

        #endregion

        #region password

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestSetUpPassword()
        {
            bool result1 = _controller.SetPassword(null, null);

            Assert.IsFalse(result1);

            bool result2 = _controller.SetPassword("0000", PASSWORD);

            Assert.IsFalse(result2);

            bool result3 = _controller.SetPassword(PASSWORD, "0000");

            Assert.IsFalse(result3);

            bool firstChange = false;

            TypedEventHandler<IPenClient, SimpleResultEventArgs> passwordChanged;

            passwordChanged = new TypedEventHandler<IPenClient, SimpleResultEventArgs>((IPenClient sender, SimpleResultEventArgs args) =>
            {
                firstChange = args.Result;
                _autoResetEvent.Set();
            });

            _controller.PasswordChanged += passwordChanged;

            Task.Factory.StartNew(() =>
            {
                //1234로 비밀번호 변경
                _controller.SetPassword("", PASSWORD);
            });

            _autoResetEvent.WaitOne();

            _controller.PasswordChanged -= passwordChanged;

            bool secondChange = false;

            passwordChanged = new TypedEventHandler<IPenClient, SimpleResultEventArgs>((IPenClient sender, SimpleResultEventArgs args) =>
            {
                secondChange = true;
                _autoResetEvent.Set();
            });

            _controller.PasswordChanged += passwordChanged;

            Task.Factory.StartNew(() =>
            {
                //비밀번호 삭제
                _controller.SetPassword(PASSWORD, "");
            });

            _autoResetEvent.WaitOne();

            _controller.PasswordChanged -= passwordChanged;

            Assert.IsTrue(firstChange && secondChange);
        }

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestInputPassword()
        {
            bool result = false;

            _controller.PasswordChanged += (IPenClient sender, SimpleResultEventArgs args) =>
            {
                _client.Disconnect();
                _autoResetEvent.Set();
            };

            _controller.Authenticated += (IPenClient sender, object args) =>
            {
                result = true;
                _autoResetEvent.Set();
            };

            _controller.PasswordRequested += (IPenClient sender, PasswordRequestedEventArgs args) =>
            {
                _controller.InputPassword(PASSWORD);
            };

            Task.Factory.StartNew(() =>
            {
                //1234로 비밀번호 변경
                _controller.SetPassword("", PASSWORD);
            });

            _autoResetEvent.WaitOne();

            _client.Connect(MAC).Wait();

            bool connResult = _client.Alive;

            _autoResetEvent.WaitOne();

            Assert.IsTrue(result && connResult);
        }

        #endregion

        #region note

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestAvailableNoteRequest()
        {
            bool firstCheck = false;

            TypedEventHandler<IPenClient, object> availableNoteAccepted;

            availableNoteAccepted = new TypedEventHandler<IPenClient, object>((IPenClient sender, object args)=>
            {
                firstCheck = true;
                _autoResetEvent.Set();
            });

            _controller.AvailableNoteAdded += availableNoteAccepted;    

            Task.Factory.StartNew(() =>
            {
                _controller.AddAvailableNote();
            });

            _autoResetEvent.WaitOne();

            _controller.AvailableNoteAdded -= availableNoteAccepted;

            bool secondCheck = false;

            availableNoteAccepted = new TypedEventHandler<IPenClient, object>((IPenClient sender, object args) =>
            {
                secondCheck = true;
                _autoResetEvent.Set();
            });

            _controller.AvailableNoteAdded += availableNoteAccepted;

            Task.Factory.StartNew(() =>
            {
                _controller.AddAvailableNote(DEFAULT_SECTION, DEFAULT_OWNER, new int[] { DEFAULT_NOTE });
            });

            _autoResetEvent.WaitOne();

            _controller.AvailableNoteAdded -= availableNoteAccepted;

            bool thirdCheck = false;

            availableNoteAccepted = new TypedEventHandler<IPenClient, object>((IPenClient sender, object args) =>
            {
                thirdCheck = true;
                _autoResetEvent.Set();
            });

            _controller.AvailableNoteAdded += availableNoteAccepted;

            Task.Factory.StartNew(() =>
            {
                _controller.AddAvailableNote(new int[] { DEFAULT_SECTION }, new int[] { DEFAULT_OWNER });
            });

            _autoResetEvent.WaitOne();

            _controller.AvailableNoteAdded -= availableNoteAccepted;

            bool fourthCheck = false;

            availableNoteAccepted = new TypedEventHandler<IPenClient, object>((IPenClient sender, object args) =>
            {
                fourthCheck = true;
                _autoResetEvent.Set();
            });

            _controller.AvailableNoteAdded += availableNoteAccepted;

            Task.Factory.StartNew(() =>
            {
                _controller.AddAvailableNote(DEFAULT_SECTION, DEFAULT_OWNER);
            });

            _autoResetEvent.WaitOne();

            _controller.AvailableNoteAdded -= availableNoteAccepted;

            bool fifthCheck = false;

            availableNoteAccepted = new TypedEventHandler<IPenClient, object>((IPenClient sender, object args) =>
            {
                fifthCheck = true;
                _autoResetEvent.Set();
            });

            _controller.AvailableNoteAdded += availableNoteAccepted;

            Task.Factory.StartNew(() =>
            {
                _controller.AddAvailableNote(DEFAULT_SECTION, DEFAULT_OWNER, null);
            });

            _autoResetEvent.WaitOne();

            _controller.AvailableNoteAdded -= availableNoteAccepted;

            bool sixthCheck = false;

            availableNoteAccepted = new TypedEventHandler<IPenClient, object>((IPenClient sender, object args) =>
            {
                sixthCheck = true;
                _autoResetEvent.Set();
            });

            _controller.AvailableNoteAdded += availableNoteAccepted;

            Task.Factory.StartNew(() =>
            {
                _controller.AddAvailableNote(DEFAULT_SECTION, DEFAULT_OWNER, new int[] { 601, 602, 603, 604, 605, 606, 607, 608, 609, 610 });
            });

            _autoResetEvent.WaitOne();

            _controller.AvailableNoteAdded -= availableNoteAccepted;

            Assert.IsTrue(firstCheck && secondCheck && thirdCheck && fourthCheck && fifthCheck && sixthCheck);
        }

        #endregion

        #region fw update

        [TestMethod]
        public void TestUpdateFirmware()
        {
            Assert.IsTrue(TestUpdateCancel());
            Assert.IsTrue(TestUpdate());
        }

        private bool TestUpdate()
        {
            bool started = false;
            bool result = false;
            bool requestResult = false;

            TypedEventHandler<IPenClient, object> firmwareInstallationStarted;
            TypedEventHandler<IPenClient, ProgressChangeEventArgs> firmwareInstallationStatusUpdated;
            TypedEventHandler<IPenClient, SimpleResultEventArgs> firmwareInstallationFinished;

            firmwareInstallationStarted = new TypedEventHandler<IPenClient, object>((IPenClient sender, object args) =>
            {
                requestResult = true;
            });

            _controller.FirmwareInstallationStarted += firmwareInstallationStarted;

            firmwareInstallationStatusUpdated = new TypedEventHandler<IPenClient, ProgressChangeEventArgs>((IPenClient sender, ProgressChangeEventArgs args) =>
            {
                started = true;
                Debug.WriteLine(args.AmountDone + " / " + args.Total);
            });

            _controller.FirmwareInstallationStatusUpdated += firmwareInstallationStatusUpdated;

            firmwareInstallationFinished = new TypedEventHandler<IPenClient, SimpleResultEventArgs>((IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            });

            _controller.FirmwareInstallationFinished += firmwareInstallationFinished;

            Task.Factory.StartNew(async () =>
            {
                StorageFile file = await KnownFolders.PicturesLibrary.GetFileAsync(FIRMWARE_FILENAME);
                _controller.RequestFirmwareInstallation(file, FIRMWARE_VERSION);
            });

            _autoResetEvent.WaitOne();

            _controller.FirmwareInstallationStarted -= firmwareInstallationStarted;
            _controller.FirmwareInstallationStatusUpdated -= firmwareInstallationStatusUpdated;
            _controller.FirmwareInstallationFinished -= firmwareInstallationFinished;

            return requestResult && started && result;
        }

        private bool TestUpdateCancel()
        {
            bool result = false;

            TypedEventHandler<IPenClient, ProgressChangeEventArgs> firmwareInstallationStatusUpdated;
            TypedEventHandler<IPenClient, SimpleResultEventArgs> firmwareInstallationFinished;

            firmwareInstallationStatusUpdated = new TypedEventHandler<IPenClient, ProgressChangeEventArgs>((IPenClient sender, ProgressChangeEventArgs args) =>
            {
                _controller.SuspendFirmwareInstallation();
            });

            _controller.FirmwareInstallationStatusUpdated += firmwareInstallationStatusUpdated;

            firmwareInstallationFinished = new TypedEventHandler<IPenClient, SimpleResultEventArgs>((IPenClient sender, SimpleResultEventArgs args) =>
            {
                result = args.Result;
                _autoResetEvent.Set();
            });

            _controller.FirmwareInstallationFinished += firmwareInstallationFinished;

            Task.Factory.StartNew(async () =>
            {
                StorageFile file = await KnownFolders.PicturesLibrary.GetFileAsync(FIRMWARE_FILENAME);
                _controller.RequestFirmwareInstallation(file, FIRMWARE_VERSION);
            });

            _autoResetEvent.WaitOne();

            _controller.FirmwareInstallationStatusUpdated -= firmwareInstallationStatusUpdated;
            _controller.FirmwareInstallationFinished -= firmwareInstallationFinished;

            return !result;
        }

        #endregion

        #region pen profile

        public readonly static string PROFILE_NAME = "neolab_t";
        public readonly static string PROFILE_NAME_LONG = "aaaaaaaaaaaa";
        public readonly static string PROFILE_NAME_INVALID = "abcd";

        public readonly static byte[] PROFILE_PASS = new byte[] { 0x3E, 0xD5, 0x95, 0x25, 0x06, 0xF7, 0x83, 0xDD };
        public readonly static byte[] PROFILE_PASS_LONG = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public readonly static byte[] PROFILE_PASS_INVALID = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public readonly static string[] PROFILE_VALUE_KEYS = new string[] { "harry", "sally" };
        public readonly static string[] PROFILE_VALUE_KEYS_INVALID = new string[] { "john", "doe" };
        public readonly static byte[][] PROFILE_VALUE_VALUES = new byte[][] { Encoding.UTF8.GetBytes("harrys password"), Encoding.UTF8.GetBytes("sally password") };

        [TestMethod]
        [Timeout(TEST_TIMEOUT)]
        public void TestPenProfile()
        {
            // 프로파일 생성
            Assert.IsTrue(PenProfileCreateParamNullTest());
            Assert.IsTrue(PenProfileCreateParamLongTest());
            Assert.IsTrue(PenProfileCreatePermissionDeniedTest());
            Assert.IsTrue(PenProfileCreateSuccessTest());
            Assert.IsTrue(PenProfileCreateAlreadyExistsTest());

            // 프로파일 조회
            Assert.IsTrue(PenProfileInfoParamTest());
            Assert.IsTrue(PenProfileInfoTest());

            // 프로파일 값 생성
            Assert.IsTrue(PenProfileWriteValueParamNullTest());
            Assert.IsTrue(PenProfileWriteValueParamLongTest());
            Assert.IsTrue(PenProfileWriteValuePermissionDeniedTest());
            Assert.IsTrue(PenProfileWriteValueSuccessTest());

            // 프로파일 값 조회
            Assert.IsTrue(PenProfileReadValueParamTest());
            Assert.IsTrue(PenProfileReadValueTest());

            // 프로파일 값 삭제
            Assert.IsTrue(PenProfileDeleteValueParamNullTest());
            Assert.IsTrue(PenProfileDeleteValueParamLongTest());
            Assert.IsTrue(PenProfileDeleteValueInvalidPasswordTest());
            Assert.IsTrue(PenProfileDeleteValueProfileNotExistsTest());
            Assert.IsTrue(PenProfileDeleteValueNotExistsTest());
            Assert.IsTrue(PenProfileDeleteValueSuccessTest());

            // 프로파일 삭제
            Assert.IsTrue(PenProfileDeleteParamNullTest());
            Assert.IsTrue(PenProfileDeleteParamLongTest());
            Assert.IsTrue(PenProfileDeleteInvalidPasswordTest());
            Assert.IsTrue(PenProfileDeleteSuccessTest());
            Assert.IsTrue(PenProfileDeleteNameNotExistsTest());
        }

        #region pen profile create test

        private bool PenProfileCreateParamNullTest()
        {
            bool resultPassNull = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.CreateProfile(PROFILE_NAME, null);
                }
                catch (ArgumentNullException)
                {
                    resultPassNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultNameNull = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.CreateProfile(null, PROFILE_PASS);
                }
                catch (ArgumentNullException)
                {
                    resultNameNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultPassNull && resultNameNull;
        }

        private bool PenProfileCreateParamLongTest()
        {
            bool resultNameLong = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.CreateProfile(PROFILE_NAME_LONG, PROFILE_PASS);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultNameLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultPassLong = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.CreateProfile(PROFILE_NAME, PROFILE_PASS_LONG);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultPassLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultNameLong && resultPassLong;
        }

        private bool PenProfileCreatePermissionDeniedTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Status == PenProfile.PROFILE_STATUS_NO_PERMISSION)
                {
                    result = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.CreateProfile(PROFILE_NAME, PROFILE_PASS_INVALID);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileCreateSuccessTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success && arg.Status == PenProfile.PROFILE_STATUS_SUCCESS)
                {
                    result = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.CreateProfile(PROFILE_NAME, PROFILE_PASS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileCreateAlreadyExistsTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success && arg.Status == PenProfile.PROFILE_STATUS_EXIST_PROFILE_ALREADY)
                {
                    result = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.CreateProfile(PROFILE_NAME, PROFILE_PASS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        #endregion

        #region pen profile info test

        private bool PenProfileInfoParamTest()
        {
            bool resultNameNull = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.GetProfileInfo(null);
                }
                catch (ArgumentNullException)
                {
                    resultNameNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultNameLong = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.GetProfileInfo(PROFILE_NAME_LONG);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultNameLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultNameNull && resultNameLong;
        }

        private bool PenProfileInfoTest()
        {
            bool resultNotExists = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success && arg.Status == PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE)
                {
                    resultNotExists = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.GetProfileInfo(PROFILE_NAME_INVALID);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultExists = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success && arg.Status == PenProfile.PROFILE_STATUS_SUCCESS)
                {
                    resultExists = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.GetProfileInfo(PROFILE_NAME);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultNotExists && resultExists;
        }

        #endregion

        #region pen profile delete test

        private bool PenProfileDeleteParamNullTest()
        {
            bool resultPassNull = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfile(PROFILE_NAME, null);
                }
                catch (ArgumentNullException)
                {
                    resultPassNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultNameNull = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfile(null, PROFILE_PASS);
                }
                catch (ArgumentNullException)
                {
                    resultNameNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultPassNull && resultNameNull;
        }

        private bool PenProfileDeleteParamLongTest()
        {
            bool resultNameLong = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfile(PROFILE_NAME_LONG, PROFILE_PASS);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultNameLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultPassLong = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfile(PROFILE_NAME, PROFILE_PASS_LONG);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultPassLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultNameLong && resultPassLong;
        }

        private bool PenProfileDeleteNameNotExistsTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success && arg.Status == PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE)
                {
                    result = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfile(PROFILE_NAME, PROFILE_PASS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileDeleteInvalidPasswordTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success && arg.Status == PenProfile.PROFILE_STATUS_NO_PERMISSION)
                {
                    result = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfile(PROFILE_NAME, PROFILE_PASS_INVALID);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileDeleteSuccessTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReceivedEventArgs arg = args as PenProfileReceivedEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success && arg.Status == PenProfile.PROFILE_STATUS_SUCCESS)
                {
                    result = true;
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfile(PROFILE_NAME, PROFILE_PASS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        #endregion

        #region pen profile write value test

        private bool PenProfileWriteValueParamNullTest()
        {
            bool resultPassNull = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.WriteProfileValues(PROFILE_NAME, null, PROFILE_VALUE_KEYS, PROFILE_VALUE_VALUES);
                }
                catch (ArgumentNullException)
                {
                    resultPassNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultNameNull = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.WriteProfileValues(null, PROFILE_PASS, PROFILE_VALUE_KEYS, PROFILE_VALUE_VALUES);
                }
                catch (ArgumentNullException)
                {
                    resultNameNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultPassNull && resultNameNull;
        }

        private bool PenProfileWriteValueParamLongTest()
        {
            bool resultNameLong = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.WriteProfileValues(PROFILE_NAME_LONG, PROFILE_PASS, PROFILE_VALUE_KEYS, PROFILE_VALUE_VALUES);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultNameLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultPassLong = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.WriteProfileValues(PROFILE_NAME, PROFILE_PASS_LONG, PROFILE_VALUE_KEYS, PROFILE_VALUE_VALUES);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultPassLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultNameLong && resultPassLong;
        }

        private bool PenProfileWriteValuePermissionDeniedTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileWriteValueEventArgs arg = args as PenProfileWriteValueEventArgs;

                if (arg.Result != PenProfileReceivedEventArgs.ResultType.Failed)
                {
                    foreach (var d in arg.Data)
                    {
                        if (d.Status == PenProfile.PROFILE_STATUS_NO_PERMISSION)
                        {
                            result = true;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.WriteProfileValues(PROFILE_NAME, PROFILE_PASS_INVALID, PROFILE_VALUE_KEYS, PROFILE_VALUE_VALUES);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileWriteValueSuccessTest()
        {
            bool result = true;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileWriteValueEventArgs arg = args as PenProfileWriteValueEventArgs;

                if (arg.Result != PenProfileReceivedEventArgs.ResultType.Failed)
                {
                    foreach (var d in arg.Data)
                    {
                        if (d.Status != PenProfile.PROFILE_STATUS_SUCCESS)
                        {
                            result = false;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.WriteProfileValues(PROFILE_NAME, PROFILE_PASS, PROFILE_VALUE_KEYS, PROFILE_VALUE_VALUES);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        #endregion pen profile write value test

        #region pen profile read value test

        private bool PenProfileReadValueParamTest()
        {
            bool resultNameNull = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.ReadProfileValues(null, PROFILE_VALUE_KEYS);
                }
                catch (ArgumentNullException)
                {
                    resultNameNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultNameLong = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.ReadProfileValues(PROFILE_NAME_LONG, PROFILE_VALUE_KEYS);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultNameLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultNameNull && resultNameLong;
        }

        private bool PenProfileReadValueTest()
        {
            // 프로파일 명이 올바르지 않을때

            bool resultProfileNotExists = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReadValueEventArgs arg = args as PenProfileReadValueEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success)
                {
                    foreach (var d in arg.Data)
                    {
                        if (d.Status == PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE)
                        {
                            resultProfileNotExists = true;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.ReadProfileValues(PROFILE_NAME_INVALID, PROFILE_VALUE_KEYS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            // 프로파일의 키가 존재 하지 않을때

            bool resultKeyNotExists = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReadValueEventArgs arg = args as PenProfileReadValueEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success)
                {
                    foreach (var d in arg.Data)
                    {
                        if (d.Status == PenProfile.PROFILE_STATUS_NO_EXIST_KEY)
                        {
                            resultKeyNotExists = true;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.ReadProfileValues(PROFILE_NAME, PROFILE_VALUE_KEYS_INVALID);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            // 프로파일 키가 존재하여 값을 잘 얻어올때

            bool resultKeyExists = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileReadValueEventArgs arg = args as PenProfileReadValueEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success)
                {
                    resultKeyExists = true;

                    foreach (var d in arg.Data)
                    {
                        if (d.Status != PenProfile.PROFILE_STATUS_SUCCESS)
                        {
                            resultKeyExists = false;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.ReadProfileValues(PROFILE_NAME, PROFILE_VALUE_KEYS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultProfileNotExists && resultKeyNotExists && resultKeyExists;
        }

        #endregion

        #region pen profile delete value test

        private bool PenProfileDeleteValueParamNullTest()
        {
            bool resultPassNull = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(PROFILE_NAME, null, PROFILE_VALUE_KEYS);
                }
                catch (ArgumentNullException)
                {
                    resultPassNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultNameNull = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(null, PROFILE_PASS, PROFILE_VALUE_KEYS);
                }
                catch (ArgumentNullException)
                {
                    resultNameNull = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultPassNull && resultNameNull;
        }

        private bool PenProfileDeleteValueParamLongTest()
        {
            bool resultNameLong = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(PROFILE_NAME_LONG, PROFILE_PASS, PROFILE_VALUE_KEYS);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultNameLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            bool resultPassLong = false;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(PROFILE_NAME, PROFILE_PASS_LONG, PROFILE_VALUE_KEYS);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resultPassLong = true;
                    _autoResetEvent.Set();
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return resultNameLong && resultPassLong;
        }

        private bool PenProfileDeleteValueProfileNotExistsTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileDeleteValueEventArgs arg = args as PenProfileDeleteValueEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success)
                {
                    foreach (var v in arg.Data)
                    {
                        if (v.Status == PenProfile.PROFILE_STATUS_NO_EXIST_PROFILE)
                        {
                            result = true;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(PROFILE_NAME_INVALID, PROFILE_PASS, PROFILE_VALUE_KEYS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileDeleteValueInvalidPasswordTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileDeleteValueEventArgs arg = args as PenProfileDeleteValueEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success)
                {
                    foreach (var v in arg.Data)
                    {
                        if (v.Status == PenProfile.PROFILE_STATUS_NO_PERMISSION)
                        {
                            result = true;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(PROFILE_NAME, PROFILE_PASS_INVALID, PROFILE_VALUE_KEYS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileDeleteValueSuccessTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileDeleteValueEventArgs arg = args as PenProfileDeleteValueEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success)
                {
                    result = true;

                    foreach (var v in arg.Data)
                    {
                        if (v.Status != PenProfile.PROFILE_STATUS_SUCCESS)
                        {
                            result = false;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(PROFILE_NAME, PROFILE_PASS, PROFILE_VALUE_KEYS);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        private bool PenProfileDeleteValueNotExistsTest()
        {
            bool result = false;

            TypedEventHandler<IPenClient, PenProfileReceivedEventArgs> penProfileReceived;

            penProfileReceived = new TypedEventHandler<IPenClient, PenProfileReceivedEventArgs>((IPenClient sender, PenProfileReceivedEventArgs args) =>
            {
                PenProfileDeleteValueEventArgs arg = args as PenProfileDeleteValueEventArgs;

                if (arg.Result == PenProfileReceivedEventArgs.ResultType.Success)
                {
                    foreach (var v in arg.Data)
                    {
                        if (v.Status == PenProfile.PROFILE_STATUS_NO_EXIST_KEY)
                        {
                            result = true;
                            break;
                        }
                    }
                }

                _autoResetEvent.Set();
            });

            _controller.PenProfileReceived += penProfileReceived;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _controller.DeleteProfileValues(PROFILE_NAME, PROFILE_PASS, PROFILE_VALUE_KEYS_INVALID);
                }
                catch
                {
                    _autoResetEvent.Set();
                }
            });

            _autoResetEvent.WaitOne();

            _controller.PenProfileReceived -= penProfileReceived;

            return result;
        }

        #endregion

        #endregion
    }
}
