using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Neosmartpen.Net.Support;
using Windows.Storage;
using Neosmartpen.Net.Filter;

namespace Neosmartpen.Net
{
	internal class PenClientParserV2 : IPenClientParser
	{
		public class Const
		{
			public const byte PK_STX = 0xC0;
			public const byte PK_ETX = 0xC1;
			public const byte PK_DLE = 0x7D;

			public const int PK_POS_CMD = 1;
			public const int PK_POS_RESULT = 2;
			public const int PK_POS_LENG1 = 2;
			public const int PK_POS_LENG2 = 3;

			public const int PK_HEADER_SIZE = 3;
		}

		[Flags]
		public enum Cmd
		{
			VERSION_REQUEST = 0x01,
			VERSION_RESPONSE = 0x81,

			PASSWORD_REQUEST = 0x02,
			PASSWORD_RESPONSE = 0X82,

			PASSWORD_CHANGE_REQUEST = 0X03,
			PASSWORD_CHANGE_RESPONSE = 0X83,

			SETTING_INFO_REQUEST = 0X04,
			SETTING_INFO_RESPONSE = 0X84,

			LOW_BATTERY_EVENT = 0X61,
			SHUTDOWN_EVENT = 0X62,

			SETTING_CHANGE_REQUEST = 0X05,
			SETTING_CHANGE_RESPONSE = 0X85,

			ONLINE_DATA_REQUEST = 0X11,
			ONLINE_DATA_RESPONSE = 0X91,

			ONLINE_PEN_UPDOWN_EVENT = 0X63,
			ONLINE_PAPER_INFO_EVENT = 0X64,
			ONLINE_PEN_DOT_EVENT = 0X65,

			OFFLINE_NOTE_LIST_REQUEST = 0X21,
			OFFLINE_NOTE_LIST_RESPONSE = 0XA1,

			OFFLINE_PAGE_LIST_REQUEST = 0X22,
			OFFLINE_PAGE_LIST_RESPONSE = 0XA2,

			OFFLINE_DATA_REQUEST = 0X23,
			OFFLINE_DATA_RESPONSE = 0XA3,
			OFFLINE_PACKET_REQUEST = 0X24,
			OFFLINE_PACKET_RESPONSE = 0XA4,

			OFFLINE_DATA_DELETE_REQUEST = 0X25,
			OFFLINE_DATA_DELETE_RESPONSE = 0XA5,

			FIRMWARE_UPLOAD_REQUEST = 0X31,
			FIRMWARE_UPLOAD_RESPONSE = 0XB1,
			FIRMWARE_PACKET_REQUEST = 0X32,
			FIRMWARE_PACKET_RESPONSE = 0XB2
		};
		public PenClientParserV2(PenController penClient)
		{
			this.PenController = penClient;
			dotFilterForPaper = new FilterForPaper(SendDotReceiveEvent);
			offlineFilterForPaper = new FilterForPaper(AddOfflineFilteredDot);
		}

		public PenController PenController { get; private set; }

		/// <summary>
		/// Gets a name of a device.
		/// </summary>
		public string DeviceName { get; private set; }

		/// <summary>
		/// Gets a version of a firmware.
		/// </summary>
		public string FirmwareVersion { get; private set; }

		/// <summary>
		/// Gets a version of a protocol.
		/// </summary>
		public string ProtocolVersion { get; private set; }

		public string SubName { get; private set; }

		/// <summary>
		/// Gets the device identifier.
		/// </summary>
		public string MacAddress { get; private set; }

		public short DeviceType { get; private set; }

		/// <summary>
		/// Gets the maximum level of force sensor.
		/// </summary>
		public short MaxForce { get; private set; }

		private long mTime = -1L;

		private PenTipType mPenTipType = PenTipType.Normal;

		private int mPenTipColor = -1;

		public enum PenTipType { Normal = 0, Eraser = 1 };

		private bool IsStartWithDown = false;

		private int mDotCount = -1;

		private int mCurSection = -1, mCurOwner = -1, mCurNote = -1, mCurPage = -1;

		//private Packet mPrevPacket = null;

		private int mTotalOfflineStroke = -1, mReceivedOfflineStroke = 0, mTotalOfflineDataSize = -1;
		private int PenMaxForce = 0;

		//private Dot previousDot = null;

		private readonly string DEFAULT_PASSWORD = "0000";
		private bool reCheckPassword = false;
		private string newPassword;

		private FilterForPaper dotFilterForPaper = null;
		private FilterForPaper offlineFilterForPaper = null;

		public bool HoverMode
		{
			get;
			private set;
		}

		public void ParsePacket(Packet packet)
		{
			Cmd cmd = (Cmd)packet.Cmd;

			//Debug.Write("Cmd : {0}", cmd.ToString());

			switch (cmd)
			{
				case Cmd.VERSION_RESPONSE:
					{
						DeviceName = packet.GetString(16);
						FirmwareVersion = packet.GetString(16);
						ProtocolVersion = packet.GetString(8);
						SubName = packet.GetString(16);
						DeviceType = packet.GetShort();
						MaxForce = -1;
						MacAddress = BitConverter.ToString(packet.GetBytes(6)).Replace("-", "");

						IsUploading = false;

						ReqPenStatus();
					}
					break;

				#region event
				case Cmd.SHUTDOWN_EVENT:
					{
						byte reason = packet.GetByte();
						Debug.Write(" => SHUTDOWN_EVENT : {0}", reason.ToString());
					}
					break;

				case Cmd.LOW_BATTERY_EVENT:
					{
						int battery = (int)(packet.GetByte() & 0xff);

						PenController.onReceiveBatteryAlarm(new BatteryAlarmReceivedEventArgs(battery));
					}
					break;

				case Cmd.ONLINE_PEN_UPDOWN_EVENT:
				case Cmd.ONLINE_PEN_DOT_EVENT:
				case Cmd.ONLINE_PAPER_INFO_EVENT:
					{
						ParseDotPacket(cmd, packet);
					}
					break;
				#endregion

				#region setting response
				case Cmd.SETTING_INFO_RESPONSE:
					{
						// 비밀번호 사용 여부
						bool lockyn = packet.GetByteToInt() == 1;

						// 비밀번호 입력 최대 시도 횟수
						int pwdMaxRetryCount = packet.GetByteToInt();

						// 비밀번호 입력 시도 횟수
						int pwdRetryCount = packet.GetByteToInt();

						// 1970년 1월 1일부터 millisecond tick
						long time = packet.GetLong();

						// 사용하지 않을때 자동으로 전원이 종료되는 시간 (단위:분)
						short autoPowerOffTime = packet.GetShort();

						// 최대 필압
						short maxForce = packet.GetShort();

						// 현재 메모리 사용량
						int usedStorage = packet.GetByteToInt();

						// 펜의 뚜껑을 닫아서 펜의 전원을 차단하는 기능 사용 여부
						bool penCapOff = packet.GetByteToInt() == 1;

						// 전원이 꺼진 펜에 필기를 시작하면 자동으로 펜의 켜지는 옵션 사용 여부
						bool autoPowerON = packet.GetByteToInt() == 1;

						// 사운드 사용여부
						bool beep = packet.GetByteToInt() == 1;

						// 호버기능 사용여부
						bool hover = packet.GetByteToInt() == 1;

						// 남은 배터리 수치
						int batteryLeft = packet.GetByteToInt();

						// 오프라인 데이터 저장 기능 사용 여부
						bool useOffline = packet.GetByteToInt() == 1;

						// 필압 단계 설정 (0~4) 0이 가장 민감
						short fsrStep = (short)packet.GetByteToInt();

						// 최초 연결시
						if (MaxForce == -1)
						{
							MaxForce = maxForce;

							var connectedEventArgs = new ConnectedEventArgs();

							PenController.onConnected(new ConnectedEventArgs(MacAddress, DeviceName, FirmwareVersion, ProtocolVersion, SubName, MaxForce));
							PenMaxForce = MaxForce;

							if (lockyn)
							{
								PenController.onPenPasswordRequest(new PasswordRequestedEventArgs(pwdRetryCount, pwdMaxRetryCount));
							}
							else
							{
								ReqSetupTime(Time.GetUtcTimeStamp());
								PenController.onPenAuthenticated();
							}
						}
						else
						{
							PenController.onReceivePenStatus(new PenStatusReceivedEventArgs(lockyn, pwdMaxRetryCount, pwdRetryCount, time, autoPowerOffTime, MaxForce, batteryLeft, usedStorage, useOffline, autoPowerON, penCapOff, hover, beep, fsrStep));
						}
					}
					break;

				case Cmd.SETTING_CHANGE_RESPONSE:
					{
						int inttype = packet.GetByteToInt();

						SettingType stype = (SettingType)inttype;

						bool result = packet.Result == 0x00;

						switch (stype)
						{
							case SettingType.AutoPowerOffTime:
								PenController.onPenAutoShutdownTimeSetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.AutoPowerOn:
								PenController.onPenAutoPowerOnSetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.Beep:
								PenController.onPenBeepSetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.Hover:
								PenController.onPenHoverSetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.LedColor:
								PenController.onPenColorSetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.OfflineData:
								PenController.onPenOfflineDataSetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.PenCapOff:
								PenController.onPenCapPowerOnOffSetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.Sensitivity:
								PenController.onPenSensitivitySetupResponse(new SimpleResultEventArgs(result));
								break;

							case SettingType.Timestamp:
								PenController.onPenTimestampSetupResponse(new SimpleResultEventArgs(result));
								break;
						}
					}
					break;
				#endregion

				#region password response
				case Cmd.PASSWORD_RESPONSE:
					{
						int status = packet.GetByteToInt();
						int cntRetry = packet.GetByteToInt();
						int cntMax = packet.GetByteToInt();

						if (status == 1)
						{
							if (reCheckPassword)
							{
								PenController.onPenPasswordSetupResponse(new SimpleResultEventArgs(true));
								reCheckPassword = false;
								break;
							}

							ReqSetupTime(Time.GetUtcTimeStamp());
							PenController.onPenAuthenticated();
						}
						else
						{
							if (reCheckPassword)
							{
								reCheckPassword = false;
								PenController.onPenPasswordSetupResponse(new SimpleResultEventArgs(false));
							}
							else
							{
								PenController.onPenPasswordRequest(new PasswordRequestedEventArgs(cntRetry, cntMax));
							}
						}
					}
					break;

				case Cmd.PASSWORD_CHANGE_RESPONSE:
					{
						int cntRetry = packet.GetByteToInt();
						int cntMax = packet.GetByteToInt();

						if (packet.Result == 0x00)
						{
							reCheckPassword = true;
							ReqInputPassword(newPassword);
						}
						else
						{
							newPassword = string.Empty;
							PenController.onPenPasswordSetupResponse(new SimpleResultEventArgs(false));
						}
					}
					break;
				#endregion

				#region offline response
				case Cmd.OFFLINE_NOTE_LIST_RESPONSE:
					{
						short length = packet.GetShort();

						List<OfflineDataInfo> result = new List<OfflineDataInfo>();

						for (int i = 0; i < length; i++)
						{
							byte[] rb = packet.GetBytes(4);

							int section = (int)(rb[3] & 0xFF);
							int owner = ByteConverter.ByteToInt(new byte[] { rb[0], rb[1], rb[2], (byte)0x00 });
							int note = packet.GetInt();

							result.Add(new OfflineDataInfo(section, owner, note));
						}

						PenController.onReceiveOfflineDataList(new OfflineDataListReceivedEventArgs(result.ToArray()));
					}
					break;

				case Cmd.OFFLINE_PAGE_LIST_RESPONSE:
					{
						byte[] rb = packet.GetBytes(4);

						int section = (int)(rb[3] & 0xFF);
						int owner = ByteConverter.ByteToInt(new byte[] { rb[0], rb[1], rb[2], (byte)0x00 });
						int note = packet.GetInt();

						short length = packet.GetShort();

						int[] pages = new int[length];

						for (int i = 0; i < length; i++)
						{
							pages[i] = packet.GetInt();
						}

						OfflineDataInfo info = new OfflineDataInfo(section, owner, note);

						PenController.onReceiveOfflineDataList(new OfflineDataListReceivedEventArgs(info));
					}
					break;

				case Cmd.OFFLINE_DATA_RESPONSE:
					{
						mTotalOfflineStroke = packet.GetInt();
						mReceivedOfflineStroke = 0;
						mTotalOfflineDataSize = packet.GetInt();

						bool isCompressed = packet.GetByte() == 1;

						PenController.onStartOfflineDownload();
					}
					break;

				case Cmd.OFFLINE_PACKET_REQUEST:
					{
						#region offline data parsing

						List<Stroke> result = new List<Stroke>();

						short packetId = packet.GetShort();

						bool isCompressed = packet.GetByte() == 1;

						short sizeBefore = packet.GetShort();

						short sizeAfter = packet.GetShort();

						short location = (short)(packet.GetByte() & 0xFF);

						byte[] rb = packet.GetBytes(4);

						int section = (int)(rb[3] & 0xFF);

						int owner = ByteConverter.ByteToInt(new byte[] { rb[0], rb[1], rb[2], (byte)0x00 });

						int note = packet.GetInt();

						short strCount = packet.GetShort();

						mReceivedOfflineStroke += strCount;

						Debug.WriteLine(" packetId : {0}, isCompressed : {1}, sizeBefore : {2}, sizeAfter : {3}, size : {4}", packetId, isCompressed, sizeBefore, sizeAfter, packet.Data.Length - 18);

						if (sizeAfter != (packet.Data.Length - 18))
						{
							SendOfflinePacketResponse(packetId, false);
							return;
						}

						byte[] oData = packet.GetBytes(sizeAfter);

						GZipStream gzipStream = new GZipStream(new System.IO.MemoryStream(oData), CompressionLevel.Fastest);

						byte[] strData = Ionic.Zlib.ZlibStream.UncompressBuffer(oData);

						if (strData.Length != sizeBefore)
						{
							SendOfflinePacketResponse(packetId, false);
							return;
						}

						ByteUtil butil = new ByteUtil(strData);

						for (int i = 0; i < strCount; i++)
						{
							int pageId = butil.GetInt();

							long timeStart = butil.GetLong();

							long timeEnd = butil.GetLong();

							int penTipType = (int)(butil.GetByte() & 0xFF);

							int color = butil.GetInt();

							short dotCount = butil.GetShort();

							long time = timeStart;

							//System.Console.WriteLine( "pageId : {0}, timeStart : {1}, timeEnd : {2}, penTipType : {3}, color : {4}, dotCount : {5}, time : {6},", pageId, timeStart, timeEnd, penTipType, color, dotCount, time );

							offlineStroke = new Stroke(section, owner, note, pageId);

							for (int j = 0; j < dotCount; j++)
							{
								byte dotChecksum = butil.GetChecksum(15);

								int timeadd = butil.GetByte();

								time += timeadd;

								int force = butil.GetShort();

								int x = butil.GetShort();
								int y = butil.GetShort();

								int fx = butil.GetByte();
								int fy = butil.GetByte();

								int tx = butil.GetByte();
								int ty = butil.GetByte();

								int twist = butil.GetShort();

								short reserved = butil.GetShort();

								byte checksum = butil.GetByte();

								//System.Console.WriteLine( "x : {0}, y : {1}, force : {2}, checksum : {3}, dotChecksum : {4}", tx, ty, twist, checksum, dotChecksum );

								if (dotChecksum != checksum)
								{
									SendOfflinePacketResponse(packetId, false);
									result.Clear();
									return;
								}

								DotTypes dotType;

								if (j == 0)
								{
									dotType = DotTypes.PEN_DOWN;
								}
								else if (j == dotCount - 1)
								{
									dotType = DotTypes.PEN_UP;
								}
								else
								{
									dotType = DotTypes.PEN_MOVE;
								}

								offlineFilterForPaper.Put(MakeDot(PenMaxForce, owner, section, note, pageId, time, x, y, fx, fy, force, dotType, color));
								//stroke.Add(MakeDot(PenMaxForce, owner, section, note, pageId, time, x, y, fx, fy, force, dotType, color));
							}

							result.Add(offlineStroke);
						}

						SendOfflinePacketResponse(packetId);

						PenController.onReceiveOfflineStrokes(new OfflineStrokeReceivedEventArgs(mTotalOfflineStroke, mReceivedOfflineStroke, result.ToArray()));

						if (location == 2)
						{
							PenController.onFinishedOfflineDownload(new SimpleResultEventArgs(true));
						}

						#endregion
					}
					break;

				case Cmd.OFFLINE_DATA_DELETE_RESPONSE:
					{
						PenController.onRemovedOfflineData(new SimpleResultEventArgs(packet.Result == 0x00));
					}
					break;
				#endregion

				#region firmware response
				case Cmd.FIRMWARE_UPLOAD_RESPONSE:
					{
						if (packet.Result != 0 || packet.GetByteToInt() != 0)
						{
							IsUploading = false;
							PenController.onReceiveFirmwareUpdateResult(new SimpleResultEventArgs(false));
						}
					}
					break;

				case Cmd.FIRMWARE_PACKET_REQUEST:
					{
						int status = packet.GetByteToInt();
						int offset = packet.GetInt();

						ResponseChunkRequest(offset, status != 3);
					}
					break;
				#endregion

				case Cmd.ONLINE_DATA_RESPONSE:
					break;



				default:
					break;
			}
		}

        private Dot mPrevDot = null;

        private bool IsBeforeMiddle = false;

        private void ParseDotPacket(Cmd cmd, Packet pk)
		{
			switch (cmd)
			{
				case Cmd.ONLINE_PEN_UPDOWN_EVENT:

					IsStartWithDown = pk.GetByte() == 0x00;

                    IsBeforeMiddle = false;

                    mDotCount = 0;

					mTime = pk.GetLong();

					mPenTipType = pk.GetByte() == 0x00 ? PenTipType.Normal : PenTipType.Eraser;

					mPenTipColor = pk.GetInt();

					if (mPrevDot != null && !IsStartWithDown)
					{
                        //mPrevPacket.Reset();
                        //ParseDot(mPrevPacket, DotTypes.PEN_UP);
                        var udot = mPrevDot.Clone();
                        udot.DotType = DotTypes.PEN_UP;
						ProcessDot(udot);
                        //PenController.onReceiveDot(new DotReceivedEventArgs(udot));
                        mPrevDot = null;
                    }

					break;

				case Cmd.ONLINE_PEN_DOT_EVENT:

                    int timeadd = pk.GetByte();

                    mTime += timeadd;

                    int force = pk.GetShort();

                    int x = pk.GetShort();
                    int y = pk.GetShort();

                    int fx = pk.GetByte();
                    int fy = pk.GetByte();

                    Dot dot = null;

                    if (HoverMode && !IsStartWithDown)
                    {
                        dot = MakeDot(PenMaxForce, mCurOwner, mCurSection, mCurNote, mCurPage, mTime, x, y, fx, fy, force, DotTypes.PEN_HOVER, mPenTipColor);
                    }
                    else if (IsStartWithDown)
                    {
                        dot = MakeDot(PenMaxForce, mCurOwner, mCurSection, mCurNote, mCurPage, mTime, x, y, fx, fy, force, mDotCount == 0 ? DotTypes.PEN_DOWN : DotTypes.PEN_MOVE, mPenTipColor);
                    }
                    else
                    {
                        //오류
                    }

                    if (dot != null)
                    {
						ProcessDot(dot);
                        //PenController.onReceiveDot(new DotReceivedEventArgs(dot));
                    }
                    IsBeforeMiddle = true;
                    mPrevDot = dot;
                    //mPrevPacket = pk;
					mDotCount++;

					break;

				case Cmd.ONLINE_PAPER_INFO_EVENT:
                    
                    // 미들도트 중에 페이지가 바뀐다면 강제로 펜업을 만들어 준다.
                    if (IsBeforeMiddle)
                    {
                        var audot = mPrevDot.Clone();
                        audot.DotType = DotTypes.PEN_UP;
						ProcessDot(audot);
                        //PenController.onReceiveDot(new DotReceivedEventArgs(audot));
                    }

					byte[] rb = pk.GetBytes(4);

					mCurSection = (int)(rb[3] & 0xFF);
					mCurOwner = ByteConverter.ByteToInt(new byte[] { rb[0], rb[1], rb[2], (byte)0x00 });
					mCurNote = pk.GetInt();
					mCurPage = pk.GetInt();

                    mDotCount = 0;

                    break;
			}
		}

		private void ProcessDot(Dot dot)
		{
			dotFilterForPaper.Put(dot);
		}

		private void SendDotReceiveEvent(Dot dot)
		{
			PenController.onReceiveDot(new DotReceivedEventArgs(dot));
		}

		private Stroke offlineStroke;
		private void AddOfflineFilteredDot(Dot dot)
		{
			offlineStroke.Add(dot);
		}

		private void ParseDot(Packet mPack, DotTypes type)
		{
			int timeadd = mPack.GetByte();

			mTime += timeadd;

			int force = mPack.GetShort();

			int x = mPack.GetShort();
			int y = mPack.GetShort();

			int fx = mPack.GetByte();
			int fy = mPack.GetByte();

			int tx = mPack.GetByte();
			int ty = mPack.GetByte();

			int twist = mPack.GetShort();

			ProcessDot(MakeDot(PenMaxForce, mCurOwner, mCurSection, mCurNote, mCurPage, mTime, x, y, fx, fy, force, type, mPenTipColor));
			//PenController.onReceiveDot(new DotReceivedEventArgs(MakeDot(PenMaxForce, mCurOwner, mCurSection, mCurNote, mCurPage, mTime, x, y, fx, fy, force, type, mPenTipColor)));
		}

		private byte[] Escape(byte input)
		{
			if (input == Const.PK_STX || input == Const.PK_ETX || input == Const.PK_DLE)
			{
				return new byte[] { Const.PK_DLE, (byte)(input ^ 0x20) };
			}
			else
			{
				return new byte[] { input };
			}
		}

		private bool Send(ByteUtil bf)
		{
			PenController.PenClient.Write(bf.ToArray());

			bf.Clear();
			bf = null;

			return true;
		}

		public void ReqVersion()
		{
			ByteUtil bf = new ByteUtil(Escape);

			// TODO 정상적으로 넘어오는지 확인이 필요하다.
			Assembly assemObj = this.GetType().GetTypeInfo().Assembly;
			Version v = assemObj.GetName().Version; // 현재 실행되는 어셈블리..dll의 버전 가져오기

			byte[] StrByte = Encoding.UTF8.GetBytes(String.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision));

			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.VERSION_REQUEST)
			  .PutShort(34)
			  .PutNull(16)
			  .Put(0x12)
			  .Put(0x01)
			  .Put(StrByte, 16)
			  .Put(Const.PK_ETX, false);

			Send(bf);
		}

		#region password

		/// <summary>
		/// Change the password of device.
		/// </summary>
		/// <param name="oldPassword">Current password</param>
		/// <param name="newPassword">New password</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetUpPassword(string oldPassword, string newPassword = "")
		{
			if (oldPassword == null || newPassword == null)
				return false;

			if (oldPassword.Equals(DEFAULT_PASSWORD))
				return false;
			if (newPassword.Equals(DEFAULT_PASSWORD))
				return false;

			this.newPassword = newPassword;

			byte[] oPassByte = Encoding.UTF8.GetBytes(oldPassword);
			byte[] nPassByte = Encoding.UTF8.GetBytes(newPassword);

			ByteUtil bf = new ByteUtil(Escape);
			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.PASSWORD_CHANGE_REQUEST)
			  .PutShort(33)
			  .Put((byte)(newPassword == "" ? 0 : 1))
			  .Put(oPassByte, 16)
			  .Put(nPassByte, 16)
			  .Put(Const.PK_ETX, false);

			return Send(bf);
		}

		/// <summary>
		/// Input password if device is locked.
		/// </summary>
		/// <param name="password">Specifies the password for authentication. Password is a string</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqInputPassword(string password)
		{
			if (password == null)
				return false;

			if (password.Equals(DEFAULT_PASSWORD))
				return false;

			byte[] bStrByte = Encoding.UTF8.GetBytes(password);

			ByteUtil bf = new ByteUtil(Escape);
			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.PASSWORD_REQUEST)
			  .PutShort(16)
			  .Put(bStrByte, 16)
			  .Put(Const.PK_ETX, false);

			return Send(bf);
		}

		#endregion


		#region pen setup

		/// <summary>
		/// Request the status of pen.
		/// If you requested, you can receive result by PenCommV2Callbacks.onReceivedPenStatus method.
		/// </summary>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqPenStatus()
		{
			ByteUtil bf = new ByteUtil();

			bf.Put(Const.PK_STX)
				.Put((byte)Cmd.SETTING_INFO_REQUEST)
				.PutShort(0)
				.Put(Const.PK_ETX);

			return Send(bf);
		}

		public enum SettingType : byte { Timestamp = 1, AutoPowerOffTime = 2, PenCapOff = 3, AutoPowerOn = 4, Beep = 5, Hover = 6, OfflineData = 7, LedColor = 8, Sensitivity = 9 };

		private bool RequestChangeSetting(SettingType stype, object value)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put(Const.PK_STX).Put((byte)Cmd.SETTING_CHANGE_REQUEST);

			switch (stype)
			{
				case SettingType.Timestamp:
					bf.PutShort(9).Put((byte)stype).PutLong((long)value);
					break;

				case SettingType.AutoPowerOffTime:
					bf.PutShort(3).Put((byte)stype).PutShort((short)value);
					break;

				case SettingType.LedColor:
					byte[] b = BitConverter.GetBytes((int)value);
					byte[] nBytes = new byte[] { b[3], b[2], b[1], b[0] };
					bf.PutShort(5).Put((byte)stype).Put(nBytes, 4);

					//bf.PutShort(5).Put((byte)stype).PutInt((int)value);
					break;

				case SettingType.PenCapOff:
				case SettingType.AutoPowerOn:
				case SettingType.Beep:
				case SettingType.Hover:
				case SettingType.OfflineData:
					bf.PutShort(2).Put((byte)stype).Put((byte)((bool)value ? 1 : 0));
					break;
				case SettingType.Sensitivity:
					bf.PutShort(2).Put((byte)stype).Put((byte)((short)value));
					break;
			}

			bf.Put(Const.PK_ETX);

			return Send(bf);
		}

		/// <summary>
		/// Sets the RTC timestamp.
		/// </summary>
		/// <param name="timetick">milisecond timestamp tick (from 1970-01-01)</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupTime(long timetick)
		{
			return RequestChangeSetting(SettingType.Timestamp, timetick);
		}

		/// <summary>
		/// Sets the value of the auto shutdown time property that if pen stay idle, shut off the pen.
		/// </summary>
		/// <param name="minute">minute of maximum idle time, staying power on (0~)</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenAutoShutdownTime(short minute)
		{
			return RequestChangeSetting(SettingType.AutoPowerOffTime, minute);
		}

		/// <summary>
		/// Sets the status of the power control by cap on property.
		/// </summary>
		/// <param name="seton">true if you want to use, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenCapPower(bool enable)
		{
			return RequestChangeSetting(SettingType.PenCapOff, enable);
		}

		/// <summary>
		/// Sets the status of the auto power on property that if write the pen, turn on when pen is down.
		/// </summary>
		/// <param name="seton">true if you want to use, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenAutoPowerOn(bool enable)
		{
			return RequestChangeSetting(SettingType.AutoPowerOn, enable);
		}

		/// <summary>
		/// Sets the status of the beep property.
		/// </summary>
		/// <param name="enable">true if you want to listen sound of pen, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenBeep(bool enable)
		{
			return RequestChangeSetting(SettingType.Beep, enable);
		}

		/// <summary>
		/// Sets the hover mode.
		/// </summary>
		/// <param name="enable">true if you want to enable hover mode, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupHoverMode(bool enable)
		{
			return RequestChangeSetting(SettingType.Hover, enable);
		}

		/// <summary>
		/// Sets the offline data option whether save offline data or not.
		/// </summary>
		/// <param name="enable">true if you want to enable offline mode, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupOfflineData(bool enable)
		{
			return RequestChangeSetting(SettingType.OfflineData, enable);
		}

		/// <summary>
		/// Sets the color of LED.
		/// </summary>
		/// <param name="rgbcolor">integer type color formatted 0xAARRGGBB</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenColor(int color)
		{
			return RequestChangeSetting(SettingType.LedColor, color);
		}

		/// <summary>
		/// Sets the value of the pen's sensitivity property that controls the force sensor of pen.
		/// </summary>
		/// <param name="level">the value of sensitivity. (0~4, 0 means maximum sensitivity)</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenSensitivity(short step)
		{
			return RequestChangeSetting(SettingType.Sensitivity, step);
		}

		#endregion

		#region using note

		private bool SendAddUsingNote(int sectionId = -1, int ownerId = -1, int[] noteIds = null)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put(Const.PK_STX)
			  .Put((byte)Cmd.ONLINE_DATA_REQUEST);

			if (sectionId > 0 && ownerId > 0 && noteIds == null)
			{
				bf.PutShort(2 + 8)
				  .PutShort(1)
				  .Put(GetSectionOwnerByte(sectionId, ownerId))
				  .Put(0xFF).Put(0xFF).Put(0xFF).Put(0xFF);
			}
			else if (sectionId > 0 && ownerId > 0 && noteIds != null)
			{
				short length = (short)(2 + (noteIds.Length * 8));

				bf.PutShort(length)
				  .PutShort((short)noteIds.Length);

				foreach (int item in noteIds)
				{
					bf.Put(GetSectionOwnerByte(sectionId, ownerId))
					.PutInt(item);
				}
			}
			else
			{
				bf.PutShort(2)
				  .Put(0xFF)
				  .Put(0xFF);
			}

			bf.Put(Const.PK_ETX);

			return Send(bf);
		}

		/// <summary>
		/// Sets the available notebook type
		/// </summary>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqAddUsingNote()
		{
			return SendAddUsingNote();
		}

		/// <summary>
		/// Sets the available notebook type
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <param name="notes">The array of Note Id list</param>
		public bool ReqAddUsingNote(int section, int owner, int[] notes = null)
		{
			return SendAddUsingNote(section, owner, notes);
		}

		#endregion

		#region offline

		/// <summary>
		/// Requests the list of Offline data.
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqOfflineDataList(int section = -1, int owner = -1)
		{
			ByteUtil bf = new ByteUtil(Escape);

			byte[] pInfo = section > 0 && owner > 0 ? GetSectionOwnerByte(section, owner) : new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.OFFLINE_NOTE_LIST_REQUEST)
			  .PutShort(4)
			  .Put(pInfo)
			  .Put(Const.PK_ETX, false);

			return Send(bf);
		}

		/// <summary>
		/// Requests the list of Offline data.
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <param name="note">The Note Id of the paper</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqOfflineDataList(int section, int owner, int note)
		{
			ByteUtil bf = new ByteUtil(Escape);

			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.OFFLINE_PAGE_LIST_REQUEST)
			  .PutShort(8)
			  .Put(GetSectionOwnerByte(section, owner))
			  .PutInt(note)
			  .Put(Const.PK_ETX, false);

			return Send(bf);
		}

		/// <summary>
		/// Requests the transmission of data
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <param name="note">The Note Id of the paper</param>
		/// <param name="deleteOnFinished">delete offline data when transmission is finished,</param>
		/// <param name="pages">The number of page</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqOfflineData(int section, int owner, int note, bool deleteOnFinished = true, int[] pages = null)
		{
			byte[] ownerByte = ByteConverter.IntToByte(owner);

			short length = 14;

			length += (short)(pages == null ? 0 : pages.Length * 4);

			ByteUtil bf = new ByteUtil(Escape);

			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.OFFLINE_DATA_REQUEST)
			  .PutShort(length)
			  .Put((byte)(deleteOnFinished ? 1 : 2))
			  .Put((byte)1)
			  .Put(GetSectionOwnerByte(section, owner))
			  .PutInt(note)
			  .PutInt(pages == null ? 0 : pages.Length);

			if (pages != null)
			{
				foreach (int page in pages)
				{
					bf.PutInt(page);
				}
			}

			bf.Put(Const.PK_ETX, false);

			return Send(bf);
		}

		private void SendOfflinePacketResponse(short index, bool isSuccess = true)
		{
			ByteUtil bf = new ByteUtil(Escape);

			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.OFFLINE_PACKET_RESPONSE)
			  .Put((byte)(isSuccess ? 0 : 1))
			  .PutShort(3)
			  .PutShort(index)
			  .Put(1)
			  .Put(Const.PK_ETX, false);

			Send(bf);
		}

		/// <summary>
		/// Request to remove offline data in device.
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqRemoveOfflineData(int section, int owner, int[] notes)
		{
			ByteUtil bf = new ByteUtil(Escape);

			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.OFFLINE_DATA_DELETE_REQUEST);

			short length = (short)(5 + (notes.Length * 4));

			bf.PutShort(length)
			  .Put(GetSectionOwnerByte(section, owner))
			  .Put((byte)notes.Length);

			foreach (int noteId in notes)
			{
				bf.PutInt(noteId);
			}

			bf.Put(Const.PK_ETX, false);

			return Send(bf);
		}

		#endregion

		#region firmware

		private Chunk mFwChunk;

		private bool IsUploading = false;

		/// <summary>
		/// Requests the firmware installation
		/// </summary>
		/// <param name="filepath">absolute path of firmware file</param>
		/// <param name="version">version of firmware, this value is string</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public async void ReqPenSwUpgrade(StorageFile filepath, string version)
		{
			if (IsUploading)
			{
				return;
			}

			IsUploading = true;

			mFwChunk = new Chunk(1024);

			bool loaded = await mFwChunk.Load(filepath);

			if (!loaded)
			{
				return;
			}

			int file_size = mFwChunk.GetFileSize();

			short chunk_count = (short)mFwChunk.GetChunkLength();
			short chunk_size = (short)mFwChunk.GetChunksize();

			byte[] StrVersionByte = Encoding.UTF8.GetBytes(version);

			byte[] StrDeviceByte = Encoding.UTF8.GetBytes(DeviceName);

			Debug.WriteLine("[FileUploadWorker] file upload => filesize : {0}, packet count : {1}, packet size {2}", file_size, chunk_count, chunk_size);

			ByteUtil bf = new ByteUtil(Escape);

			bf.Put(Const.PK_STX, false)
			  .Put((byte)Cmd.FIRMWARE_UPLOAD_REQUEST)
			  .PutShort(42)
			  .Put(StrDeviceByte, 16)
			  .Put(StrVersionByte, 16)
			  .PutInt(file_size)
			  .PutInt(chunk_size)
			  .Put(1)
			  .Put(mFwChunk.GetTotalChecksum())
			  .Put(Const.PK_ETX, false);

			Send(bf);

			PenController.onStartFirmwareInstallation();
		}

		private void ResponseChunkRequest(int offset, bool status = true)
		{
			byte[] data = null;

			int index = (int)(offset / mFwChunk.GetChunksize());

			Debug.WriteLine("[FileUploadWorker] ResponseChunkRequest upload => index : {0}", index);

			ByteUtil bf = new ByteUtil(Escape);

			if (!status || mFwChunk == null || !IsUploading || (data = mFwChunk.Get(index)) == null)
			{
				bf.Put(Const.PK_STX, false)
				  .Put((byte)Cmd.FIRMWARE_PACKET_RESPONSE)
				  .Put(1)
				  .PutShort(0)
				  .Put(Const.PK_ETX, false);

				IsUploading = false;
			}
			else
			{
				byte[] cdata = Ionic.Zlib.ZlibStream.CompressBuffer(data);

				byte checksum = mFwChunk.GetChecksum(index);

				short dataLength = (short)(cdata.Length + 14);

				bf.Put(Const.PK_STX, false)
				  .Put((byte)Cmd.FIRMWARE_PACKET_RESPONSE)
				  .Put(0)
				  .PutShort(dataLength)
				  .Put(0)
				  .PutInt(offset)
				  .Put(checksum)
				  .PutInt(data.Length)
				  .PutInt(cdata.Length)
				  .Put(cdata)
				  .Put(Const.PK_ETX, false);
			}

			Send(bf);

			PenController.onReceiveFirmwareUpdateStatus(new ProgressChangeEventArgs(mFwChunk.GetChunkLength(), (int)index));
		}

		/// <summary>
		/// To suspend firmware installation.
		/// </summary>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool SuspendSwUpgrade()
		{
			mFwChunk = null;
			return true;
		}

		#endregion

		#region util

		private static byte[] GetSectionOwnerByte(int section, int owner)
		{
			byte[] ownerByte = ByteConverter.IntToByte(owner);
			ownerByte[3] = (byte)section;

			return ownerByte;
		}

		//public Dot( 
		private Dot MakeDot(int penMaxForce, int owner, int section, int note, int page, long timestamp, int x, int y, int fx, int fy, int force, DotTypes type, int color)
		{
			Dot.Builder builder = null;
			if (penMaxForce == 0) builder = new Dot.Builder();
			else builder = new Dot.Builder(penMaxForce);

			builder.owner(owner)
				.section(section)
				.note(note)
				.page(page)
				.timestamp(timestamp)
				.coord(x + fx * 0.01f, y + fy * 0.01f)
				.force(force)
				.dotType(type)
				.color(color);
			return builder.Build();
		}

		#endregion

		#region Protocol Parse
		private ByteUtil mBuffer = null;
		private bool IsEscape = false;
		public void ProtocolParse(byte[] buff, int size)
		{
			byte[] test = new byte[size];

			//Array.Copy( buff, 0, test, 0, size );
			//System.Console.WriteLine( "Read Buffer : {0}", BitConverter.ToString( test ) );
			//System.Console.WriteLine();

			for (int i = 0; i < size; i++)
			{
				if (buff[i] == Const.PK_STX)
				{
					// 패킷 시작
					mBuffer = new ByteUtil();

					IsEscape = false;
				}
				else if (buff[i] == Const.PK_ETX)
				{
					// 패킷 끝
					Packet.Builder builder = new Packet.Builder();

					int cmd = mBuffer.GetByteToInt();

					string cmdstr = Enum.GetName(typeof(Cmd), cmd);

					int result_size = cmdstr != null && cmdstr.EndsWith("RESPONSE") ? 1 : 0;

					int result = result_size > 0 ? mBuffer.GetByteToInt() : -1;

					int length = mBuffer.GetShort();

					byte[] data = mBuffer.GetBytes();

					//System.Console.WriteLine( "length : {0}, data : {1}", length, data.Length );

					builder.cmd(cmd)
						.result(result)
						.data(data);

					//System.Console.WriteLine( "Read Packet : {0}", BitConverter.ToString( data ) );
					//System.Console.WriteLine();

					mBuffer.Clear();
					mBuffer = null;

					ParsePacket(builder.Build());

					IsEscape = false;
				}
				else if (buff[i] == Const.PK_DLE)
				{
					if (i < size - 1)
					{
						mBuffer.Put((byte)(buff[++i] ^ 0x20));
					}
					else
					{
						IsEscape = true;
					}
				}
				else if (IsEscape)
				{
					mBuffer.Put((byte)(buff[i] ^ 0x20));

					IsEscape = false;
				}
				else
				{
					mBuffer.Put(buff[i]);
				}
			}
		}
		#endregion
	}
}
