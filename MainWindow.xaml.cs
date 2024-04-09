using log4net;
using MessageCtrl;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VitualPersonSpeech.Model;
using VitualPersonSpeech.Utils;
using VitualPersonSpeech.Utils.AIImgUtils;
using VitualPersonSpeech.Utils.AIReplyUtils;
using VitualPersonSpeech.Utils.AudioToFaceUtils;
using VitualPersonSpeech.Utils.KnowledgeBaseUtils;
using VitualPersonSpeech.Utils.TTSUtils;
using VoiceRecorder.Audio;
using WebSocket4Net;
using static VitualPersonSpeech.Utils.MultiScreenPlayerCmdUtils;
using static VitualPersonSpeech.Utils.VirtualHumanCmdUtils;

namespace VitualPersonSpeech
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 打印日志
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // 配置信息
        private JObject configDataJObect;

        //http服务
        private HttpServerService httpServerService;

        // 消息接受发送服务
        private UdpServerCtrl udpServerCtrl;// udp消息服务器
        private string udpServerIP = "127.0.0.1";// 本地服务ip
        private int udpServerPort = 5500; // 本地服务端口

        private string virtualHumanSeverIP = "127.0.0.1";// 虚拟人展示端服务ip
        private int virtualHumanSeverPort = 5501; // 虚拟人展示端服务端口

        private string multiScreenPlayerIP = ""; // 当前播放推流展示端ip
        private int multiScreenPlayerPort = 5501; // 推流展示端口
            
        // 判断刷新udp,判断虚拟数字人是否在线
        private DispatcherTimer checkVirtualPersonTimer;
        private int checkVirtualPersonNumber = 5;
        private DateTime lastGetHeartbeatTime = DateTime.Now.AddMinutes(-10f);
        private bool isVirtualPersonLive = false;

        private WaveInEvent waveIn;// 获取录音设备
        private bool isSaveWaveFile = false; // 是否保存音频文件到本地
        private WaveFileWriter waveFileWriter;
        private byte[] wavBytes = new byte[0];// 音频数据字节组

        private AudioRecorder audioRecorder;// 录音音量分析等
        private float lastPeak;//说话音量
        int Ends = -1;
        List<VoiceData> VoiceBuffer = new List<VoiceData>();

        // 语音唤醒
        // 讯飞语音唤醒
        private const string xunfeiWakeOnAppId = "af564733";
        XunFeiUtils.ivw_ntf_handler IVW_callback;
        AudioStatus aud_stat = AudioStatus.ISR_AUDIO_SAMPLE_CONTINUE;

        // 实时流语音识别
        private WebSocket webSocket; // 进行websocket识别的websocket
        private string asrStr = ""; // 使用websocket识别出来的文字

        // 识别状态：第一帧、中间帧、尾帧 websocket模式使用
        enum Status
        {
            FirstFrame = 0,
            ContinueFrame = 1,
            LastFrame = 2,
            NoFrame = 3
        }
        private Status status = Status.NoFrame;

        // 讯飞流式语音识别api信息
        private const string xunfeiAppid = "a30a26fc";
        private const string xunfeiApiKey = "4c9014586f0ca11682e6f218293a0867";
        private const string xunfeiApiSecret = "MjFlNzE3ZGYyOTQxNzMzMDgwODNkYjRi";
        private string xunfeiCurAsrStr = ""; // 讯飞当前句识别asr，因为pgs会有rpl和apd区别
        private List<string> xunfeiAsrStrList = new List<string>();

        // 知识库
        private ParthenonKnowledgeBase parthenonKnowledgeBase;

        // AI应答(百度文心一言)
        private BaiduWenXinAIReply baiduWenXinAIReply;

        // 百度语音合成
        private BaiduTTS baiduTTS;

        // AI作画 百度ai作画
        private BaiduAIImg baiduAIImg;

        // Audio2Face
        private NvAuido2Face nvAuido2Face;

        private Thread thread;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region 界面相关事件
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            thread = new Thread(new ThreadStart(DoAsrAndAIReply));
            InitLog4net();

            DebugMessage("虚拟数字人语音识别模块运行");

            LoadConfigData();

            InitUdpServer();

            StartCheckVirtualPersonTimer();

            InitWaveIn();

            StartWaveInRecording();

            InitAudioRecorder();

            StartWakeOn();

            InitParthenonKnowledgeBase();

            InitNvAudio2Face();

            InitBaiduAIImg();

            InitBaiduTTS();

            InitBaiduWenXinAiReply();

            //StartHttpServerService();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisposeWaveInFileWriter();
            Environment.Exit(0);
        }

        // 开始录音按钮点击事件
        private void StartRecording_Button_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(StartWaveInRecording));
            thread.Start();
        }

        // 结束录音按钮点击事件
        private void StopRecording_Button_Click(object sender, RoutedEventArgs e)
        {
            StopWaveInRecording();
        }

        // 录音3s自动识别按钮点击事件
        private void AutoRecordingFor3s_Button_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(AutoRecordingFor3s));
            thread.Start();
        }

        // 开始流式识别按钮点击事件
        private void StartWebsocketRecording_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenAsrWebSocket();
        }

        // 结束流式识别按钮点击事件
        private void StopWebsocketRecording_Button_Click(object sender, RoutedEventArgs e)
        {
            status = Status.LastFrame;
        }

        // 开始语音唤醒按钮点击事件
        private void StartAwakOnWord_Button_Click(object sender, RoutedEventArgs e)
        {
            StartWakeOn();
        }

        // 结束语音唤醒按钮点击事件
        private void StopAwakOnWord_Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        // ai应答点击按钮
        private void AIReply_Button_Click(object sender, RoutedEventArgs e)
        {
            //ChatWenXin(AIReply_TextBox.Text);
            asrStr = AIReply_TextBox.Text;
            //AIReply(asrStr);
            DebugMessage("语音唤醒成功!", MSG_TYPE.INFO);
            if (thread.ThreadState == ThreadState.Running)
            {
                try
                {
                    thread.Abort();
                    DebugMessage("关闭正在运行的识别应答线程");
                }
                catch (Exception ex)
                {
                    DebugMessage($"关闭正在运行的识别应答线程出现异常，异常信息：{ex.Message}", MSG_TYPE.ERROR);
                }
            }
            thread = new Thread(new ThreadStart(DoAsrAndAIReply));
            thread.Start();
        }

        private void Audio2FaceLoadUSD_Button_Click(object sender, RoutedEventArgs e)
        {
            nvAuido2Face.LoadUSD();
        }

        private void Audio2FaceLoadDefaultWav_Button_Click(object sender, RoutedEventArgs e)
        {
            nvAuido2Face.SetDefaultWav();
        }

        private void Audio2FacePlayWav_Button_Click(object sender, RoutedEventArgs e)
        {
            nvAuido2Face.Play();
        }

        private void Audio2FacePauseWav_Button_Click(object sender, RoutedEventArgs e)
        {
            nvAuido2Face.Pause();
        }

        private void Audio2FaceActicateStreamLivelink_Button_Click(object sender, RoutedEventArgs e)
        {
            nvAuido2Face.ActicateStreamLivelink();
        }
        #endregion

        #region http服务
        private void StartHttpServerService()
        {
            try
            {
                string httpServerIP = configDataJObect["httpServer"]["ip"].ToString();
                int httpServerPort = int.Parse(configDataJObect["httpServer"]["port"].ToString());

                httpServerService = new HttpServerService(httpServerIP, httpServerPort, HttpDebugMessage, DoData);
                httpServerService.Start();
            }
            catch (Exception exception)
            {
                DebugMessage("运行服务出现异常，异常信息:" + exception.Message);
            }
        }

        private void HttpDebugMessage(string data)
        {
            DebugMessage(data);
        }

        private void DoData(string data)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    AIReply(data);
                }));
            }
            else
            {
                AIReply(data);
            }
        }
        #endregion

        #region 录音设备
        /// <summary>
        /// 初始化设置录音设备
        /// </summary>
        private void InitWaveIn()
        {
            // 设置录音设备
            DebugMessage("初始化设置录音设备");
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(16000, 1); // 采样率为 16000，单声道
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;
        }

        /// <summary>
        /// 开始录音
        /// </summary>
        private void StartWaveInRecording()
        {
            DebugMessage("开始录音");

            status = Status.NoFrame;

            // 开始录音
            waveIn.StartRecording();
        }

        /// <summary>
        /// 结束录音
        /// </summary>
        private void StopWaveInRecording()
        {
            if (waveIn != null)//结束录音
            {
                waveIn.StopRecording();
            }

            DebugMessage("结束录音");
        }

        /// <summary>
        /// 接受录音数据的回传事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            // 进行语音唤醒
            if (isStartIVW) // 是否打开语音唤醒
            {
                VoiceData data = new VoiceData();
                Buffer.BlockCopy(e.Buffer, 0, data.data, 0, 3200);
                VoiceBuffer.Add(data);

                if (lastPeak < 20)
                {
                    Ends = Ends - 1;
                }
                else
                {
                    Ends = 5;
                }

                if (Ends == 0)
                {
                    if (VoiceBuffer.Count() > 5) // 有效音频数据，过小则认为不存在
                    {
                        RunAwaken(VoiceBuffer, session_begin_params);//调用语音唤醒
                    }
                    VoiceBuffer.Clear();
                    Ends = 5;
                }
            }

            if (status != Status.NoFrame)
            {
                if (status == Status.FirstFrame)// 握手
                {
                    dynamic frame = new JObject();
                    frame.common = new JObject
                    {
                        {"app_id",xunfeiAppid},
                    };

                    frame.business = new JObject
                    {
                        { "language","zh_cn"},
                        { "domain", "iat"},
                        { "accent", "mandarin"},
                        { "dwa","wpgs"}
                    };
                    frame.data = new JObject
                    {
                        { "status", 0 },
                        { "format","audio/L16;rate=16000"},
                        { "encoding", "raw"},
                        { "audio", Convert.ToBase64String(e.Buffer)}

                    };
                    webSocket.Send(frame.ToString());
                    status = Status.ContinueFrame;
                }
                else if (status == Status.ContinueFrame)// 开始发送和继续发送
                {
                    dynamic frame = new JObject();
                    frame.data = new JObject
                    {
                        { "status", 1 },
                        { "format","audio/L16;rate=16000"},
                        { "encoding", "raw"},
                        { "audio", Convert.ToBase64String(e.Buffer)}

                    };
                    webSocket.Send(frame.ToString());
                }
                else if (status == Status.LastFrame)
                {
                    dynamic frame = new JObject();
                    frame.data = new JObject
                    {
                        { "status", 2 }
                    };
                    webSocket.Send(frame.ToString());

                    status = Status.NoFrame;
                    DebugMessage("流式语音识别结束");
                }
            }
        }

        /// <summary>
        /// 停止录音回调事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            // 录音停止时的处理
            if (e.Exception != null)
            {
                DebugMessage("录音出现异常:" + e.Exception.Message, MSG_TYPE.ERROR);
            }

            if (waveFileWriter != null) // 关闭文件流
            {
                waveFileWriter.Close();
            }
        }

        /// <summary>
        /// 关闭和释放录音设备资源
        /// </summary>
        private void DisposeWaveInFileWriter()
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
            }

            if (waveFileWriter != null)
            {
                waveFileWriter.Close();
                waveFileWriter.Dispose();
            }
        }

        private void InitAudioRecorder()
        {
            DebugMessage("初始化设置音频记录器");
            audioRecorder = new AudioRecorder();//录音设备
            audioRecorder.BeginMonitoring(-1);
            audioRecorder.SampleAggregator.MaximumCalculated += OnRecorderMaximumCalculated;
        }

        /// <summary>
        /// 计算获取到的声音的最大值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnRecorderMaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            lastPeak = Math.Max(e.MaxSample, Math.Abs(e.MinSample)) * 100;
            //DebugMessage(string.Format("音量:{0}", lastPeak));
        }
        #endregion

        #region 普通接口语音识别
        /// <summary>
        /// 自动录音3s并识别
        /// </summary>
        private void AutoRecordingFor3s()
        {
            StartWaveInRecording();

            Thread.Sleep(3000);

            StopWaveInRecording();

            //BaiduAIUtils.Instance.GetTTS().Synthesis();

            //BaiduAsrResult baiduAsrResult = BaiduAIUtils.Instance.GetAsr().Recognize(wavBytes, "wav", 16000).ToObject<BaiduAsrResult>();
            //DebugMessage(baiduAsrResult.result[0]);
        }
        #endregion

        #region websocket语音识别
        /// <summary>
        /// 打开流式语音识别websocket连接
        /// </summary>
        private void OpenAsrWebSocket()
        {
            string websocketUrl = "";

            // 建立讯飞实时识别WebSocket的地址
            string apiAddress = "wss://iat-api.xfyun.cn/v2/iat";
            string host = "wss://iat-api.xfyun.cn";
            string dateTimeStr = DateTime.UtcNow.ToString("r");
            string signatureOrigin = string.Format("host: {0}\ndate: {1}\nGET /v2/iat HTTP/1.1", host, dateTimeStr);
            string signature = EncryptUtils.HMACSHA256Text(signatureOrigin, xunfeiApiSecret);
            string authorizationOrigin = string.Format("api_key=\"{0}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{1}\"", xunfeiApiKey, signature);
            string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationOrigin));

            websocketUrl = string.Format("{0}?authorization={1}&date={2}&host={3}", apiAddress, authorization, dateTimeStr, host);

            webSocket = new WebSocket(websocketUrl);
            //开始请求 WebSocket
            webSocket.Opened += OnOpened;
            webSocket.DataReceived += OnDataReceived;
            webSocket.MessageReceived += OnMessageReceived;
            webSocket.Closed += OnClosed;
            //webSocket.Error += OnError;

            webSocket.Open();
        }

        /// <summary>
        /// 检查麦克风
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOpened(object sender, EventArgs e)
        {
            status = Status.FirstFrame;
        }

        /// <summary>
        /// websocket接受数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            DebugMessage("WebSocket接受到数据，数据长度：" + e.Data.Length);
        }

        /// <summary>
        /// websocket接受信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //DebugMessage(string.Format("接收到讯飞WebSocket服务端发送的信息:{0}", e.Message));

            XunfeiAsrResult xunfeiAsrResult = JsonConvert.DeserializeObject<XunfeiAsrResult>(e.Message);

            if (xunfeiAsrResult == null)
            {
                return;
            }

            if (xunfeiAsrResult.code == 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Ws ws in xunfeiAsrResult.data.result.wss)
                {
                    foreach (Cw cw in ws.cws)
                    {
                        sb.Append(cw.w);
                    }
                }

                if (xunfeiAsrResult.data.result.pgs == "rpl")
                {
                    // 当为替代时，更新当前语句识别内容
                    xunfeiCurAsrStr = sb.ToString();
                }
                else if (xunfeiAsrResult.data.result.pgs == "apd")
                {
                    // 当为添加时，1、将原有内容添加的内容list中，2、更新当前语句内容
                    xunfeiAsrStrList.Add(xunfeiCurAsrStr);
                    xunfeiCurAsrStr = sb.ToString();
                }

                if (xunfeiAsrResult.data.status == 0 || xunfeiAsrResult.data.status == 1)
                {
                    // 当为中间句，则继续识别
                    DebugMessage(string.Format("识别讯飞WebSocket<第一块结果>或<中间结果>的消息:{0}", sb.ToString()));
                }
                else if (xunfeiAsrResult.data.status == 2) // 结束标志
                {
                    // 当为结束句，则将现有识别内容也添加如内容list，进行拼接
                    DebugMessage(string.Format("接收到讯飞WebSocket识别为<最后一块结果>的消息:{0},关闭WebSocket", sb.ToString()));

                    xunfeiAsrStrList.Add(xunfeiCurAsrStr);
                    asrStr = string.Join("", xunfeiAsrStrList);

                    status = Status.LastFrame;

                    DebugMessage(string.Format("当前讯飞WebSocket识别语音为:{0}", asrStr));
                    xunfeiAsrStrList.Clear();

                    if (string.IsNullOrEmpty(asrStr))
                    {
                        DebugMessage("未识别到麦克风输入，请检查麦克分", MSG_TYPE.WARNNING);
                        Ctrl2SayNoVoice();
                    }
                    else
                    {

                        // 进行语音结果判断，是否是ai作画
                        if (baiduAIImg.MatchAIImg(asrStr)) // 先判断是作画还是应答
                        {
                            DebugMessage("语句与AI作画正则匹配成功，进行AI作画");

                            AIImg(asrStr);
                        }
                        else
                        {
                            DebugMessage("语句与AI作画正则匹配不成功，进行AI应答问答");

                            AIReply(asrStr);
                        }
                    }
                }
            }
            else
            {
                DebugMessage(string.Format("识别讯飞WebSocket服务端发送为错误信息，code:{0}, message:{1}", xunfeiAsrResult.code, xunfeiAsrResult.message), MSG_TYPE.ERROR);
                return;
            }
        }

        /// <summary>
        /// websocket关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClosed(object sender, EventArgs e)
        {
            DebugMessage("已关闭WebSocket连接");
        }

        /// <summary>
        /// websocket错误事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            DebugMessage("WebSocket出现错误，错误信息：" + e.Exception.Message, MSG_TYPE.ERROR);
            //StopWaveInRecording();
            //webSocket.Close();
        }
        #endregion

        #region 语音唤醒
        int ret = (int)ErrorCode.MSP_SUCCESS;
        //bool awaken_flag = false;
        IntPtr session_id = IntPtr.Zero;
        int errcode = 0;
        string session_begin_params = "ivw_threshold=0:1450, sst=wakeup, ivw_res_path =fo|res/ivw/wakeupresource.jet";
        bool isStartIVW = false;

        // 开始讯飞语音唤醒
        private void StartWakeOn()
        {
            string login_params = string.Format("appid={0},word_dir= . ", xunfeiWakeOnAppId);//appid和msc.dll要配套
            ret = XunFeiUtils.MSPLogin("", "", login_params);

            if (ret != (int)ErrorCode.MSP_SUCCESS)
            {
                DebugMessage(string.Format("语音唤醒初始化msc，用户登录失败，ErrorCode:{0}", ret), MSG_TYPE.ERROR);
                return;
            }

            //初始化语音唤醒            
            IVW_callback = cb_ivw_msg_proc;
            GC.KeepAlive(IVW_callback);

            session_id = XunFeiUtils.QIVWSessionBegin(null, session_begin_params, ref errcode);

            if ((int)ErrorCode.MSP_SUCCESS != errcode)
            {
                DebugMessage(string.Format("语音唤醒开启Session失败，ErrorCode:{0}", errcode), MSG_TYPE.ERROR);
                return;
            }

            IntPtr userDATA = IntPtr.Zero;
            errcode = XunFeiUtils.QIVWRegisterNotify(CommonUtils.PtrToStr(session_id), IVW_callback, userDATA);
            GC.KeepAlive(IVW_callback);

            isStartIVW = true;
            DebugMessage("开启讯飞语音唤醒成功");
        }

        //回调函数 
        private int cb_ivw_msg_proc(string sessionID, int msg, int param1, int param2, IntPtr info, IntPtr userData)
        {
            
            if (msg == 2) //唤醒出错消息
            {
                DebugMessage(string.Format("语音唤醒回调函数：cb_ivw_msg_proc出现错误"), MSG_TYPE.ERROR);
            }
            else if (msg == 1)//唤醒成功消息
            {
                DebugMessage("语音唤醒成功!", MSG_TYPE.INFO);
                if(thread.ThreadState == ThreadState.Running)
                {
                    try
                    {
                        thread.Abort();
                        DebugMessage("关闭正在运行的识别应答线程");
                    }
                    catch(Exception ex)
                    {
                        DebugMessage($"关闭正在运行的识别应答线程出现异常，异常信息：{ex.Message}", MSG_TYPE.ERROR);
                    }
                }
                thread = new Thread(new ThreadStart(DoAsrAndAIReply));
                thread.Start();
            }
            return 0;
        }

        // 语音唤醒输入语音数据
        private void RunAwaken(List<VoiceData> VoiceBuffer, string session_begin_params)
        {
            for (int i = 0; i < VoiceBuffer.Count(); i++)
            {
                if (i == 0)
                {
                    aud_stat = AudioStatus.ISR_AUDIO_SAMPLE_FIRST;
                }
                else
                {
                    aud_stat = AudioStatus.ISR_AUDIO_SAMPLE_CONTINUE;
                }

                try
                {
                    errcode = XunFeiUtils.QIVWAudioWrite(CommonUtils.PtrToStr(session_id), VoiceBuffer[i].data, (uint)VoiceBuffer[i].data.Length, aud_stat);
                }
                catch (Exception ex)
                {
                    DebugMessage(string.Format("语音唤醒输入语音数据出现异常,异常信息：{0}", ex.Message), MSG_TYPE.ERROR);
                    XunFeiUtils.MSPLogout();
                    break;
                }
                //errcode = MSCDLL.QIVWAudioWrite(PtrToStr(session_id), VoiceBuffer[i]._data, (uint)VoiceBuffer[i]._data.Length, aud_stat);                                
                if ((int)ErrorCode.MSP_SUCCESS != errcode)
                {
                    XunFeiUtils.QIVWSessionEnd(CommonUtils.PtrToStr(session_id), null);
                }
            }
            errcode = XunFeiUtils.QIVWAudioWrite(CommonUtils.PtrToStr(session_id), null, 0, AudioStatus.ISR_AUDIO_SAMPLE_LAST);
            if ((int)ErrorCode.MSP_SUCCESS != errcode)
            {
                //SetText("\nQIVWAudioWrite failed! error code:" + errcode);
                return;
            }

        }
        #endregion

        private void DoAsrAndAIReply()
        {
            DebugMessage("数字人响应欢迎词");
            Ctrl2SayWelcome();

            DebugMessage("800ms后开始流式语音识别");
            Thread.Sleep(800);

            OpenAsrWebSocket();
        }
       
        // AI应答
        // AI作画
        private void AIImg(string asrStr)
        {
            ResultMsg baiduAIImgResultMsg = baiduAIImg.Text2Img(asrStr);

            if (baiduAIImgResultMsg.StatusCode == StatusCode.SUCCESS)
            {
                UdpSendMessage2VirtualHumanSever(VirtualHumanCmdUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgSuccess, baiduAIImgResultMsg.Data.ToString()));
                DebugMessage(string.Format("AI作画生成成功,画作地址:{0}", baiduAIImgResultMsg.Data.ToString()));
            }
            else
            {
                UdpSendMessage2VirtualHumanSever(VirtualHumanCmdUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgFail));
                DebugMessage("AI作画生成失败", MSG_TYPE.ERROR);
            }
        }

        private void InitBaiduAIImg()
        {
            try
            {
                List<string> qaAIImgPatternList = configDataJObect["qaAIImgPattern"].ToObject<List<string>>();
                DebugMessage(string.Format("获取语句与AI作画正则表达式设置成功，共计：{0} 条", qaAIImgPatternList.Count));

                baiduAIImg = new BaiduAIImg(qaAIImgPatternList);
                baiduAIImg.SendMsgEvent += HandleOnSendMsg;

                baiduAIImg.GetAccessToken();
            }
            catch (Exception ex)
            {
                DebugMessage($"初始化设置Baidu AI Img库相关设置出现异常，异常信息：{ex.Message}，使用默认信息", MSG_TYPE.ERROR);
            }
        }

        // 行业知识库问答
        public void AIReply(string asrStr)
        {
            ResultMsg parthenonKnowledgeBaseResultMsg = parthenonKnowledgeBase.AnswerQuest(asrStr);
            //ResultMsg parthenonKnowledgeBaseResultMsg = ResultMsg.Error("");

            if (parthenonKnowledgeBaseResultMsg.StatusCode == StatusCode.SUCCESS)
            {
                IndustryQASimilarResult industryQASimilarResult = (IndustryQASimilarResult)parthenonKnowledgeBaseResultMsg.Data;

                DebugMessage(string.Format("行业知识库匹配完成，ES权重：{0}，NLP相似度：{1}，原问题：{2}，匹配问题：{3}，匹配回答：{4}", industryQASimilarResult.relevance, industryQASimilarResult.similarity, industryQASimilarResult.questStr, industryQASimilarResult.similarStr, industryQASimilarResult.industryQAChatReply.replyContent));

                if (industryQASimilarResult.relevance > parthenonKnowledgeBase.RelevanceThreshold && industryQASimilarResult.similarity > parthenonKnowledgeBase.SimilarityThreshold)
                {
                    DebugMessage("行业知识库匹配结果满足权重阈值和相似度阈值，使用行业知识库回答进行应答");

                    // 查看音频链接地址，本地有就播放本地，没有就下载，最后才是百度文字转语音，
                    if (string.IsNullOrEmpty(industryQASimilarResult.industryQAChatReply.audioFileSource))// 文件信息为空，直接进行文字转语音
                    {
                        DebugMessage("接口返回信息没有音频链接地址，进行文字转语音");
                        ResultMsg baiduTTSResultMsg = baiduTTS.TTS(industryQASimilarResult.industryQAChatReply.replyContent);

                        if (baiduTTSResultMsg.StatusCode == StatusCode.SUCCESS)
                        {
                            DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                            Ctrl2SayWav(Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                        }
                        else
                        {
                            DebugMessage("百度短文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                            Ctrl2SayApiError();
                        }
                    }
                    else// 文件信息不为空，判断本地有没有音频文件，本地有就直接播放，没有就下载，下载出错就文字转语音
                    {
                        if(File.Exists(parthenonKnowledgeBase.LocalFileDictionary + Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource)))
                        {
                            DebugMessage("本地已有回答音频文件，数字人开始说话:" + Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource));
                            Ctrl2SayWav(Path.GetFileName(Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource)));
                            return;
                        }

                        DebugMessage("本地没有回答音频文件，下载音频文件");

                        ResultMsg donwloadFileResultMsg = HttpUtils.DownloadFile(string.Format(parthenonKnowledgeBase.DownloadFileAPIUrl, industryQASimilarResult.industryQAChatReply.audioFileSource), parthenonKnowledgeBase.LocalFileDictionary + Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource));

                        if (donwloadFileResultMsg.StatusCode == StatusCode.SUCCESS)// 文件下载完成
                        {
                            DebugMessage("回答音频文件下载完成，进行播放");
                            Ctrl2SayWav(Path.GetFileName(Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource)));

                        }
                        else// 文件下载出错
                        {
                            DebugMessage("回答音频文件下载出错，开始文字转音频");

                            ResultMsg baiduTTSResultMsg = baiduTTS.TTS(industryQASimilarResult.industryQAChatReply.replyContent);

                            if (baiduTTSResultMsg.StatusCode == StatusCode.SUCCESS)
                            {
                                DebugMessage("Baidu 文本转语音完成，数字人开始说话:" + Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                                Ctrl2SayWav(Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                            }
                            else
                            {
                                DebugMessage("Baidu 文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                                Ctrl2SayApiError();
                            }
                        }
                    }
                }
                else
                {
                    DebugMessage("行业知识库匹配结果不满足相似度阈值或权重，进行百度文心AI应答");
                    ResultMsg baiduWenXinAIReplyResultMsg = baiduWenXinAIReply.Chat_ERNIE_3D5_8K(asrStr);

                    if (baiduWenXinAIReplyResultMsg.StatusCode == StatusCode.SUCCESS)
                    {
                        ResultMsg baiduTTSResultMsg = baiduTTS.TTS(baiduWenXinAIReplyResultMsg.Data.ToString());

                        if (baiduTTSResultMsg.StatusCode == StatusCode.SUCCESS)
                        {
                            DebugMessage("Baidu 文本转语音完成，数字人开始说话:" + Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                            Ctrl2SayWav(Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                        }
                        else
                        {
                            DebugMessage("Baidu 文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                            Ctrl2SayApiError();
                        }
                    }
                    else
                    {

                        DebugMessage("调用文心ai应答接口出现错误，数字人响应api错误");
                        Ctrl2SayApiError();
                    }
                }
            }
            else
            {
                DebugMessage("行业知识库未匹配到相关问答，进行百度文心AI应答");
                ResultMsg baiduWenXinAIReplyResultMsg = baiduWenXinAIReply.Chat_ERNIE_3D5_8K(asrStr);

                if (baiduWenXinAIReplyResultMsg.StatusCode == StatusCode.SUCCESS)
                {
                    ResultMsg baiduTTSResultMsg = baiduTTS.TTS(baiduWenXinAIReplyResultMsg.Data.ToString());

                    if (baiduTTSResultMsg.StatusCode == StatusCode.SUCCESS)
                    {
                        DebugMessage("Baidu 文本转语音完成，数字人开始说话:" + Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                        Ctrl2SayWav(Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                    }
                    else
                    {
                        DebugMessage("Baidu 文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                        Ctrl2SayApiError();
                    }
                }
                else
                {
                    DebugMessage("调用文心ai应答接口出现错误，数字人响应api错误");
                    Ctrl2SayApiError();
                }
            }
        }

        private void InitParthenonKnowledgeBase()
        {
            try
            {
                float industryQARelevanceThreshold = (float)configDataJObect["industryQADataSetting"]["relevanceThreshold"];
                float industryQASimilarityThreshold = (float)configDataJObect["industryQADataSetting"]["similarityThreshold"];
                string industryQALocalFileDictionary = configDataJObect["industryQADataSetting"]["localFileDictionary"].ToString();

                string industryQAGetTokenAPIUrl = configDataJObect["industryQADataSetting"]["getTokenAPIUrl"].ToString();
                string industryQAGetAllReplysAndScenesAPIUrl = configDataJObect["industryQADataSetting"]["getAllReplysAndScenesAPIUrl"].ToString();
                string industryQASimilarAPIUrl = configDataJObect["industryQADataSetting"]["similarAPIUrl"].ToString();
                string industryQADownloadFileAPIUrl = configDataJObect["industryQADataSetting"]["downloadFileAPIUrl"].ToString();

                DebugMessage(string.Format("获取行业知识库相关设置完成，API地址：{0}，权重阈值：{1}，相似度阈值：{2}", industryQASimilarAPIUrl, industryQARelevanceThreshold, industryQASimilarityThreshold));

                parthenonKnowledgeBase = new ParthenonKnowledgeBase(industryQARelevanceThreshold, industryQASimilarityThreshold, industryQALocalFileDictionary, industryQAGetTokenAPIUrl, industryQAGetAllReplysAndScenesAPIUrl, industryQASimilarAPIUrl, industryQADownloadFileAPIUrl);
                parthenonKnowledgeBase.SendMsgEvent += HandleOnSendMsg;

                parthenonKnowledgeBase.GetAccessToken();

                parthenonKnowledgeBase.DownloadAllKnowledgeBaseFile();
            }
            catch (Exception ex)
            {
                DebugMessage($"初始化行业知识库相关设置出现异常，异常信息：{ex.Message}，使用默认信息", MSG_TYPE.ERROR);
            }
        }

        private void InitBaiduWenXinAiReply()
        {
            baiduWenXinAIReply = new BaiduWenXinAIReply();
            baiduWenXinAIReply.SendMsgEvent += HandleOnSendMsg;

            baiduWenXinAIReply.GetAccessToken();
        }

        private void PlayLocalIndustryQAChatReply(IndustryQAChatReply industryQAChatReply)
        {
            DebugMessage("播放本地知识库内容信息");
            // 查看音频链接地址，本地有就播放本地，没有就下载，最后才是百度文字转语音，
            if (string.IsNullOrEmpty(industryQAChatReply.audioFileSource))// 文件信息为空，直接进行文字转语音
            {
                DebugMessage("接口返回信息没有音频链接地址，进行文字转语音");
                ResultMsg baiduTTSResultMsg = baiduTTS.TTS(industryQAChatReply.replyContent);

                if (baiduTTSResultMsg.StatusCode == StatusCode.SUCCESS)
                {
                    DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                    Ctrl2SayWav(Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                }
                else
                {
                    DebugMessage("百度短文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                    Ctrl2SayApiError();
                }
            }
            else// 文件信息不为空，判断本地有没有音频文件，本地有就直接播放，没有就下载，下载出错就文字转语音
            {
                if (File.Exists(parthenonKnowledgeBase.LocalFileDictionary + Path.GetFileName(industryQAChatReply.audioFileSource)))
                {
                    DebugMessage("本地已有回答音频文件，数字人开始说话:" + Path.GetFileName(industryQAChatReply.audioFileSource));
                    Ctrl2SayWav(Path.GetFileName(Path.GetFileName(industryQAChatReply.audioFileSource)));
                    return;
                }

                DebugMessage("本地没有回答音频文件，下载音频文件");

                ResultMsg donwloadFileResultMsg = HttpUtils.DownloadFile(string.Format(parthenonKnowledgeBase.DownloadFileAPIUrl, industryQAChatReply.audioFileSource), parthenonKnowledgeBase.LocalFileDictionary + Path.GetFileName(industryQAChatReply.audioFileSource));

                if (donwloadFileResultMsg.StatusCode == StatusCode.SUCCESS)// 文件下载完成
                {
                    DebugMessage("回答音频文件下载完成，进行播放");
                    Ctrl2SayWav(Path.GetFileName(Path.GetFileName(industryQAChatReply.audioFileSource)));

                }
                else// 文件下载出错
                {
                    DebugMessage("回答音频文件下载出错，开始文字转音频");

                    ResultMsg baiduTTSResultMsg = baiduTTS.TTS(industryQAChatReply.replyContent);

                    if (baiduTTSResultMsg.StatusCode == StatusCode.SUCCESS)
                    {
                        DebugMessage("Baidu 文本转语音完成，数字人开始说话:" + Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                        Ctrl2SayWav(Path.GetFileName(baiduTTSResultMsg.Data.ToString()));
                    }
                    else
                    {
                        DebugMessage("Baidu 文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                        Ctrl2SayApiError();
                    }
                }
            }
        }

        // Baidu语音在线合成
        private async void InitBaiduTTS()
        {
            string industryQALocalFileDictionary = configDataJObect["industryQADataSetting"]["localFileDictionary"].ToString();
            baiduTTS = new BaiduTTS(industryQALocalFileDictionary);
            baiduTTS.SendMsgEvent += HandleOnSendMsg;

            await baiduTTS.GetAccessToken();
        }

        #region 发送Audio2Face和数字人控制信息
        private void Ctrl2StandBy()
        {
            nvAuido2Face.Mute();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2Play()
        {
            nvAuido2Face.Play();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.TakeAnim));
        }

        private void Ctrl2Pause()
        {
            nvAuido2Face.Pause();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayWelcome()
        {
            nvAuido2Face.SayWelcome();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayApiError()
        {
            nvAuido2Face.SayApiError();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayNoVoice()
        {
            nvAuido2Face.SayNoVoice();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayInAIReply()
        {
            nvAuido2Face.SayInAIReply();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void CtrlSayInAIImg()
        {
            nvAuido2Face.SayInAIImg();
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayWav(string wavName)
        {
            Thread.Sleep(200);
            nvAuido2Face.SayWav(wavName);
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.TakeAnim));
        }
        #endregion

        #region Audio2Face控制
        private void InitNvAudio2Face()
        {
            try
            {
                string serverAddress = configDataJObect["audio2Face"]["serverAddress"].ToString();
                string usdFile = configDataJObect["audio2Face"]["usdFile"].ToString();
                string wavFile = configDataJObect["audio2Face"]["wavFile"].ToString();

                nvAuido2Face = new NvAuido2Face(serverAddress, usdFile, wavFile);
                nvAuido2Face.SendMsgEvent += HandleOnSendMsg;

                ResultMsg resultMsg = nvAuido2Face.InitServer();

                if (resultMsg.StatusCode == StatusCode.SUCCESS)
                {
                    DebugMessage("Nv Audio2Face初始化设置成功，设置为在线状态");
                    SetAudio2FaceServerStatusLabel(true);
                }
                else
                {
                    DebugMessage("Nv Audio2Face初始化设置失败，设置为离线状态", MSG_TYPE.ERROR);
                    SetAudio2FaceServerStatusLabel(false);
                }
            }
            catch(Exception ex)
            {
                DebugMessage($"初始化设置Nv Auido2Face发生异常，异常信息:{ex.Message}", MSG_TYPE.ERROR);
            }
        }

        /// <summary>
        /// 设置Aduio2Face服务是否在线
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mSG_TYPE"></param>
        public void SetAudio2FaceServerStatusLabel(bool islive)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    DoSetAudio2FaceServerStatusLabel(islive);
                }));
            }
            else
            {
                DoSetAudio2FaceServerStatusLabel(islive);
            }
        }

        /// <summary>
        /// 执行设置展示端是否在线
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mSG_TYPE"></param>
        private void DoSetAudio2FaceServerStatusLabel(bool islive)
        {
            if (islive)
            {
                Audio2FaceSeverStatus_Label.Foreground = Brushes.Green;
                Audio2FaceSeverStatus_Label.Content = "在线";
            }
            else
            {
                Audio2FaceSeverStatus_Label.Foreground = Brushes.Red;
                Audio2FaceSeverStatus_Label.Content = "离线";
            }
        }
        #endregion

        #region 消息接收发送服务
        private bool InitUdpServer()
        {
            try
            {
                udpServerIP = configDataJObect["udpServer"]["ip"].ToString();
                udpServerPort = int.Parse(configDataJObect["udpServer"]["port"].ToString());

                DebugMessage("读取UDP Server设置信息：IP：" + udpServerIP + "，端口：" + udpServerPort);

                virtualHumanSeverIP = configDataJObect["virtualHumanSever"]["ip"].ToString();
                virtualHumanSeverPort = int.Parse(configDataJObect["virtualHumanSever"]["port"].ToString());
                DebugMessage("读取Virtual Human Sever设置信息：IP：" + virtualHumanSeverIP + "，端口：" + virtualHumanSeverPort);

                multiScreenPlayerPort = int.Parse(configDataJObect["multiScreenPlayer"]["port"].ToString());
                DebugMessage("读取Multi Screen Player 设置信息：端口：" + multiScreenPlayerPort);

                udpServerCtrl = new UdpServerCtrl(udpServerIP, udpServerPort, OnMessage, DebugMessage, ';');
                udpServerCtrl.Init();
            }
            catch (Exception ex)
            {
                DebugMessage("初始化设置UDP服务失败:" + ex.Message, MSG_TYPE.ERROR);
                return false;
            }

            return true;
        }

        private void OnMessage(object sender, string message)
        {
            string[] messageArray = message.Split('-');
            if (messageArray[0].Equals("HeartBeat"))
            {
                lastGetHeartbeatTime = DateTime.Now;
            }
            else if (messageArray[0].Equals("KnowledgeBase"))
            {
                if (messageArray[1] == "Play")
                {
                    Ctrl2Play();
                }
                else if (messageArray[1] == "Pause")
                {
                    Ctrl2Pause();
                }
                else if (messageArray[1].Contains("wav"))
                {
                    Ctrl2SayWav(messageArray[1]);
                }
                else
                {
                    bool isNumeric = int.TryParse(messageArray[1], out int number);
                    if(isNumeric)
                    {
                        IndustryQAChatReply industryQAChatReply = parthenonKnowledgeBase.GetIndustryQAChatReplyById(number);
                        if (industryQAChatReply != null)
                        {
                            DebugMessage("本地知识库Id匹配知识库命令，进行播放");

                            if (thread.ThreadState == ThreadState.Running)
                            {
                                try
                                {
                                    thread.Abort();
                                    DebugMessage("关闭正在运行的识别应答线程");
                                }
                                catch (Exception ex)
                                {
                                    DebugMessage($"关闭正在运行的识别应答线程出现异常，异常信息：{ex.Message}", MSG_TYPE.ERROR);
                                }
                            }

                            PlayLocalIndustryQAChatReply(industryQAChatReply);
                        }
                        else
                        {
                            DebugMessage("本地知识库Id无知识库命令，不进行播放");
                        }
                        
                    }
                    else
                    {
                        DebugMessage("输入的命令不符合所有知识库格式");
                    }
                }
            }
            else if (messageArray[0].Equals("Stream"))
            {
                string targetIP = messageArray[2];
                if (messageArray[1] == "Play")
                {
                    UdpSendMessage2MultiScreenPlayer(targetIP, GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Play));

                    if (!string.IsNullOrEmpty(multiScreenPlayerIP) && multiScreenPlayerIP != targetIP)
                    {
                        UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Stop));
                    }

                    multiScreenPlayerIP = targetIP;
                }
                else if (messageArray[1] == "Stop")
                {
                    UdpSendMessage2MultiScreenPlayer(targetIP, GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Stop));
                    if (!string.IsNullOrEmpty(multiScreenPlayerIP) && multiScreenPlayerIP != targetIP)
                    {
                        UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Stop));
                    }

                    multiScreenPlayerIP = "";
                }
            }
            else if (messageArray[0] == "Position")
            {
                if (!string.IsNullOrEmpty(multiScreenPlayerIP))
                {
                    UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, MultiScreenPlayerCmdUtils.GetMultiScreenPlayerCtrlPositionStr(int.Parse(messageArray[1]), int.Parse(messageArray[2])));
                }
            }
            else if (messageArray[0] == "Size")
            {
                if (!string.IsNullOrEmpty(multiScreenPlayerIP))
                {
                    UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, MultiScreenPlayerCmdUtils.GetMultiScreenPlayerCtrlSizeStr(int.Parse(messageArray[1]), int.Parse(messageArray[2])));
                }
            }
        }

        private void UdpSendMessage2VirtualHumanSever(string jsonStr)
        {
            //DebugMessage("UDP服务发送VirtualHumanSever消息，地址：" + virtualHumanSeverIP + ":" + virtualHumanSeverPort + " 发送消息" + jsonStr);
            udpServerCtrl.Send(virtualHumanSeverIP, virtualHumanSeverPort, jsonStr);
        }

        private void UdpSendMessage2MultiScreenPlayer(string ip, string jsonStr)
        {
            DebugMessage("UDP服务发送MultiScreenPlayer消息，地址：" + ip + ":" + multiScreenPlayerPort + " 发送消息" + jsonStr);
            udpServerCtrl.Send(ip, multiScreenPlayerPort, jsonStr);
        }
        #endregion

        #region 和虚拟人端进行检测，控制端每10s发送一次心跳包消息，让虚拟人端进行判断是否在线，本地也定时根据接受到展示端发送来的消息，进行判断是否在线
        private void StartCheckVirtualPersonTimer()
        {
            DebugMessage(string.Format("初始化运行显示端连接检测定时器"));

            checkVirtualPersonTimer = new DispatcherTimer();
            checkVirtualPersonTimer.Tick += new EventHandler(CheckVirtualPersonTimerTick);
            checkVirtualPersonTimer.Interval = new TimeSpan(0, 0, 10);
            checkVirtualPersonTimer.Start();
        }

        private void CheckVirtualPersonTimerTick(object sender, EventArgs e)
        {
            UdpSendMessage2VirtualHumanSever(GetVirtualPersonCtrlStr(VirtualPersonCtrlType.HeartBeat));

            bool flag = true;

            if ((DateTime.Now - lastGetHeartbeatTime).TotalSeconds > 10) // 当前时间和上次获取到展示端发送的消息时间大于5s，则添加一次检测次数，当检测次数大于5次时则认为离线
            {
                checkVirtualPersonNumber++;

                if (checkVirtualPersonNumber > 5) // 当检测次数大于5次时则认为离线
                {
                    flag = false;
                }
            }
            else
            {
                checkVirtualPersonNumber = 0;

                flag = true;
            }

            if (flag != isVirtualPersonLive)
            {
                SetVirtualPersonStatusLabel(flag);
                isVirtualPersonLive = flag;
            }
        }

        /// <summary>
        /// 设置展示端是否在线
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mSG_TYPE"></param>
        public void SetVirtualPersonStatusLabel(bool islive)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    DoSetVirtualPersonStatusLabel(islive);
                }));
            }
            else
            {
                DoSetVirtualPersonStatusLabel(islive);
            }
        }

        /// <summary>
        /// 执行设置展示端是否在线
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mSG_TYPE"></param>
        private void DoSetVirtualPersonStatusLabel(bool islive)
        {
            if (islive)
            {
                VirtualPersionStatus_Label.Foreground = Brushes.Green;
                VirtualPersionStatus_Label.Content = "在线";
            }
            else
            {
                VirtualPersionStatus_Label.Foreground = Brushes.Red;
                VirtualPersionStatus_Label.Content = "离线";
            }
        }
        #endregion

        #region 加载配置文件中配置信息
        private void LoadConfigData()
        {
            string jsonfile = Path.Combine(Thread.GetDomain().BaseDirectory, "Config", "Config.json");//配置文件路径
            try
            {
                using (StreamReader file = File.OpenText(jsonfile))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        configDataJObect = (JObject)JToken.ReadFrom(reader);
                    }
                }
            }
            catch(Exception ex)
            {
                DebugMessage(string.Format("读取配置文件{0}出现异常，异常信息：{1}", jsonfile, ex.Message), MSG_TYPE.ERROR);
            }
        }
        #endregion

        #region 日志和信息
        /// <summary>
        /// 初始化Log4net和Notifier
        /// </summary>
        private void InitLog4net()
        {
            // 初始化设置Log4net
            log4net.Config.XmlConfigurator.Configure();

            //Log_ListView.ScrollChanged += (sender, e) =>
            //{
            //    // 如果滚动到了底部，就自动向下滚动
            //    if (e.VerticalOffset == listBox.ScrollableHeight)
            //    {
            //        listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
            //    }
            //};
        }

        private void DebugMessage(string msg, MSG_TYPE msgType = MSG_TYPE.INFO)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString());
            sb.Append(":  ");

            if (msgType == MSG_TYPE.INFO)
            {
                sb.Append("INFO:  ");
                log.Info(msg);
            }
            else if (msgType == MSG_TYPE.WARNNING)
            {
                sb.Append("WARNNING:  ");
                log.Warn(msg);
            }
            else if (msgType == MSG_TYPE.ERROR)
            {
                sb.Append("ERROR:  ");
                log.Error(msg);
            }

            sb.Append(msg);
            SetListBox(sb.ToString(), msgType);
        }

        /// <summary>
        /// 输出日志信息到窗体和日志文件和弹窗
        /// </summary>
        /// <param name="msg">日志信息</param>
        /// <param name="msgType">日志类型</param>
        /// <param name="isShowNotifier">是否显示日志弹窗</param>
        private void HandleOnSendMsg(ResultMsg resultMsg)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString());
            sb.Append(":  ");

            if (resultMsg.StatusCode == StatusCode.SUCCESS)
            {
                log.Info(resultMsg.Data.ToString());
                sb.Append("INFO:  ");
                sb.Append(resultMsg.Data.ToString());
                SetListBox(sb.ToString(), MSG_TYPE.INFO);
            }
            else if (resultMsg.StatusCode == StatusCode.INFO)
            {
                log.Info(resultMsg.Msg);
                sb.Append("INFO:  ");
                sb.Append(resultMsg.Msg);
                SetListBox(sb.ToString(), MSG_TYPE.INFO);
            }
            else if (resultMsg.StatusCode == StatusCode.ERROR)
            {
                log.Error(resultMsg.Msg);
                sb.Append("ERROR:  ");
                sb.Append(resultMsg.Msg);
                SetListBox(sb.ToString(), MSG_TYPE.ERROR);
            }
        }

        /// <summary>
        /// 设置日志信息到窗体
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mSG_TYPE"></param>
        public void SetListBox(string msg, MSG_TYPE msgType)
        {
            if ( string.IsNullOrEmpty(msg))
                return;
            
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    DoSetListBox(msg, msgType);
                }));
            }
            else
            {
                DoSetListBox(msg, msgType);
            }
        }

        /// <summary>
        /// 执行日志信息到窗体
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="msgType"></param>
        private void DoSetListBox(string msg, MSG_TYPE msgType)
        {
            TextBlock tb = new TextBlock();
            tb.FontWeight = FontWeights.Normal;
            tb.Typography.NumeralStyle = FontNumeralStyle.OldStyle;
            tb.Typography.SlashedZero = true;
            tb.Text = msg;
            tb.TextWrapping = TextWrapping.Wrap;

            if (msgType == MSG_TYPE.INFO) 
            {
                tb.Foreground = Brushes.Green;
            }
            else if (msgType == MSG_TYPE.WARNNING)
            {
                tb.Foreground = Brushes.Yellow;
            }
            else if (msgType == MSG_TYPE.ERROR)
            {
                tb.Foreground = Brushes.Red;
            }
            Log_ListView.Items.Add(tb);
            Log_ListView.ScrollIntoView(tb);
        }
        #endregion
    }
}
