using System.Net;
using System.Net.Sockets;

namespace MessageCtrl
{
    public abstract class NetworkCtrl : MessageCtrlBase
    {
        protected string host;
        // The udpServerPort number for the remote device.  
        protected int port;
        protected Socket socket;
        // Send buffer.  
        public byte[] readBuffer = new byte[1024];

        public NetworkCtrl(string host, int port, OutputDebugMsg outputDebugMsg) : base(outputDebugMsg)
        {
            this.host = host;
            this.port = port;

            // Establish the remote endpoint for the socket.  
            IPAddress ipAddress = IPAddress.Parse(host);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            NetStartup();
        }

        protected abstract void NetStartup();
    }
}
