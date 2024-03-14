using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace VitualPersonSpeech
{
    public class HttpServerService
    {
        private bool isExcute = false;
        private HttpListener listener = new HttpListener();

        private string ipAddress;
        private int port;

        public delegate void ClientMsgDelegate(string str);
        public ClientMsgDelegate clientMsg;


        public delegate void DoDataDelegate(string data);
        public DoDataDelegate doData;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口号</param>
        public HttpServerService(string ipAddress, int port, ClientMsgDelegate clientMsg, DoDataDelegate setData)
        {
            this.ipAddress = ipAddress;
            this.port = port;

            this.clientMsg = clientMsg;
            this.doData = setData;
        }


        public void Start()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(w => Excute());//单独开启一个线程执行监听消息
        }

        private void Excute()
        {
            if (HttpListener.IsSupported)
            {
                if (!listener.IsListening)
                {
                    try
                    {
                        listener.Prefixes.Add(string.Format("http://{0}:{1}/", ipAddress, port)); //添加需要监听的url
                        listener.Start(); //开始监听端口，接收客户端请求

                        isExcute = true;

                        clientMsg("http服务启动完成，服务地址：" + string.Format("http://{0}:{1}/", ipAddress, port));
                    }
                    catch (Exception exception)
                    {
                        clientMsg("http服务启动发生异常，异常信息：" + exception.Message);
                    }

                }
                while (isExcute)
                {
                    try
                    {
                        //阻塞主函数至接收到一个客户端请求为止  等待请求
                        HttpListenerContext context = listener.GetContext();
                        //解析请求
                        HttpListenerRequest request = context.Request;
                        //构造响应
                        HttpListenerResponse response = context.Response;
                        //http请求方式：get，post等等
                        string httpMethod = request.HttpMethod?.ToLower();

                        if (httpMethod == "get")
                        {
                            clientMsg("接收到http请求，请求地址：" + request.RawUrl);
                            string[] urls = request.RawUrl.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                            if (urls.Length > 0)
                            {
                                string askStr = HttpUtility.UrlDecode(urls[0]);

                                if (askStr.Contains("askStr="))
                                {
                                    clientMsg("接收到http请求，具体中文内容：" + askStr);
                                    doData(askStr.Replace("askStr=", ""));
                                }
                                else
                                {
                                    clientMsg("接收到http请求，具体中文内容：" + askStr + "，不满足要求");
                                }
                            }
                        }

                        byte[] buffer = Encoding.UTF8.GetBytes("SUCCESS");
                        //对客户端输出相应信息.
                        response.ContentLength64 = buffer.Length;
                        //response.StatusCode = 200;
                        //response.ContentType = "text/plain";
                        //发送响应
                        using (Stream output = response.OutputStream)
                        {
                            output.Write(buffer, 0, buffer.Length);
                        }
                    }
                    catch (Exception exceotion)
                    {
                        clientMsg("处理http请求发生异常，异常信息：" + exceotion.Message);
                    }
                }
            }
            else
            {
                //Logger.Info("系统不支持HttpListener");
            }
        }

        public void Stop()
        {
            isExcute = false;
            if (listener.IsListening)
                listener.Stop();
        }
    }
}
