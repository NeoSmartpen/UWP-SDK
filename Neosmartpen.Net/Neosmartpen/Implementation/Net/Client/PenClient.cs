using System;
using System.Diagnostics;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Neosmartpen.Net
{
    public class PenClient : IPenClient
    {
        private DataWriter mDataWriter;
        private DataReader mDataReader;

        public static readonly uint BufferSize = 1024;

        public string Name
        {
            get;set;
        }

        public StreamSocket Socket
        {
            get; set;
        }

        public PenClient(IPenController penctrl)
        {
			PenController = penctrl;
		}

		public void Bind(StreamSocket socket)
		{
            try
            {
                Socket = socket;

                PenController.PenClient = this;

                mDataWriter = new DataWriter(Socket.OutputStream);
                mDataReader = new DataReader(Socket.InputStream);

                mDataReader.InputStreamOptions = InputStreamOptions.Partial;

                PenController.OnConnected();

                ReadSocket(mDataReader);
            }
            catch ( Exception ex )
            {
                Debug.WriteLine("Exception : " + ex.Message);

                switch ((uint)ex.HResult)
                {
                    case (0x80070490): // ERROR_ELEMENT_NOT_FOUND
                        break;
                    default:
                        throw;
                }
            }
		}

        public void Unbind()
        { 
			Socket?.Dispose();
			mDataWriter?.Dispose();
			mDataReader?.Dispose();

			Socket = null;
			mDataWriter = null;
			mDataReader = null;
        }

		public bool Alive
		{
			get
			{
                return mDataReader != null && mDataWriter != null;
            }
            internal set { }
        }

        public IPenController PenController
        {
            get;set;
        }

        private async void WriteSocket(byte[] data)
		{
			// todo check
			mDataWriter.WriteBytes(data);

			try
			{
				await mDataWriter.StoreAsync();
			}
			catch ( Exception exception )
			{
                // todo : disconnect
				if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
				{
					throw;
				}
			}
        }

        private async void ReadSocket(DataReader dataReader)
		{
            try
			{
                uint size = await dataReader.LoadAsync(BufferSize);

                if ( size <= 0 )
                {
                    throw new Exception();
                }

                byte[] data = new byte[size];

                dataReader.ReadBytes(data);

                PenController.OnDataReceived(data);

                ReadSocket(dataReader);
			}
			catch
			{
                onDisconnect();
            }
		}

        private void onDisconnect()
        {
			Unbind();

            PenController.OnDisconnected();
        }

		public void Write(byte[] data)
		{
			WriteSocket(data);
		}

        public void Read()
        {
            ReadSocket(mDataReader);
        }
    }
}