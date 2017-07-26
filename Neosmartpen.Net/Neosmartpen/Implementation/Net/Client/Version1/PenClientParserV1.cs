using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Neosmartpen.Net.Support;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Neosmartpen.Net
{
    internal class PenClientParserV1 : IPenClientParser, OfflineWorkResponseHandler
	{
		public enum Cmd : byte
		{
			A_PenOnState = 0x01,
			P_PenOnResponse = 0x02,

			P_RTCset = 0x03,
			A_RTCsetResponse = 0x04,

			P_HoverOnOff = 0x05,
			A_HoverOnOffResponse = 0x06,

			P_ForceCalibrate = 0x07,
			A_ForceCalibrateResponse = 0x08,

			P_AutoShutdownTime = 0x09,
			A_AutoShutdownTimeResponse = 0x0A,
			P_PenSensitivity = 0x2C,
			A_PenSensitivityResponse = 0x2D,
			P_PenColorSet = 0x28,
			A_PenColorSetResponse = 0x29,
			P_AutoPowerOnSet = 0x2A,
			A_AutoPowerOnResponse = 0x2B,
			P_BeepSet = 0x2E,
			A_BeepSetResponse = 0x2F,

			P_UsingNoteNotify = 0x0B,
			A_UsingNoteNotifyResponse = 0x0C,

			A_PasswordRequest = 0x0D,
			P_PasswordResponse = 0x0E,
			P_PasswordSet = 0x0F,
			A_PasswordSetResponse = 0x10,

			A_DotData = 0x11,
			A_DotUpDownData = 0x13,
			P_DotUpDownResponse = 0x14,
			A_DotIDChange = 0x15,
			A_DotUpDownDataNew = 0x16,

			P_PenStatusRequest = 0x21,
			A_PenStatusOldResponse = 0x22,
			A_PenStatusResponse = 0x25,

			P_OfflineDataRequest = 0x47,
			A_OfflineDataInfo = 0x49,
			A_OfflineFileInfo = 0x41,
			P_OfflineFileInfoResponse = 0x42,
			A_OfflineChunk = 0x43,
			P_OfflineChunkResponse = 0x44,
			A_OfflineResultResponse = 0x48,
			P_OfflineNoteList = 0x45,
			A_OfflineNoteListResponse = 0x46,
			P_OfflineDataRemove = 0x4A,
			A_OfflineDataRemoveResponse = 0x4B,

			P_PenSWUpgradeCommand = 0x51,
			A_PenSWUpgradeRequest = 0x52,
			P_PenSWUpgradeResponse = 0x53,
			A_PenSWUpgradeStatus = 0x54
		}

		private readonly int PKT_START = 0xC0;
		private readonly int PKT_END = 0xC1;
		private readonly int PKT_EMPTY = 0x00;
		private readonly int PKT_HEADER_LEN = 3;
		private readonly int PKT_LENGTH_POS1 = 1;
		private readonly int PKT_LENGTH_POS2 = 2;
		private readonly int PKT_MAX_LEN = 8200;

		private readonly string DEFAULT_PASSWORD = "0000";

		private IPacket mPrevPacket;
		private int mOwnerId = 0, mSectionId = 0, mNoteId = 0, mPageId = 0;
		private long mPrevDotTime = 0;
		private bool IsPrevDotDown = false;
		private bool IsStartWithDown = false;
		private int mCurrentColor = 0x000000;
		private String mOfflineFileName;
		private long mOfflineFileSize;
		private short mOfflinePacketCount, mOfflinePacketSize;
		private OfflineDataSerializer mOfflineDataBuilder;
		private bool Authenticated = false;
		private int mOfflineTotalDataSize = 0, mOfflineTotalFileCount = 0, mOfflineRcvDataSize = 0;
		private List<OfflineDataInfo> mOfflineNotes = new List<OfflineDataInfo>();
		private bool IsStartOfflineTask = false;
		private Chunk mFwChunk;
		private bool IsUploading = false;
		private OfflineWorker mOfflineworker = null;
		private int PenMaxForce = 0;		// 상수로 박아도 될것같지만 혹시 모르니 connection시 받는다.

		public PenClientParserV1(PenController penClient) 
		{
			this.PenController = penClient;

			if ( mOfflineworker == null )
			{
				mOfflineworker = new OfflineWorker(this);
				mOfflineworker.Startup();
			}
		}

		public void OnDisconnected()
		{
			mOfflineworker.Reset();
		}

		public PenController PenController { get; private set; }

		public void onReceiveOfflineStrokes(int totalDataSize, int receiveDataSize, Stroke[] strokes)
		{
            PenController.onReceiveOfflineStrokes(new OfflineStrokeReceivedEventArgs(mOfflineTotalDataSize, mOfflineRcvDataSize, strokes));
		}

		public void onRequestDownloadOfflineData(int sectionId, int ownerId, int noteId)
		{
			SendReqOfflineData(sectionId, ownerId, noteId);
		}

		public void onRequestRemoveOfflineData(int sectionId, int ownerId)
		{
			ReqRemoveOfflineData(sectionId, ownerId);
		}
		private void Reset()
		{
			Debug.WriteLine("[PenCommCore] Reset");

			IsPrevDotDown = false;
			IsStartWithDown = false;

			Authenticated = false;

			IsUploading = false;

			IsStartOfflineTask = false;
		}
		public void ParsePacket(Packet packet)
		{
            //Debug.WriteLine("[PenCommCore] ParsePacket : " + packet.Cmd);

            switch ((Cmd)packet.Cmd)
			{
				case Cmd.A_DotData:
					{
						long time = packet.GetByteToInt();
						int x = packet.GetShort();
						int y = packet.GetShort();
						int fx = packet.GetByteToInt();
						int fy = packet.GetByteToInt();
						int force = packet.GetByteToInt();

						long timeLong = mPrevDotTime + time;

						if (!IsStartWithDown || timeLong < 10000)
						{
							Debug.WriteLine("[PenCommCore] this stroke start with middle dot.");
							return;
						}

						if (IsPrevDotDown)
						{
							// 펜업의 경우 시작 도트로 저장
							IsPrevDotDown = false;
							ProcessDot(mOwnerId, mSectionId, mNoteId, mPageId, timeLong, x, y, fx, fy, force, DotTypes.PEN_DOWN, mCurrentColor);
						}
						else
						{
							// 펜업이 아닌 경우 미들 도트로 저장
							ProcessDot(mOwnerId, mSectionId, mNoteId, mPageId, timeLong, x, y, fx, fy, force, DotTypes.PEN_MOVE, mCurrentColor);
						}

						mPrevDotTime = timeLong;
						mPrevPacket = packet;
					}
					break;

				case Cmd.A_DotUpDownDataNew:
				case Cmd.A_DotUpDownData:
					{
						// TODO Check
						long updownTime = packet.GetLong();

						int updown = packet.GetByteToInt();

						byte[] cbyte = packet.GetBytes(3);

						mCurrentColor = ByteConverter.ByteToInt(new byte[] { cbyte[2], cbyte[1], cbyte[0], (byte)0 });

						if (updown == 0x00)
						{
							// 펜 다운 일 경우 Start Dot의 timestamp 설정
							mPrevDotTime = updownTime;
							IsPrevDotDown = true;
							IsStartWithDown = true;

							//Callback.onUpDown(this, false);
						}
						else if (updown == 0x01)
						{
							if (mPrevPacket != null)
							{
								mPrevPacket.Reset();

								// 펜 업 일 경우 바로 이전 도트를 End Dot로 삽입
								int time = mPrevPacket.GetByteToInt();
								int x = mPrevPacket.GetShort();
								int y = mPrevPacket.GetShort();
								int fx = mPrevPacket.GetByteToInt();
								int fy = mPrevPacket.GetByteToInt();
								int force = mPrevPacket.GetByteToInt();

								ProcessDot(mOwnerId, mSectionId, mNoteId, mPageId, updownTime, x, y, fx, fy, force, DotTypes.PEN_UP, mCurrentColor);
							}

							//Callback.onUpDown(this, true);

							IsStartWithDown = false;
						}

						mPrevPacket = null;
					}
					break;

				case Cmd.A_DotIDChange:

					byte[] rb = packet.GetBytes(4);

					mSectionId = (int)(rb[3] & 0xFF);
					mOwnerId = ByteConverter.ByteToInt(new byte[] { rb[0], rb[1], rb[2], (byte)0x00 });
					mNoteId = packet.GetInt();
					mPageId = packet.GetInt();

					break;

				case Cmd.A_PenOnState:

					packet.Move(8);

					int STATUS = packet.GetByteToInt();

					int FORCE_MAX = packet.GetByteToInt();

					string SW_VER = packet.GetString(5);

					if (STATUS == 0x00)
					{
						SendPenOnOffData();
					}
					else if (STATUS == 0x01)
					{
						Reset();

						SendPenOnOffData();
						SendRTCData();

                        PenController.onConnected(new ConnectedEventArgs(SW_VER, FORCE_MAX));
						PenMaxForce = FORCE_MAX;
						mOfflineworker.PenMaxForce = FORCE_MAX;
					}

					break;

				case Cmd.A_RTCsetResponse:
					break;

				case Cmd.A_PenStatusResponse:

					if (!Authenticated)
					{
						Authenticated = true;
                        PenController.onPenAuthenticated();
					}

					packet.Move(2);

					int stat_timezone = packet.GetInt();
					long stat_timetick = packet.GetLong();
					int stat_forcemax = packet.GetByteToInt();
					int stat_battery = packet.GetByteToInt();
					int stat_usedmem = packet.GetByteToInt();
					int stat_pencolor = packet.GetInt();

					bool stat_autopower = packet.GetByteToInt() == 2 ? false : true;
					bool stat_accel = packet.GetByteToInt() == 2 ? false : true;
					bool stat_hovermode = packet.GetByteToInt() == 2 ? false : true;
					bool stat_beep = packet.GetByteToInt() == 2 ? false : true;

					short stat_autoshutdowntime = packet.GetShort();
					short stat_pensensitivity = packet.GetShort();

                    PenController.onReceivePenStatus(new PenStatusReceivedEventArgs(stat_timezone, stat_timetick, stat_forcemax, stat_battery, stat_usedmem, stat_pencolor, stat_autopower, stat_accel, stat_hovermode, stat_beep, stat_autoshutdowntime, stat_pensensitivity));
					break;

				// 오프라인 데이터 크기,갯수 전송
				case Cmd.A_OfflineDataInfo:

					mOfflineTotalFileCount = packet.GetInt();
					mOfflineTotalDataSize = packet.GetInt();

					Debug.WriteLine("[PenCommCore] A_OfflineDataInfo : {0}, {1}", mOfflineTotalFileCount, mOfflineTotalDataSize);

                    PenController.onStartOfflineDownload();

					IsStartOfflineTask = true;

					break;

				// 오프라인 전송 최종 결과 응답
				case Cmd.A_OfflineResultResponse:

					int result = packet.GetByteToInt();

					//System.Console.WriteLine( "[PenCommCore] A_OfflineDataResponse : {0}", result );

					IsStartOfflineTask = false;

                    PenController.onFinishedOfflineDownload(new SimpleResultEventArgs(result == 0x00));

					mOfflineworker.onFinishDownload();

					break;

				// 오프라인 파일 정보
				case Cmd.A_OfflineFileInfo:

					mOfflineFileName = packet.GetString(128);
					mOfflineFileSize = packet.GetInt();
					mOfflinePacketCount = packet.GetShort();
					mOfflinePacketSize = packet.GetShort();

					Debug.WriteLine("[PenCommCore] offline file transfer is started ( name : " + mOfflineFileName + ", size : " + mOfflineFileSize + ", packet_qty : " + mOfflinePacketCount + ", packet_size : " + mOfflinePacketSize + " )");

					mOfflineDataBuilder = null;
					mOfflineDataBuilder = new OfflineDataSerializer(mOfflineFileName, mOfflinePacketCount, mOfflineFileName.Contains(".zip") ? true : false);

					SendOfflineInfoResponse();

					break;

				// 오프라인 파일 조각 전송
				case Cmd.A_OfflineChunk:

					int index = packet.GetShort();

					// 체크섬 필드
					byte cs = packet.GetByte();

					// 체크섬 계산
					byte calcChecksum = packet.GetChecksum();

					// 오프라인 데이터
					byte[] data = packet.GetBytes();

					// 체크섬이 틀리거나, 카운트, 사이즈 정보가 맞지 않으면 버린다.
					if (cs == calcChecksum && mOfflinePacketCount > index && mOfflinePacketSize >= data.Length)
					{
						mOfflineDataBuilder.Put(data, index);

						// 만약 Chunk를 다 받았다면 offline data를 처리한다.
						if (mOfflinePacketCount == mOfflineDataBuilder.chunks.Count)
						{
							string output = mOfflineDataBuilder.MakeFile();

							if (output != null)
							{
								SendOfflineChunkResponse((short)index);
								mOfflineworker.onCreateFile(mOfflineDataBuilder.sectionId, mOfflineDataBuilder.ownerId, mOfflineDataBuilder.noteId, output, mOfflineTotalDataSize, mOfflineRcvDataSize);
							}

							mOfflineDataBuilder = null;
						}
						else
						{
							SendOfflineChunkResponse((short)index);
						}

						mOfflineRcvDataSize += data.Length;

						// TODO Check
						//if (mOfflineTotalDataSize > 0)
						//{
						//	Debug.WriteLine("[PenCommCore] mOfflineRcvDataSize : " + mOfflineRcvDataSize);

						//	//Callback.onUpdateOfflineDownload(this, mOfflineTotalDataSize, mOfflineRcvDataSize);
						//}
					}
					else
					{
						Debug.WriteLine("[PenCommCore] offline data file verification failed ( index : " + index + " )");
					}

					break;

				case Cmd.A_UsingNoteNotifyResponse:
                    PenController.onAvailableNoteAdded();
                    break;

				case Cmd.A_OfflineNoteListResponse:
					{
						int status = packet.GetByteToInt();

						byte[] rxb = packet.GetBytes(4);

						int section = (int)(rxb[3] & 0xFF);

						int owner = ByteConverter.ByteToInt(new byte[] { rxb[0], rxb[1], rxb[2], (byte)0x00 });

						int noteCnt = packet.GetByteToInt();

						for (int i = 0; i < noteCnt; i++)
						{
							int note = packet.GetInt();
							mOfflineNotes.Add(new OfflineDataInfo(section, owner, note));
						}

						if (status == 0x01)
						{
							OfflineDataInfo[] array = mOfflineNotes.ToArray();

							PenController.onReceiveOfflineDataList(new OfflineDataListReceivedEventArgs(array));
							mOfflineNotes.Clear();
						}
						else
						{
							PenController.onReceiveOfflineDataList(new OfflineDataListReceivedEventArgs(new OfflineDataInfo[0]));
						}
					}
					break;

				case Cmd.A_OfflineDataRemoveResponse:
					//System.Console.WriteLine( "[PenCommCore] CMD.A_OfflineDataRemoveResponse" );
					break;

				case Cmd.A_PasswordRequest:
					{
						int countRetry = packet.GetByteToInt();
						int countReset = packet.GetByteToInt();

						Debug.WriteLine("[PenCommCore] A_PasswordRequest ( " + countRetry + " / " + countReset + " )");

						if (countRetry == 0)
							_ReqInputPassword(DEFAULT_PASSWORD);
						else if (countRetry > 0)
							PenController.onPenPasswordRequest(new PasswordRequestedEventArgs(countRetry-1, countReset-1));
					}
					break;


				case Cmd.A_PasswordSetResponse:
					{
						int setResult = packet.GetByteToInt();

						//System.Console.WriteLine( "[PenCommCore] A_PasswordSetResponse => " + setResult );

						PenController.onPenPasswordSetupResponse(new SimpleResultEventArgs(setResult == 0x00));
					}
					break;

				case Cmd.A_PenSensitivityResponse:
				case Cmd.A_AutoShutdownTimeResponse:
				case Cmd.A_AutoPowerOnResponse:
				case Cmd.A_BeepSetResponse:
				case Cmd.A_PenColorSetResponse:
					ResPenSetup((Cmd)packet.Cmd, packet.GetByteToInt() == 0x01);
					break;

				case Cmd.A_PenSWUpgradeRequest:

					short idx = packet.GetShort();

                    Debug.WriteLine("[PenCommCore] A_PenSWUpgradeRequest => " + idx);

                    ResponseChunkRequest(idx);

					break;

				case Cmd.A_PenSWUpgradeStatus:
					{
						int upgStatus = packet.GetByteToInt();

						if (upgStatus == 0x02)
						{
							return;
						}

						PenController.onReceiveFirmwareUpdateResult(new SimpleResultEventArgs(upgStatus == 0x01));
						mFwChunk = null;
					}
					break;
			}
		}


		private void ResPenSetup(Cmd cmd, bool result)
		{
			switch (cmd)
			{
				case Cmd.A_PenSensitivityResponse:
					PenController.onPenSensitivitySetupResponse(new SimpleResultEventArgs(result));
					break;

				case Cmd.A_AutoShutdownTimeResponse:
					PenController.onPenAutoShutdownTimeSetupResponse(new SimpleResultEventArgs(result));
					break;

				case Cmd.A_AutoPowerOnResponse:
					PenController.onPenAutoPowerOnSetupResponse(new SimpleResultEventArgs(result));
					break;

				case Cmd.A_BeepSetResponse:
					PenController.onPenBeepSetupResponse(new SimpleResultEventArgs(result));
					break;

				case Cmd.A_PenColorSetResponse:
					PenController.onPenColorSetupResponse(new SimpleResultEventArgs(result));
					break;
			}
		}

		private void ProcessDot(int ownerId, int sectionId, int noteId, int pageId, long timeLong, int x, int y, int fx, int fy, int force, DotTypes type, int color)
		{
			Dot.Builder builder = null; new Dot.Builder();
			if (PenMaxForce == 0)
				builder = new Dot.Builder();
			else builder = new Dot.Builder(PenMaxForce);

			builder.owner(ownerId)
				.section(sectionId)
				.note(noteId)
				.page(pageId)
				.timestamp(timeLong)
				.coord(x + fx * 0.01f, y + fy * 0.01f)
				.force(force)
				.dotType(type)
				.color(color);

			PenController.onReceiveDot(new DotReceivedEventArgs(builder.Build()));
		}

		private void SendPenOnOffData()
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PenOnResponse)
			  .PutShort(9)
			  .PutLong(Time.GetUtcTimeStamp())
			  .Put((byte)0x00)
			  .Put((byte)0xC1);

			PenController.PenClient.Write(bf.ToArray());

			bf = null;
		}

		private void SendRTCData()
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_RTCset)
			  .PutShort(12)
			  .PutLong(Time.GetUtcTimeStamp())
			  .PutInt(Time.GetLocalTimeOffset())
			  .Put((byte)0xC1);

			PenController.PenClient.Write(bf.ToArray());

			bf = null;
		}

		private void SendOfflineInfoResponse()
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_OfflineFileInfoResponse)
			  .PutShort(2)
			  .PutShort(1)
			  .Put((byte)0xC1);

			PenController.PenClient.Write(bf.ToArray());

			bf = null;
		}

		private void SendOfflineChunkResponse(short index)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_OfflineChunkResponse)
			  .PutShort(2)
			  .PutShort(index)
			  .Put((byte)0xC1);

			PenController.PenClient.Write(bf.ToArray());

			bf = null;
		}

		/// <summary>
		/// Sets the available paper type
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <param name="note">The Note Id of the paper</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqAddUsingNote(int section, int owner, int note)
		{
			List<int> alnoteIds = new List<int>();
			alnoteIds.Add(note);

			return SendAddUsingNote(section, owner, alnoteIds);
		}

		/// <summary>
		/// Sets the available notebook type
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqAddUsingNote(int section, int owner)
		{
			byte[] ownerByte = ByteConverter.IntToByte(owner);

			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_UsingNoteNotify)
			  .PutShort(6)
			  .Put((byte)2)
			  .Put((byte)1)
			  .Put(ownerByte[0])
			  .Put(ownerByte[1])
			  .Put(ownerByte[2])
			  .Put((byte)section)
			  .Put((byte)0xC1);

			PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
		}

		/// <summary>
		/// Sets the available notebook type
		/// </summary>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqAddUsingNote()
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_UsingNoteNotify)
			  .PutShort(2)
			  .Put((byte)3)
			  .Put((byte)0)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Sets the available notebook type
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <param name="notes">The array of Note Id list</param>
		public void ReqAddUsingNote(int section, int owner, int[] notes)
		{
			List<int> alnoteIds = new List<int>();

			for (int i = 0; i < notes.Length; i++)
			{
				alnoteIds.Add(notes[i]);

				if (i > 0 && i % 8 == 0)
				{
					SendAddUsingNote(section, owner, alnoteIds);
					alnoteIds.Clear();
				}
			}

			if (alnoteIds.Count > 0)
			{
				SendAddUsingNote(section, owner, alnoteIds);
				alnoteIds.Clear();
			}
		}

		private bool SendAddUsingNote(int sectionId, int ownerId, List<int> noteIds)
		{
			byte[] ownerByte = ByteConverter.IntToByte(ownerId);

			short length = (short)(6 + (noteIds.Count * 4));

			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_UsingNoteNotify)
			  .PutShort(length)
			  .Put((byte)1)
			  .Put((byte)noteIds.Count)
			  .Put(ownerByte[0])
			  .Put(ownerByte[1])
			  .Put(ownerByte[2])
			  .Put((byte)sectionId);

			foreach (int item in noteIds)
			{
				bf.PutInt(item);
			}

			bf.Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Requests the list of Offline data.
		/// </summary>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqOfflineDataList()
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_OfflineNoteList)
			  .PutShort(1)
			  .Put((byte)0x00)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Requests the transmission of data
		/// </summary>
		/// <param name="note">A OfflineDataInfo that specifies the information for the offline data.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqOfflineData(OfflineDataInfo note)
		{
			mOfflineworker.Put(note);

			return true;
		}

		/// <summary>
		/// Requests the transmission of data
		/// </summary>
		/// <param name="notes">A OfflineDataInfo that specifies the information for the offline data.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqOfflineData(OfflineDataInfo[] notes)
		{
			mOfflineworker.Put(notes);

			return true;
		}

		private bool SendReqOfflineData(int sectionId, int ownerId, int noteId)
		{
			byte[] ownerByte = ByteConverter.IntToByte(ownerId);

			short length = (short)(5 + 4);

			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_OfflineDataRequest)
			  .PutShort(length)
			  .Put(ownerByte[0])
			  .Put(ownerByte[1])
			  .Put(ownerByte[2])
			  .Put((byte)sectionId)
			  .Put((byte)1)
			  .PutInt(noteId)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Request to remove offline data in device.
		/// </summary>
		/// <param name="section">The Section Id of the paper</param>
		/// <param name="owner">The Owner Id of the paper</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqRemoveOfflineData(int section, int owner)
		{
			byte[] ownerByte = ByteConverter.IntToByte(owner);

			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_OfflineDataRemove)
			  .PutShort(12)
			  .Put(ownerByte[0])
			  .Put(ownerByte[1])
			  .Put(ownerByte[2])
			  .Put((byte)section)
			  .Put((byte)0x00)
			  .Put((byte)0x00)
			  .Put((byte)0x00)
			  .Put((byte)0x00)
			  .Put((byte)0x00)
			  .Put((byte)0x00)
			  .Put((byte)0x00)
			  .Put((byte)0x00)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Request the status of pen.
		/// If you requested, you can receive result by PenCommV1Callbacks.onReceivedPenStatus method.
		/// </summary>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqPenStatus()
		{
			ByteUtil bf = new ByteUtil();
			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PenStatusRequest)
			  .PutShort(0)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
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

			ByteUtil bf = new ByteUtil();
			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PasswordResponse)
			  .PutShort(16)
			  .Put(bStrByte, 16)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		public bool _ReqInputPassword(string password)
		{
			if (password == null)
				return false;

			byte[] bStrByte = Encoding.UTF8.GetBytes(password);

			ByteUtil bf = new ByteUtil();
			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PasswordResponse)
			  .PutShort(16)
			  .Put(bStrByte, 16)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Change the password of device.
		/// </summary>
		/// <param name="oldPassword">Current password</param>
		/// <param name="newPassword">New password</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetUpPassword(string oldPassword, string newPassword)
		{
			if (oldPassword == null || newPassword == null)
				return false;

			if (oldPassword.Equals(DEFAULT_PASSWORD))
				return false;
			if (newPassword.Equals(DEFAULT_PASSWORD))
				return false;

			if (oldPassword.Equals(string.Empty))
				oldPassword = DEFAULT_PASSWORD;
			if (newPassword.Equals(string.Empty))
				newPassword = DEFAULT_PASSWORD;


			byte[] oPassByte = Encoding.UTF8.GetBytes(oldPassword);
			byte[] nPassByte = Encoding.UTF8.GetBytes(newPassword);

			ByteUtil bf = new ByteUtil();
			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PasswordSet)
			  .PutShort(32)
			  .Put(oPassByte, 16)
			  .Put(nPassByte, 16)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Sets the value of the pen's sensitivity property that controls the force sensor of pen.
		/// </summary>
		/// <param name="level">the value of sensitivity. (0~4, 0 means maximum sensitivity)</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenSensitivity(short level)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PenSensitivity)
			  .PutShort(2)
			  .PutShort(level)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Sets the value of the auto shutdown time property that if pen stay idle, shut off the pen.
		/// </summary>
		/// <param name="minute">minute of maximum idle time, staying power on (0~)</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenAutoShutdownTime(short minute)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_AutoShutdownTime)
			  .PutShort(2)
			  .PutShort(minute)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Sets the status of the beep property.
		/// </summary>
		/// <param name="enable">true if you want to listen sound of pen, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenBeep(bool enable)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_BeepSet)
			  .PutShort(1)
			  .Put((byte)(enable ? 1 : 0))
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Sets the status of the auto power on property that if write the pen, turn on when pen is down.
		/// </summary>
		/// <param name="seton">true if you want to use, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenAutoPowerOn(bool enable)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_AutoPowerOnSet)
			  .PutShort(1)
			  .Put((byte)(enable ? 1 : 0))
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Sets the color of pen ink.
		/// If you want to change led color of pen, you should choose one among next preset values.
		/// 
		/// violet = 0x9C3FCD
		/// blue = 0x3c6bf0
		/// gray = 0xbdbdbd
		/// yellow = 0xfbcb26
		/// pink = 0xff2084
		/// mint = 0x27e0c8
		/// red = 0xf93610
		/// black = 0x000000
		/// </summary>
		/// <param name="rgbcolor">integer type color formatted 0xRRGGBB</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupPenColor(int rgbcolor)
		{
			byte[] cbyte = ByteConverter.IntToByte(rgbcolor);

			byte[] nbyte = new byte[] { cbyte[0], cbyte[1], cbyte[2], (byte)0x01 };

			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PenColorSet)
			  .PutShort(4)
			  .Put(nbyte, 4)
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

		/// <summary>
		/// Sets the hover mode.
		/// </summary>
		/// <param name="enable">true if you want to enable hover mode, otherwise false.</param>
		/// <returns>true if the request is accepted; otherwise, false.</returns>
		public bool ReqSetupHoverMode(bool enable)
		{
			ByteUtil bf = new ByteUtil();

			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_HoverOnOff)
			  .PutShort(1)
			  .Put((byte)(enable ? 1 : 0))
			  .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf.Clear();
            bf = null;

            return true;
        }

        public async Task<byte[]> ReadAll(StorageFile file)
        {
            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            byte[] bytes = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(buffer);
            return bytes;
        }

        /// <summary>
        /// Requests the firmware installation
        /// </summary>
        /// <param name="filepath">absolute path of firmware file</param>
        /// <returns>true if the request is accepted; otherwise, false.</returns>
        public async void ReqPenSwUpgrade(StorageFile filepath)
		{
			if (IsUploading)
			{
				Debug.WriteLine("[FileUploadWorker] Upgrade task is still excuting.");
				return;
			}

            IsUploading = true;

            mFwChunk = new Chunk(1024);

            //byte[] bytes = await ReadAll(filepath);

            bool loaded = await mFwChunk.Load(filepath);

            if (!loaded)
            {
                return;
            }

            int file_size = mFwChunk.GetFileSize();
            short chunk_count = (short)mFwChunk.GetChunkLength();
            short chunk_size = (short)mFwChunk.GetChunksize();

            byte[] StrByte = Encoding.UTF8.GetBytes("\\N2._v_");

            Debug.WriteLine("[FileUploadWorker] file upload => filesize : {0}, packet count : {1}, packet size {2}", file_size, chunk_count, chunk_size);

            ByteUtil bf = new ByteUtil();

            bf.Put((byte)0xC0)
                .Put((byte)Cmd.P_PenSWUpgradeCommand)
                .PutShort(136)
                .Put(StrByte, 128)
                .PutInt(file_size)
                .PutShort(chunk_count)
                .PutShort(chunk_size)
                .Put((byte)0xC1);

            PenController.PenClient.Write(bf.ToArray());

            bf = null;

            PenController.onStartFirmwareInstallation();

        }

		private void ResponseChunkRequest(short index)
		{
			if (mFwChunk == null)
			{
				return;
			}

			byte[] data = mFwChunk.Get(index);

			if (data == null)
			{
				return;
			}

			byte checksum = mFwChunk.GetChecksum(index);

			short dataLength = (short)(data.Length + 3);

			ByteUtil bf = new ByteUtil();
			bf.Put((byte)0xC0)
			  .Put((byte)Cmd.P_PenSWUpgradeResponse)
			  .PutShort(dataLength)
			  .PutShort(index)
			  .Put(checksum)
			  .Put(data)
			  .Put((byte)0xC1);
			PenController.PenClient.Write(bf.ToArray());

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

        #region Protocol Parse
        
        public void ProtocolParse(byte[] buff, int size)
        {
            for (int i = 0; i < size; i++)
            {
                ParseOneByte(buff[i]);
            }
        }
        
        public void ProtocolParse1(byte[] buff, int size)
		{
			for (int i = 0; i < size; i++)
			{
				if (buff[i] != PKT_START)
				{
					continue;
				}

				Packet.Builder builder = new Packet.Builder();

				int cmd = buff[i + 1];

				int length = ByteConverter.ByteToShort(new byte[] { buff[i + PKT_LENGTH_POS1], buff[i + PKT_LENGTH_POS2] });

				byte[] rs = new byte[length];

				Array.Copy(buff, i + 1 + PKT_HEADER_LEN, rs, 0, length);

				ParsePacket(builder.cmd(cmd).data(rs).Build());

				i += PKT_HEADER_LEN + length;
			}
		}
        
        private bool isStart = true;

        private int counter = 0;

        private ByteUtil mBuffer = null;

        private int dataLength = 0;

        // length
        private byte[] lbuffer = new byte[2];

        private void ParseOneByte(byte data)
        {
            int int_data = (int)(data & 0xFF);

            if (int_data == PKT_START && isStart)
            {
                mBuffer = new ByteUtil();

                counter = 0;
                isStart = false;
            }
            else if (int_data == PKT_END && counter == dataLength + PKT_HEADER_LEN)
            {
                Packet.Builder builder = new Packet.Builder();
                
                // 커맨드를 뽑는다.
                int cmd = mBuffer.GetByteToInt();

                // 길이를 뽑는다.
                int length = mBuffer.GetShort();

                // 커맨드, 길이를 제외한 나머지 바이트를 컨텐트로 지정
                byte[] content = mBuffer.GetBytes();

                Packet packet = builder.cmd(cmd).data(content).Build();

                ParsePacket(packet);

                dataLength = 0;
                counter = 10;
                mBuffer.Clear();
                isStart = true;
            }
            else if (counter > PKT_MAX_LEN)
            {
                counter = 10;
                dataLength = 0;
                isStart = true;
            }
            else
            {
                if (counter == PKT_LENGTH_POS1)
                {
                    lbuffer[0] = data;
                }
                else if (counter == PKT_LENGTH_POS2)
                {
                    lbuffer[1] = data;
                    dataLength = ByteConverter.ByteToShort(lbuffer);
                }

                if (!isStart)
                {
                    mBuffer.Put(data);
                    counter++;
                }
            }
        }

        #endregion
    }
}
