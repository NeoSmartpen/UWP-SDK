using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neosmartpen.Net;
using Neosmartpen.Net.Bluetooth;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Neosmartpen.UnitTest
{
    [TestClass]
    public class ProtocolVersion1UnitTest
    {
        private static BluetoothPenClient _client;

        private static PenController _controller;

        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        public const int TEST_TIMEOUT = 15000;

        public const string MAC = "9c:7b:d2:02:30:0f";

        public const string PASSWORD = "1234";

        public const int DEFAULT_SECTION = 3;
        public const int DEFAULT_OWNER = 27;
        public const int DEFAULT_NOTE = 603;

        // Enter the firmware file name and version and copy the file to the C:\Users\...\Pictures folder.
        public const string FIRMWARE_FILENAME = "N2_1.07.0162._v_";
        public const string FIRMWARE_VERSION = "1.07.0162";

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

        private void _controller_AvailableNoteAdded(IPenClient sender, object args)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region fw update

        [TestMethod]
        [Ignore]
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
    }
}
