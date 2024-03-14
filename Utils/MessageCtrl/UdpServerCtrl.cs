using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using VitualPersonSpeech.Model;
using VitualPersonSpeech.Tasks;
using VitualPersonSpeech.Utils;
using static VitualPersonSpeech.Utils.HandleMsgTask;

namespace MessageCtrl
{
    class UdpServerCtrl : NetworkCtrl
    {
        private AsyncCallback recv = null;
        private char seperator; // 消息字符串分隔符
        private HandleMsgTask handleMsgTask;
        private ThreadWorker threadWorker;

        private Socket sendMsgSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public UdpServerCtrl(string host, int port, OnMessage onMessage, OutputDebugMsg outputDebugMsg, char seperator) : base(host, port, outputDebugMsg)
        {
            this.seperator = seperator;

            handleMsgTask = new HandleMsgTask(onMessage);
            threadWorker = new ThreadWorker(handleMsgTask);
        }

        public void Init()
        {
            threadWorker.Start();
        }

        protected override void NetStartup()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveBufferSize = 256 * 1024;
            socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            socket.BeginReceiveFrom(readBuffer, 0, readBuffer.Length, SocketFlags.None, ref newClientEP, recv = RecvCallback, socket);

            // 输出调试信息
            string debugMsg = string.Format("启动UDP服务监听, {0}:{1}", host, port);
            outputDebugMsg?.Invoke(debugMsg);
        }

        private void RecvCallback(IAsyncResult ar)
        {
            try
            {
                EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
                int bytes = socket.EndReceiveFrom(ar, ref clientEP);

                string readString = Encoding.UTF8.GetString(readBuffer, 0, bytes);

                // 输出调试信息
                string debugMsg = string.Format("识别端接收到来自 {0} 的消息：{1}", clientEP.ToString(), readString);
                outputDebugMsg?.Invoke(debugMsg);

                // 处理消息
                string[] messages = readString.Split(seperator);
                foreach (string message in messages)
                {
                    if (message.Length > 0)
                    {
                        handleMsgTask.AddClientMsg(message);
                    }
                }

                EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
                socket.BeginReceiveFrom(readBuffer, 0, readBuffer.Length, SocketFlags.None, ref newClientEP, recv, socket);
            }
            catch(Exception ex)
            {
                outputDebugMsg?.Invoke("识别端UDP服务接收消息回调异常，异常信息：" + ex.Message, MSG_TYPE.ERROR);
            }
            
        }

        public void Send(string endIP, int endPort, string dataStr)
        {
            EndPoint serverPoint = new IPEndPoint(IPAddress.Parse(endIP), endPort);
            byte[] data = Encoding.UTF8.GetBytes(dataStr);
            sendMsgSocket.SendTo(data, serverPoint);
        }

        protected override void doClose()
        {
            socket.Close();
            socket = null;
        }
    }
}
