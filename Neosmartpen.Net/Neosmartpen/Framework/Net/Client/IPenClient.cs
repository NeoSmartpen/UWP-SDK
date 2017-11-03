using Windows.Networking.Sockets;

namespace Neosmartpen.Net
{
    /// <summary>
    /// IPenClient class provides fuctions that can handle pen.
    /// </summary>
    public interface IPenClient
    {
        IPenController PenController
        {
            get;
        }

        string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Get the connection status of StreamSocket, ie, whether there is an active connection with remote device.  
        /// </summary>
        bool Alive
        {
            get;
        }

        StreamSocket Socket
        {
            get;
        }

		/// <summary>
		/// To bind connected socket instance to PenClient
		/// </summary>
		/// <param name="socket">StreamSocket instance</param>
		/// <param name="name">it can be setted, if you want name a IPenClient</param>
		void Bind(StreamSocket socket);

        /// <summary>
        /// unbind a socket instance
        /// </summary>
        void Unbind();

        /// <summary>
        /// To write data to stream
        /// </summary>
        /// <param name="data"></param>
        void Write(byte[] data);

        /// <summary>
        /// To read data when device write something
        /// </summary>
        void Read();
    }
}
