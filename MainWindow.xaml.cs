using log4net;
using MessageCtrl;
using MiniExcelLibs;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using VitualPersonSpeech.Model;
using VitualPersonSpeech.Tasks;
using VitualPersonSpeech.Utils;
using VoiceRecorder.Audio;
using WebSocket4Net;

namespace VitualPersonSpeech
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 打印日志
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Notifier notifier;

        // 配置信息
        private JObject configJObect;

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

        // 当前识别模式
        enum AsrMode
        {
            RestfulApi = 0,
            WebSocket = 1
        }
        private AsrMode asrMode = AsrMode.WebSocket;

        // websocket识别接口：百度、讯飞
        enum AsrApi
        {
            BaiduAsr = 0,
            XunFeiAsr = 1
        }
        private AsrApi asrApi = AsrApi.XunFeiAsr;

        // 百度语音识别api信息
        private const int baiduAppid = 36012261;
        private const string baiduApiKey = "iQxBR94puMGVjLSOsUfghba5";
        private const string baiduApiSecret = "oEgvDNFTz9huud3Xb22wdeEaojLYw8hW";
        // 讯飞语音识别api信息
        private const string xunfeiAppid = "a30a26fc";
        private const string xunfeiApiKey = "4c9014586f0ca11682e6f218293a0867";
        private const string xunfeiApiSecret = "MjFlNzE3ZGYyOTQxNzMzMDgwODNkYjRi";
        private string xunfeiCurAsrStr = ""; // 讯飞当前句识别asr，因为pgs会有rpl和apd区别
        private List<string> xunfeiAsrStrList = new List<string>();

        // 本地知识库
        private LocalIndustryQAData[] industryQADatas;
        private Dictionary<int, IndustryQAChatReply> industryQAChatReplyDic = new Dictionary<int, IndustryQAChatReply>();
        private string industryQAUserName = "1";
        private string industryQAPassword = "1";
        private string industryQAUserAccessToken = "";
        private string industryQAGetTokenAPIUrl = "http://192.168.3.99:8082/api/get-token?username={0}&password={1}";
        private string industryQAGetAllReplysAndScenesAPIUrl = "http://192.168.3.197:8082/chat-reply/get-all-replys-and-scenes?token={0}";
        private string industryQASimilarAPIUrl = "http://192.168.3.99:8082/chat-quest/answer-quest?content={0}&token={1}";
        private string industryQADownloadFileAPIUrl = "http://192.168.3.197:9000/{0}";
        private float industryQARelevanceThreshold = 0.1f;
        private float industryQASimilarityThreshold = 0.1f;
        private string industryQALocalFileDictionary = "C:\\wav\\";

        // AI应答
        // 百度文心一言接口appkey和secret
        private const int wenxinAppid = 35271065;
        private const string wenxinApiKey = "yfHH6GDV8g8ETcDyFKHMIbfN";
        private const string wenxinApiSecret = "NGKZeanmHF41q0yPtjufqyqWx3ma3cNK";
        private string wenxinAccessToken = "";
        private List<WenXinMessage> wenXinMessages = new List<WenXinMessage>();

        // 百度语音合成
        private const int baiduTTSAppid = 36012261;
        private const string baiduTTSAPIKey = "iQxBR94puMGVjLSOsUfghba5";
        private const string baiduTTSAPISecret = "oEgvDNFTz9huud3Xb22wdeEaojLYw8hW";
        private string baiduTTSAccessToken = "";// 百度语音文本在线合成accesstoken
        private string baiduLongTTSTaskId = "";
        private DispatcherTimer getBaiduLongTTSTimer;

        // 文件下载器
        private DownloadTask downloadTask;
        private ThreadWorker downloader;

        // AI作画
        // 百度ai作画appkey
        private List<string> qaAIImgPatternList = new List<string> // ai作画正则匹配规则：做***图 作***图 画***图    做***画 作***画 画***画    做一张 作一张 画一张    做一幅 作一幅 画一幅
        {
             ".*做.*图.*", ".*作.*图.*", ".*画.*图.*",
             ".*做.*画.*", ".*作.*画.*", ".*画.*画.*",
             ".*做一张.*", ".*作一张.*", ".*画一张.*",
             ".*做一幅.*", ".*作一幅.*", ".*画一幅.*",
        };
        private const int baiduAIImgAppid = 36763656;
        private const string baiduAIImgApiKey = "0YSOMT5OGK1GtjWc3lsg1GML";
        private const string baiduAIImgApiSecret = "XkowHDaRjCULMTvdgdzoIXP543GYahwB";
        private string baiduAIImgAccessToken = "";// baidu AI作画AccessToken
        private int baiduAIImgWidth = 1024;
        private int baiduAIImgHeight = 1024;
        private string baiduAIImgTaskId = "";
        private DispatcherTimer getBaiduAIImgTimer;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region 界面相关事件
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitLog4netAndNotifier();

            DebugMessage("虚拟数字人语音识别模块运行");

            LoadConfig();
            
            InitUdpServer();

            StartCheckVirtualPersonTimer();

            InitWaveIn();

            StartWaveInRecording();

            InitAudioRecorder();

            StartWakeOn();

            InitDownloader();

            GetindustryQADataSetting();

            GetIndustryQAAccessToken();
            //GetIndustryQADataByExcel();

            DownloadAllIndustryFile();

            GetQAIsAIImgPattern();

            GetBaiduAIImgAccessToken();

            InitGetAIImgTimer();

            GetWenXinAccessToken();

            GetBaiduLongTTSAccessToken();

            InitGetLongTTSTimer();

            InitAudio2FaceServer();

            StartHttpServerService();
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

            if (asrApi == AsrApi.BaiduAsr)
            {
                Dictionary<string, object> options = new Dictionary<string, object>() { };
                options.Add("dev_pid", 1936);

                BaiduAsrResult baiduAsrResult = BaiduAIUtils.Instance.GetAsr().Recognize(wavBytes, "wav", 16000, options).ToObject<BaiduAsrResult>();
                DebugMessage(string.Format("百度识别语音结果为{0}", baiduAsrResult.ToString()));
            }
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
            AIReply(asrStr);
        }

        private void Audio2FaceLoadUSD_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Audio2FaceUtils.LoadAudio2FaceUSD(configJObect["audio2Face"]["usdFile"].ToString()))
            {
                DebugMessage("Audio2Face服务加载 " + configJObect["audio2Face"]["usdFile"].ToString() + " 成功", MSG_TYPE.INFO);
            }
            else
            {
                DebugMessage("Audio2Face服务加载 " + configJObect["audio2Face"]["usdFile"].ToString() + " 失败", MSG_TYPE.ERROR);
            }
        }

        private void Audio2FaceLoadDefaultWav_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Audio2FaceUtils.SetAudio2FacePlayerTrack(configJObect["audio2Face"]["wavFile"].ToString()))
            {
                DebugMessage("Audio2Face服务加载 " + configJObect["audio2Face"]["wavFile"].ToString() + " 成功", MSG_TYPE.INFO);
            }
            else
            {
                DebugMessage("Audio2Face服务加载 " + configJObect["audio2Face"]["wavFile"].ToString() + " 失败", MSG_TYPE.ERROR);
            }  
        }

        private void Audio2FacePlayWav_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Audio2FaceUtils.ControlAudio2FacePlayerPlay())
            {
                DebugMessage("Audio2Face服务播放成功", MSG_TYPE.INFO);
            }
            else
            {
                DebugMessage("Audio2Face服务播放失败", MSG_TYPE.ERROR);
            }
        }

        private void Audio2FacePauseWav_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Audio2FaceUtils.ControlAudio2FacePlayerPause())
            {
                DebugMessage("Audio2Face服务暂停成功", MSG_TYPE.INFO);
            }
            else
            {
                DebugMessage("Audio2Face服务暂停失败", MSG_TYPE.ERROR);
            }
        }

        private void Audio2FaceActicateStreamLivelink_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Audio2FaceUtils.ActivateAudio2FaceExporterStreamLiveLink())
            {
                DebugMessage("Audio2Face服务建立LveLink连接成功", MSG_TYPE.INFO);
            }
            else
            {
                DebugMessage("Audio2Face服务建立LveLink连接失败", MSG_TYPE.INFO);
            }
        }
        #endregion

        #region http服务
        private void StartHttpServerService()
        {
            try
            {
                string httpServerIP = configJObect["httpServer"]["ip"].ToString();
                int httpServerPort = int.Parse(configJObect["httpServer"]["port"].ToString());

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

            if (asrMode == AsrMode.RestfulApi)
            {
                wavBytes = new byte[0];// 清空已有录音数据

                if (isSaveWaveFile)
                {
                    string directoryName = System.IO.Path.Combine(Thread.GetDomain().BaseDirectory, "Wavs", DateTime.Now.ToString("yyyy-MM-dd"));
                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    string waveFileName = System.IO.Path.Combine(directoryName, string.Format("{0}{1}", DateTime.Now.ToString("HH_mm_dd"), ".wav"));
                    waveFileWriter = new WaveFileWriter(waveFileName, waveIn.WaveFormat);
                }
            }
            else if (asrMode == AsrMode.WebSocket)
            {
                status = Status.NoFrame;
            }

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
            if (isStartIVW) // 是否打开语音识别
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
            /*
             if (asrMode == AsrMode.RestfulApi) // 使用restful进行语音识别，倒计时关闭识别
            {
                wavBytes = CommonUtils.CombineBytes(wavBytes, e.Buffer);

                if (waveFileWriter != null)// 写入录音数据
                {
                    waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
                    waveFileWriter.Flush();
                }
            }
            else if (asrMode == AsrMode.WebSocket)// 使用websocket进行流式语音识别
            {
                if (asrApi == AsrApi.XunFeiAsr)// 讯飞流式语音识别
                {
                    switch (status)
                    {
                        case Status.FirstFrame: // 握手
                            {
                                dynamic frame = new JObject();
                                frame.common = new JObject
                                {
                                    { "app_id",xunfeiAppid},
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
                            break;
                        case Status.ContinueFrame://开始发送
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
                            break;
                        case Status.LastFrame://关闭
                            {
                                dynamic frame = new JObject();
                                frame.data = new JObject
                                {
                                    { "status", 2 }
                                };
                                webSocket.Send(frame.ToString());
                                StopWaveInRecording();
                            }
                            break;
                        default:
                            break;
                    }
                }
                else if (asrApi == AsrApi.BaiduAIUtils)// 百度流式语音识别
                {
                    switch (status)
                    {
                        case Status.FirstFrame: // 握手
                            {
                                dynamic frame = new JObject();
                                frame.type = "START";
                                frame.data = new JObject
                                {
                                    { "appid",baiduAppid},
                                    { "appkey", baiduApiKey},
                                    { "dev_pid",15372},
                                    { "cuid","cuid-1"},
                                    { "format","pcm"},
                                    { "sample",16000},
                                };
                                webSocket.Send(frame.ToString());
                                status = Status.ContinueFrame;
                            }
                            break;
                        case Status.ContinueFrame://开始发送
                            {
                                webSocket.Send(e.Buffer, 0, e.Buffer.Length);
                            }
                            break;
                        case Status.LastFrame://关闭
                            {
                                dynamic frame = new JObject();
                                frame.type = "FINISH";
                                webSocket.Send(frame.ToString());
                                StopWaveInRecording();
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
             */
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
                DebugMessage("录音出现异常:" + e.Exception.Message, MSG_TYPE.ERROR, true);
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

            BaiduAsrResult baiduAsrResult = BaiduAIUtils.Instance.GetAsr().Recognize(wavBytes, "wav", 16000).ToObject<BaiduAsrResult>();
            DebugMessage(baiduAsrResult.result[0]);
        }
        #endregion

        #region websocket语音识别
        /// <summary>
        /// 打开流式语音识别websocket连接
        /// </summary>
        private void OpenAsrWebSocket()
        {
            string websocketUrl = "";
            if (asrApi == AsrApi.XunFeiAsr)// 建立讯飞实时识别WebSocket的地址
            {
                string apiAddress = "wss://iat-api.xfyun.cn/v2/iat";
                string host = "wss://iat-api.xfyun.cn";
                string dateTimeStr = DateTime.UtcNow.ToString("r");
                string signatureOrigin = string.Format("host: {0}\ndate: {1}\nGET /v2/iat HTTP/1.1", host, dateTimeStr);
                string signature = EncryptUtils.HMACSHA256Text(signatureOrigin, xunfeiApiSecret);
                string authorizationOrigin = string.Format("api_key=\"{0}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{1}\"", xunfeiApiKey, signature);
                string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationOrigin));

                websocketUrl = string.Format("{0}?authorization={1}&date={2}&host={3}", apiAddress, authorization, dateTimeStr, host);
            }
            else if (asrApi == AsrApi.BaiduAsr)// 建立百度实时识别WebSocket的地址
            {
                websocketUrl = string.Format("wss://vop.baidu.com/realtime_asr?sn={0}", Guid.NewGuid().ToString());
            }

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
            if (asrApi == AsrApi.XunFeiAsr)
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
                            if (MatchQAIsAIImg(asrStr)) // 先判断是作画还是应答
                            {
                                DebugMessage("语句与AI作画正则匹配成功，进行AI作画");
                                BaiduAIImgTxt2Img(asrStr);
                            }
                            else
                            {
                                DebugMessage("语句与AI作画正则匹配不成功，进行行业知识库问答");

                                IndustryQASimilarResult industryQAData = GetSimilarIndustryQAData(asrStr);

                                if (industryQAData != null)
                                {
                                    DebugMessage(string.Format("行业知识库匹配完成，ES权重：{0}，NLP相似度：{1}，原问题：{2}，匹配问题：{3}，匹配回答：{4}", industryQAData.relevance, industryQAData.similarity, industryQAData.questStr, industryQAData.similarStr, industryQAData.industryQAChatReply.replyContent));

                                    if (industryQAData.relevance > industryQARelevanceThreshold && industryQAData.similarity > industryQASimilarityThreshold)
                                    {
                                        DebugMessage("行业知识库匹配结果满足权重阈值和相似度阈值，使用行业知识库回答进行应答");

                                        if (string.IsNullOrEmpty(industryQAData.industryQAChatReply.replyContent))
                                        {
                                            string fileFullName = BaiduShortTTS(industryQAData.industryQAChatReply.replyContent);

                                            if (string.IsNullOrEmpty(fileFullName))
                                            {
                                                DebugMessage("百度短文本转语音出现错误，数字人响应api错误");
                                                Ctrl2SayApiError();
                                            }
                                            else
                                            {
                                                DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                                                Ctrl2SayWav(Path.GetFileName(fileFullName));
                                            }
                                        }
                                        else
                                        {
                                            if (File.Exists(Path.Combine(configJObect["industryQADataSetting"]["localFileDictionary"].ToString(), Path.GetFileName(industryQAData.industryQAChatReply.replyContent))))
                                            {
                                                Ctrl2SayWav(Path.GetFileName(industryQAData.industryQAChatReply.replyContent));
                                            }
                                            else
                                            {
                                                string fileFullName = BaiduShortTTS(industryQAData.industryQAChatReply.replyContent);

                                                if (string.IsNullOrEmpty(fileFullName))
                                                {
                                                    DebugMessage("百度短文本转语音出现错误，数字人响应api错误");
                                                    Ctrl2SayApiError();
                                                }
                                                else
                                                {
                                                    DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                                                    Ctrl2SayWav(Path.GetFileName(fileFullName));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        DebugMessage("行业知识库匹配结果不满足相似度阈值或权重，进行百度文心AI应答");
                                        string asrResult = ChatWenXin(asrStr);

                                        if (string.IsNullOrEmpty(asrResult))
                                        {
                                            DebugMessage("调用文心ai应答接口出现错误，数字人响应api错误");
                                            Ctrl2SayApiError();
                                        }
                                        else
                                        {
                                            // 合成的文本，文本长度必须小于1024GBK字节。建议每次请求文本不超过120字节，约为60个汉字或者字母数字。
                                            // 请注意计费统计依据：120个GBK字节以内（含120个）记为1次计费调用；每超过120个GBK字节则多记1次计费调用。
                                            if (asrResult.Length <= 500)
                                            {
                                                DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，小于等于500，采用短文本转语音", asrResult.Length));

                                                string fileFullName = BaiduShortTTS(asrResult);

                                                if (string.IsNullOrEmpty(fileFullName))
                                                {
                                                    DebugMessage("百度短文本转语音出现错误，数字人响应api错误");
                                                    Ctrl2SayApiError();
                                                }
                                                else
                                                {
                                                    DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                                                    Ctrl2SayWav(Path.GetFileName(fileFullName));
                                                }
                                            }
                                            else
                                            {
                                                DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，大于500，采用长文本转语音", asrResult.Length));
                                                CreateBaiduLongTTS(asrResult);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    DebugMessage("行业知识库未匹配到相关问答，进行百度文心AI应答");
                                    string asrResult = ChatWenXin(asrStr);

                                    if (string.IsNullOrEmpty(asrResult))
                                    {
                                        DebugMessage("调用文心ai应答接口出现错误，数字人响应api错误");
                                        Ctrl2SayApiError();
                                    }
                                    else
                                    {
                                        // 合成的文本，文本长度必须小于1024GBK字节。建议每次请求文本不超过120字节，约为60个汉字或者字母数字。
                                        // 请注意计费统计依据：120个GBK字节以内（含120个）记为1次计费调用；每超过120个GBK字节则多记1次计费调用。
                                        if (asrResult.Length <= 500)
                                        {
                                            DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，小于等于500，采用短文本转语音", asrResult.Length));

                                            string fileFullName = BaiduShortTTS(asrResult);

                                            if (string.IsNullOrEmpty(fileFullName))
                                            {
                                                DebugMessage("百度短文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                                                Ctrl2SayApiError();
                                            }
                                            else
                                            {
                                                DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                                                Ctrl2SayWav(Path.GetFileName(fileFullName));
                                            }
                                        }
                                        else
                                        {
                                            DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，大于500，采用长文本转语音", asrResult.Length));
                                            CreateBaiduLongTTS(asrResult);
                                        }
                                    }
                                }
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
            else if (asrApi == AsrApi.BaiduAsr)
            {
                //返回的JSON串"{\"err_msg\":\"OK\",\"err_no\":0,\"log_id\":1542502511,\"result\":\"你好\",\"sn\":\"c36964c8-d86c-4a06-b7cf-8939539d3eb8_ws_1\",\"type\":\"MID_TEXT\"}\n"
                DebugMessage(string.Format("接收到百度WebSocket服务端发送的信息:{0}", e.Message));

                dynamic msg = JsonConvert.DeserializeObject(e.Message);

                if (msg.err_no != null && msg.err_no != 0)
                {
                    DebugMessage(string.Format("识别百度WebSocket服务端发送为错误信息，错误码err_no:{0}, 错误信息err_msg:{1}", msg.err_no, msg.message), MSG_TYPE.ERROR);
                    return;
                }

                if (msg.type == "MID_TEXT")
                {
                    DebugMessage(string.Format("接收到百度WebSocket识别为临时识别结果<MID_TEXT>的消息：{0}", msg.result));
                }
                else if (msg.type == "FIN_TEXT")
                {
                    DebugMessage(string.Format("接收到百度WebSocket识别为最终识别结果<FIN_TEXT>的消息：{0}", msg.result));

                    asrStr = msg.result;

                    DebugMessage(string.Format("当前百度WebSocket识别语音为：", asrStr));
                    status = Status.LastFrame;
                }
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
            DebugMessage("WebSocket出现错误，错误信息：" + e.Exception.Message, MSG_TYPE.ERROR, true);
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

                DebugMessage("数字人响应欢迎词");
                Ctrl2SayWelcome();

                DebugMessage("800ms后开始流式语音识别");
                Thread.Sleep(800);
                OpenAsrWebSocket();
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
                    DebugMessage(string.Format("语音唤醒输入语音数据出现异常,异常信息：{0}", ex.Message), MSG_TYPE.ERROR, true);
                    //XunFeiUtils.MSPLogout();
                    //break;
                }
                //errcode = MSCDLL.QIVWAudioWrite(PtrToStr(session_id), VoiceBuffer[i].data, (uint)VoiceBuffer[i].data.Length, aud_stat);                                
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

        #region 应答
        public async void AIReply(string asrStr)
        {
            IndustryQASimilarResult industryQASimilarResult = GetSimilarIndustryQAData(asrStr);

            if (industryQASimilarResult != null)
            {
                DebugMessage(string.Format("行业知识库匹配完成，ES权重：{0}，NLP相似度：{1}，原问题：{2}，匹配问题：{3}，匹配回答：{4}", industryQASimilarResult.relevance, industryQASimilarResult.similarity, industryQASimilarResult.questStr, industryQASimilarResult.similarStr, industryQASimilarResult.industryQAChatReply.replyContent));

                if (industryQASimilarResult.relevance > industryQARelevanceThreshold && industryQASimilarResult.similarity > industryQASimilarityThreshold)
                {
                    DebugMessage("行业知识库匹配结果满足权重阈值和相似度阈值，使用行业知识库回答进行应答");

                    // 如果本地有文件则播放本地文件
                    if (File.Exists(industryQALocalFileDictionary + Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource)))
                    {
                        Ctrl2SayWav(Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource));
                        return;
                    }

                    // 本地没有则下载音频
                    // 查看音频链接地址，没有就百度文字转语音，有就下载
                    if (string.IsNullOrEmpty(industryQASimilarResult.industryQAChatReply.audioFileSource))
                    {
                        DebugMessage("接口返回信息没有音频链接地址，进行文字转语音");
                        string fileFullName = BaiduShortTTS(industryQASimilarResult.industryQAChatReply.replyContent);

                        if (string.IsNullOrEmpty(fileFullName))
                        {
                            DebugMessage("百度短文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                            Ctrl2SayApiError();
                        }
                        else
                        {
                            DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                            Ctrl2SayWav(Path.GetFileName(fileFullName));
                        }
                    }
                    else
                    {
                        if(File.Exists(industryQALocalFileDictionary + Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource)))
                        {
                            DebugMessage("本地已有回答音频文件，数字人开始说话:" + Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource));
                            Ctrl2SayWav(Path.GetFileName(Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource)));
                            return;
                        }

                        DebugMessage("本地没有回答音频文件，下载音频文件");

                        ResultMsg resultMsg = await HttpUtils.DownloadFileAsync(string.Format(industryQADownloadFileAPIUrl, industryQASimilarResult.industryQAChatReply.audioFileSource), industryQALocalFileDictionary + Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource));

                        if (resultMsg.msgType == MSG_TYPE.ERROR)// 文件下载出错
                        {
                            DebugMessage("回答音频文件下载出错，开始文字转音频");

                            string fileFullName = BaiduShortTTS(industryQASimilarResult.industryQAChatReply.replyContent);

                            if (string.IsNullOrEmpty(fileFullName))
                            {
                                DebugMessage("百度短文本转语音出现错误，数字人响应api错误");
                                Ctrl2SayApiError();
                            }
                            else
                            {
                                DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                                Ctrl2SayWav(Path.GetFileName(fileFullName));
                            }
                        }
                        else// 文件下载完成
                        {
                            DebugMessage("回答音频文件下载完成，进行播放");
                            Ctrl2SayWav(Path.GetFileName(Path.GetFileName(industryQASimilarResult.industryQAChatReply.audioFileSource)));
                        }
                    }
                }
                else
                {
                    DebugMessage("行业知识库匹配结果不满足相似度阈值或权重，进行百度文心AI应答");
                    string asrResult = ChatWenXin(asrStr);

                    if (string.IsNullOrEmpty(asrResult))
                    {
                        DebugMessage("调用文心ai应答接口出现错误，数字人响应api错误");
                        Ctrl2SayApiError();
                    }
                    else
                    {
                        // 合成的文本，文本长度必须小于1024GBK字节。建议每次请求文本不超过120字节，约为60个汉字或者字母数字。
                        // 请注意计费统计依据：120个GBK字节以内（含120个）记为1次计费调用；每超过120个GBK字节则多记1次计费调用。
                        if (asrResult.Length <= 500)
                        {
                            DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，小于等于500，采用短文本转语音", asrResult.Length));

                            string fileFullName = BaiduShortTTS(asrResult);

                            if (string.IsNullOrEmpty(fileFullName))
                            {
                                DebugMessage("百度短文本转语音出现错误，数字人响应api错误");
                                Ctrl2SayApiError();
                            }
                            else
                            {
                                DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                                Ctrl2SayWav(Path.GetFileName(fileFullName));
                            }
                        }
                        else
                        {
                            DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，大于500，采用长文本转语音", asrResult.Length));
                            CreateBaiduLongTTS(asrResult);
                        }
                    }
                }
            }
            else
            {
                DebugMessage("行业知识库未匹配到相关问答，进行百度文心AI应答");
                string asrResult = ChatWenXin(asrStr);

                if (string.IsNullOrEmpty(asrResult))
                {
                    DebugMessage("调用文心ai应答接口出现错误，数字人响应api错误");
                    Ctrl2SayApiError();
                }
                else
                {
                    // 合成的文本，文本长度必须小于1024GBK字节。建议每次请求文本不超过120字节，约为60个汉字或者字母数字。
                    // 请注意计费统计依据：120个GBK字节以内（含120个）记为1次计费调用；每超过120个GBK字节则多记1次计费调用。
                    if (asrResult.Length <= 500)
                    {
                        DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，小于等于500，采用短文本转语音", asrResult.Length));

                        string fileFullName = BaiduShortTTS(asrResult);

                        if (string.IsNullOrEmpty(fileFullName))
                        {
                            DebugMessage("百度短文本转语音出现错误，数字人响应api错误", MSG_TYPE.ERROR);
                            Ctrl2SayApiError();
                        }
                        else
                        {
                            DebugMessage("百度短文本转语音完成，数字人开始说话:" + Path.GetFileName(fileFullName));
                            Ctrl2SayWav(Path.GetFileName(fileFullName));
                        }
                    }
                    else
                    {
                        DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，大于500，采用长文本转语音", asrResult.Length));
                        CreateBaiduLongTTS(asrResult);
                    }
                }
            }
        }
        #endregion

        #region 行业知识库问答
        public void GetIndustryQADataByExcel()
        {
            string path = System.IO.Path.Combine(Thread.GetDomain().BaseDirectory, "Data", "行业知识库.xlsx");//配置文件路径
            try
            {
                industryQADatas = MiniExcel.Query<LocalIndustryQAData>(path, sheetName: "行业知识库").ToArray();

                DebugMessage(string.Format("加载本地行业知识库文件{0}成功，共有信息{1}条", path, industryQADatas.Length));
            }
            catch(Exception ex)
            {
                DebugMessage(string.Format("加载本地行业知识库文件{0}失败,失败原因：{1}", path, ex.Message));
            }
        }

        public LocalIndustryQAData MatchLocalIndustryQAData(string questionStr)
        {
            for(int i = 0; i < industryQADatas.Length; i++)
            {
                bool isMatch = industryQADatas[i].MatchQA(questionStr);
                if (isMatch)
                {
                    return industryQADatas[i];
                }
            }
            return null;
        }

        public void GetindustryQADataSetting()
        {
            try
            {
                industryQAGetTokenAPIUrl = configJObect["industryQADataSetting"]["getTokenAPIUrl"].ToString();
                industryQAGetAllReplysAndScenesAPIUrl = configJObect["industryQADataSetting"]["getAllReplysAndScenesAPIUrl"].ToString();
                industryQASimilarAPIUrl = configJObect["industryQADataSetting"]["similarAPIUrl"].ToString();
                industryQADownloadFileAPIUrl = configJObect["industryQADataSetting"]["downloadFileAPIUrl"].ToString();
                
                industryQARelevanceThreshold = (float)configJObect["industryQADataSetting"]["relevanceThreshold"];
                industryQASimilarityThreshold = (float)configJObect["industryQADataSetting"]["similarityThreshold"];
                industryQALocalFileDictionary = configJObect["industryQADataSetting"]["localFileDictionary"].ToString();
                DebugMessage(string.Format("获取行业知识库相关设置完成，API地址：{0}，权重阈值：{1}，相似度阈值：{2}", industryQASimilarAPIUrl, industryQARelevanceThreshold, industryQASimilarityThreshold));
            }
            catch(Exception ex)
            {
                DebugMessage(string.Format("获取行业知识库相关设置失败，失败原因：{1}，使用默认设置，API地址：{1}，权重阈值：{2}，相似度阈值：{3}", ex.Message, industryQASimilarAPIUrl, industryQARelevanceThreshold, industryQASimilarityThreshold), MSG_TYPE.ERROR);
            }
        }

        public void GetIndustryQAAccessToken()
        {
            string getIndustryQAAccessTokenUrl = string.Format(industryQAGetTokenAPIUrl, industryQAUserName, industryQAPassword);

            string result = HttpUtils.Post(getIndustryQAAccessTokenUrl);
            DebugMessage("获取行业知识库 AccessToken请求，请求结果：" + result);

            JObject resultJObject = JObject.Parse(result);

            if (resultJObject != null && resultJObject.ContainsKey("code"))
            {
                if (resultJObject["code"].ToString() == "SUCCESS")
                {
                    DebugMessage("获取行业知识库 AccessToken请求成功，AccessToken：" + resultJObject["data"].ToString());
                    industryQAUserAccessToken = resultJObject["data"].ToString();
                }
            }
            else
            {
                DebugMessage("获取行业知识库 AccessToken请求错误，返回结果不含code", MSG_TYPE.ERROR);
            }
        }

        public void DownloadAllIndustryFile()
        {
            string getIndustryQAGetAllReplysAPIUrl = string.Format(industryQAGetAllReplysAndScenesAPIUrl, industryQAUserAccessToken);

            string result = HttpUtils.Post(getIndustryQAGetAllReplysAPIUrl);
            DebugMessage("获取行业问答库所有请求和回答，请求结果：" + result);

            JObject resultJObject = JObject.Parse(result);

            if (resultJObject != null && resultJObject.ContainsKey("code"))
            {
                if (resultJObject["code"].ToString() == "SUCCESS")
                {
                    List<IndustryQAChatReply> industryQAChatQuestReplyList = resultJObject["data"]["chatReplys"].ToObject<List<IndustryQAChatReply>>();

                    foreach (IndustryQAChatReply industryQAChatReply in industryQAChatQuestReplyList)
                    {
                        if (!string.IsNullOrEmpty(industryQAChatReply.audioFileSource))
                        {
                            string audioFileName = Path.GetFileName(industryQAChatReply.audioFileSource);
                            if (!File.Exists(industryQALocalFileDictionary + audioFileName))
                            {
                                AddDownloadFile(string.Format(industryQADownloadFileAPIUrl, industryQAChatReply.audioFileSource));
                            }
                        }

                        foreach (IndustryQAChatReplyScene industryQAChatReplyScene in industryQAChatReply.industryQAChatReplyScenes)
                        {
                            if (!string.IsNullOrEmpty(industryQAChatReplyScene.fileSourceUrl))
                            {
                                string replySceneFileName = Path.GetFileName(industryQAChatReplyScene.fileSourceUrl);
                                if (!File.Exists(industryQALocalFileDictionary + replySceneFileName))
                                {
                                    AddDownloadFile(string.Format(industryQADownloadFileAPIUrl, industryQAChatReplyScene.fileSourceUrl));
                                }
                            }
                        }
                    }

                    DebugMessage("获取行业问答库所有回答成功，共记：" + industryQAChatReplyDic.Count + " 条");
                }
                else
                {
                    DebugMessage("获取行业问答库所有回答失败，不存在code");
                }
            }
            else
            {
                DebugMessage("获取行业问答库所有请求和回答失败");
            }

            
        }

        public IndustryQASimilarResult GetSimilarIndustryQAData(string questionStr)
        {
            string getSimilarIndustryQADataUrl = string.Format(industryQASimilarAPIUrl, questionStr, industryQAUserAccessToken);

            string result = HttpUtils.Post(getSimilarIndustryQADataUrl);
            DebugMessage("获取相似行业问答请求，请求结果：" + result);

            if (string.IsNullOrEmpty(result))
            {
                return null;
            }
            else
            {
                JObject resultJObject = JObject.Parse(result);

                if (resultJObject != null)
                {
                    return resultJObject["data"].ToObject<IndustryQASimilarResult>();
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion

        #region AI应答
        /// <summary>
        /// 获取百度文心一言accesstoken
        /// </summary>
        private void GetWenXinAccessToken()
        {
            string wenxinAccessTokenUrl = string.Format("https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={0}&client_secret={1}", wenxinApiKey, wenxinApiSecret);

            string result = HttpUtils.Post(wenxinAccessTokenUrl);
            DebugMessage("获取Baidu WenXin AccessToken请求，请求结果：" + result);

            JObject resultJObject = JObject.Parse(result);

            if (resultJObject.ContainsKey("error"))
            {
                DebugMessage(string.Format("文心获取AI应答接口accesstoken失败,失败信息:{0}", resultJObject["error_description"].ToString()), MSG_TYPE.ERROR);
            }
            else
            {
                wenxinAccessToken = resultJObject["access_token"].ToString();
                DebugMessage(string.Format("文心获取AI应答接口获取accesstoken成功:{0}", wenxinAccessToken));
            }
        }

        private string ChatWenXin(string askStr)
        {
            string chatWenXinUrl = string.Format("https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/eb-instant?access_token={0}", wenxinAccessToken);
            if (wenXinMessages.Count >= 10)
            {
                wenXinMessages.RemoveRange(0, 2);
            }
            wenXinMessages.Add(new WenXinMessage("user", askStr));
            JObject paramJObject = new JObject 
            {
                {"messages", JArray.FromObject(wenXinMessages)},
                {"stream", false},
                {"user_id", "suzhouxunishuziren"}

            };

            try
            {
                string result = HttpUtils.Post(chatWenXinUrl, paramJObject.ToString(), null);

                DebugMessage("获取Baidu WenXin AI应答请求，请求结果：" + result);

                JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

                if (resultJObject.ContainsKey("error_code"))
                {
                    if (resultJObject.ContainsKey("error_msg"))
                    {
                        DebugMessage(string.Format("文心AI应答错误,错误码:{0}, 错误信息:{1}", resultJObject["error_code"], resultJObject["error_msg"].ToString()), MSG_TYPE.ERROR);
                    }
                    else
                    {
                        DebugMessage(string.Format("文心AI应答错误,错误码:{0}", resultJObject["error_code"], MSG_TYPE.ERROR, true));
                    }
                    wenXinMessages.RemoveAt(wenXinMessages.Count - 1);

                    return null;
                }
                else
                {
                    WenXinResult wenXinResult = resultJObject.ToObject<WenXinResult>();
                    DebugMessage(string.Format("文心AI应答接口应答成功:{0}", wenXinResult.result));

                    wenXinMessages.Add(new WenXinMessage("assistant", wenXinResult.result));
                    return wenXinResult.result;
                }
            }
            catch (Exception ex)
            {
                DebugMessage(string.Format("调用文心AI应答进行接口应答出现异常，异常信息:{0}", ex.Message), MSG_TYPE.ERROR);
                return null;
            }
        }
        #endregion

        #region AI作画
        private void GetQAIsAIImgPattern()
        {
            try
            {
                qaAIImgPatternList = configJObect["qaAIImgPattern"].ToObject<List<string>>();
                DebugMessage(string.Format("获取语句与AI作画正则表达式设置成功，共计：{0} 条", qaAIImgPatternList.Count));
            }
            catch(Exception ex)
            {
                DebugMessage(string.Format("获取语句与AI作画正则表达式设置失败，失败原因：{0}，使用默认设置，共计：{1} 条", ex.Message, qaAIImgPatternList.Count), MSG_TYPE.ERROR);
            }
        }

        private bool MatchQAIsAIImg(string questionStr)
        {
            //做***图 作***图 画***图    做***画 作***画 画***画    做一张 作一张 画一张    做一幅 作一幅 画一幅
            for (int i = 0; i < qaAIImgPatternList.Count; i++)
            {
                if (Regex.IsMatch(questionStr, qaAIImgPatternList[i]))
                {
                    DebugMessage("语句与AI作画正则表达式匹配成功，问题语句：" + questionStr + ", 正则规则：" + qaAIImgPatternList[i]);
                    return true;
                }                
            }
            return false;
        }

        private void GetBaiduAIImgAccessToken()
        {
            string baiduAIImgAccessTokenUrl = string.Format("https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={0}&client_secret={1}", baiduAIImgApiKey, baiduAIImgApiSecret);

            string result = HttpUtils.Post(baiduAIImgAccessTokenUrl);
            JObject resultJObject = JObject.Parse(result);

            if (resultJObject.ContainsKey("error"))
            {
                DebugMessage(string.Format("AI作画获取AI作画接口accesstoken失败,失败信息:{0}", resultJObject["error_description"].ToString()), MSG_TYPE.ERROR);
            }
            else
            {
                baiduAIImgAccessToken = resultJObject["access_token"].ToString();
                DebugMessage(string.Format("AI作画获取AI应答接口获取accesstoken成功:{0}", baiduAIImgAccessToken));
            }
        }

        // 提交百度ai作画请求
        private void BaiduAIImgTxt2Img(string askStr)
        {
            string baiduTxt2ImgUrl = string.Format("https://aip.baidubce.com/rpc/2.0/ernievilg/v1/txt2imgv2?access_token={0}", baiduAIImgAccessToken);
            JObject paramJObject = new JObject
            {
                {  "prompt", askStr + ",写实"},
                {  "width", baiduAIImgWidth},
                {  "height", baiduAIImgHeight}
            };

            try
            {
                string result = HttpUtils.Post(baiduTxt2ImgUrl, paramJObject.ToString(), null);

                DebugMessage("创建Baidu AI Img任务请求，请求结果：" + result);

                JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

                if (resultJObject.ContainsKey("error_code"))
                {
                    if (resultJObject.ContainsKey("error_msg"))
                    {
                        DebugMessage(string.Format("AI作画添加作画任务错误,错误码:{0}, 错误信息:{1}", resultJObject["error_code"], resultJObject["error_msg"].ToString()), MSG_TYPE.ERROR);
                    }
                    else
                    {
                        DebugMessage(string.Format("AI作画添加作画任务错误,错误码:{0}", resultJObject["error_code"], MSG_TYPE.ERROR));
                    }

                    UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgFail));
                }
                else
                {
                    DebugMessage(string.Format("AI作画添加作画任务成功，任务id:{0}", resultJObject["data"]["task_id"].ToString()));
                    UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.InAIImg));

                    baiduAIImgTaskId = resultJObject["data"]["task_id"].ToString();
                    StartGetAIImgTimer();
                }
            }
            catch (Exception ex)
            {
                UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgFail));
                DebugMessage(string.Format("调用AI作画添加作画任务异常，异常信息:{0}", ex.Message), MSG_TYPE.ERROR);
            }
        }

        private void InitGetAIImgTimer()
        {
            DebugMessage(string.Format("初始化设置获取AI图像定时器"));

            getBaiduAIImgTimer = new DispatcherTimer();
            getBaiduAIImgTimer.Tick += new EventHandler(GetAIImgTimerTick);
            getBaiduAIImgTimer.Interval = new TimeSpan(0, 0, 1);
        }

        private void StartGetAIImgTimer()
        {
            getBaiduAIImgTimer.Start();
        }

        private void StopGetAIImgTimer()
        {
            getBaiduAIImgTimer.Stop();
        }

        private void GetAIImgTimerTick(object sender, EventArgs e)
        {
            string getImgUrl = string.Format("https://aip.baidubce.com/rpc/2.0/ernievilg/v1/getImgv2?access_token={0}", baiduAIImgAccessToken);
            JObject paramJObject = new JObject 
            {
                {"task_id", baiduAIImgTaskId}
            };

            try
            {
                string result = HttpUtils.Post(getImgUrl, paramJObject.ToString(), null);

                DebugMessage(string.Format("获取Baidu AI作画请求结果,信息:{0}", result));

                JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

                
                if (resultJObject.ContainsKey("error_code"))
                {
                    if (resultJObject.ContainsKey("error_description"))
                    {
                        DebugMessage(string.Format("获取AI作画画作错误,错误码:{0}, 错误信息:{1}", resultJObject["error_code"], resultJObject["error_description"].ToString()), MSG_TYPE.ERROR);
                    }
                    else
                    {
                        DebugMessage(string.Format("获取AI作画画作错误,错误码:{0}", resultJObject["error_code"], MSG_TYPE.ERROR));
                    }

                    UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgFail));
                    StopGetAIImgTimer();
                }
                else
                {
                    BaiduAIImgResult baiduAIImgResult = resultJObject.ToObject<BaiduAIImgResult>();

                    if (baiduAIImgResult.data.task_status == "SUCCESS")
                    {
                        DebugMessage(string.Format("AI作画生成成功,画作地址:{0}", baiduAIImgResult.data.sub_task_result_list[0].final_image_list[0].img_url));
                        UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgSuccess, baiduAIImgResult.data.sub_task_result_list[0].final_image_list[0].img_url));

                        StopGetAIImgTimer();
                    }
                    else if (baiduAIImgResult.data.task_status == "FAILED")
                    {
                        DebugMessage("AI作画生成失败成功", MSG_TYPE.ERROR, true);
                        UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgFail));

                        StopGetAIImgTimer();
                    }
                    else
                    {
                        DebugMessage("AI作画正在生成中");
                        //UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImage, AIImgProgress.InAIImg));
                    }
                }
            }
            catch (Exception ex)
            {
                UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.AIImg, AIImgProgress.AIImgFail));
                DebugMessage(string.Format("调用AI作画获取作画任务状态异常，异常信息:{0}", ex.Message), MSG_TYPE.ERROR, true);

                StopGetAIImgTimer();
            }
        }
        #endregion

        #region Baidu语音在线合成
        //private ResultMsg 

        private string BaiduShortTTS(string ttsStr)
        {
            //DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，小于等于500，采用短文本转语音", asrResult.Length));
            try
            {
                var option = new RestClientOptions($"https://tsn.baidu.com/text2audio")
                {
                    ThrowOnAnyError = true,
                    MaxTimeout = 30000
                };
                var client = new RestClient(option); 

                var request = new RestRequest("https://tsn.baidu.com/text2audio", Method.Post);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Accept", "*/*");
                request.AddParameter("tex", ttsStr);
                request.AddParameter("tok", baiduTTSAccessToken);
                request.AddParameter("cuid", "Ia28RO38BZpxfPkma9kOqgvbQz73QJQq");
                request.AddParameter("ctp", "1");
                request.AddParameter("lan", "zh");
                request.AddParameter("spd", 5);
                request.AddParameter("pit", 5);
                request.AddParameter("vol", 7);
                request.AddParameter("per", 0);
                request.AddParameter("aue", 6);
                RestResponse response = client.Execute(request);

                if(response.ContentType == "application/json")
                {
                    DebugMessage(string.Format("百度短文本转语音出现错误，返回结果:{0}", response.Content), MSG_TYPE.ERROR);
                    return null;
                }
                else
                {
                    string wavFileName = Path.Combine(configJObect["industryQADataSetting"]["localFileDictionary"].ToString(), Guid.NewGuid().ToString("N") + ".wav");
                    File.WriteAllBytes(wavFileName, response.RawBytes);
                    return wavFileName;
                }
            }
            catch(Exception exception)
            {
                DebugMessage(string.Format("百度短文本转语音出现异常，异常信息:{0}", exception.Message), MSG_TYPE.ERROR);
                return null;
            }
        }

        private void GetBaiduLongTTSAccessToken()
        {
            string baiduAIImgAccessTokenUrl = string.Format("https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={0}&client_secret={1}", baiduTTSAPIKey, baiduTTSAPISecret);

            string result = HttpUtils.Post(baiduAIImgAccessTokenUrl);
            JObject resultJObject = JObject.Parse(result);

            if (resultJObject.ContainsKey("error"))
            {
                DebugMessage(string.Format("Baidu LongTTS接口获取accesstoken失败,失败信息:{0}", resultJObject["error_description"].ToString()), MSG_TYPE.ERROR, true);
            }
            else
            {
                baiduTTSAccessToken = resultJObject["access_token"].ToString();
                DebugMessage(string.Format("Baidu LongTTS接口获取accesstoken成功:{0}", baiduTTSAccessToken));
            }
        }

        private void CreateBaiduLongTTS(string ttsStr)
        {
            string createLongTTSUrl = string.Format("https://aip.baidubce.com/rpc/2.0/tts/v1/create?access_token={0}", baiduTTSAccessToken);
            JObject paramJObject = new JObject
            {
                {"text",  ttsStr},
                {"format",  "wav"},
                {"voice", 0},
                {"lang", "zh"}
            };

            try
            {
                string result = HttpUtils.Post(createLongTTSUrl, paramJObject.ToString(), null);

                DebugMessage("添加Baidu Long TTS任务请求，请求结果：" + result);

                JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

                if (resultJObject.ContainsKey("error_code"))
                {
                    if (resultJObject.ContainsKey("error_description"))
                    {
                        DebugMessage(string.Format("添加Baidu Long TTS任务错误,错误码:{0}, 错误信息:{1}", resultJObject["error_code"], resultJObject["error_description"].ToString()), MSG_TYPE.ERROR, true);
                    }
                    else
                    {
                        DebugMessage(string.Format("添加Baidu Long TTS任务错误,错误码:{0}", resultJObject["error_code"], MSG_TYPE.ERROR, true));
                    }

                    UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.SayAPIError));
                }
                else
                {
                    if(resultJObject["task_status"].ToString() == "Created")
                    {
                        DebugMessage(string.Format("添加Baidu Long TTS任务成功,task id:{0}", resultJObject["task_id"].ToString()));
                        baiduLongTTSTaskId = resultJObject["task_id"].ToString();

                        StartGetLongTTSTimer();
                    }
                    else
                    {
                        DebugMessage("未添加Baidu Long TTS任务出现，请检查返回信息");
                        UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.SayAPIError));
                    }
                }
            }
            catch (Exception ex)
            {
                Ctrl2SayApiError();
                DebugMessage(string.Format("添加Baidu Long TTS任务出现异常，异常信息:{0}", ex.Message), MSG_TYPE.ERROR, true);
            }
        }

        private void InitGetLongTTSTimer()
        {
            DebugMessage(string.Format("初始化设置获取AI图像定时器"));

            getBaiduLongTTSTimer = new DispatcherTimer();
            getBaiduLongTTSTimer.Tick += new EventHandler(GetLongTTSTimerTick);
            getBaiduLongTTSTimer.Interval = new TimeSpan(0, 0, 1);
        }

        private void StartGetLongTTSTimer()
        {
            getBaiduLongTTSTimer.Start();
        }

        private void StopGetLongTTSTimer()
        {
            getBaiduLongTTSTimer.Stop();
        }

        private void GetLongTTSTimerTick(object sender, EventArgs e)
        {
            string getLongTTSUrl = string.Format("https://aip.baidubce.com/rpc/2.0/tts/v1/query?access_token={0}", baiduTTSAccessToken);
            JObject paramJObject = new JObject
            {
                {"task_ids", JArray.FromObject(new List<string>() { baiduLongTTSTaskId })}
            };

            try
            {
                string result = HttpUtils.Post(getLongTTSUrl, paramJObject.ToString(), null);

                DebugMessage(string.Format("获取Baidu Long TTS任务状态请求,信息:{0}", result));

                JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

                if (resultJObject.ContainsKey("error_code"))
                {
                    if (resultJObject.ContainsKey("error_msg"))
                    {
                        DebugMessage(string.Format("获取Baidu Long TTS结果错误,错误码:{0}, 错误信息:{1}", resultJObject["error_code"], resultJObject["error_msg"].ToString()), MSG_TYPE.ERROR, true);
                    }
                    else
                    {
                        DebugMessage(string.Format("获取Baidu Long TTS结果错误,错误码:{0}", resultJObject["error_code"], MSG_TYPE.ERROR, true));
                    }

                    StopGetLongTTSTimer();
                    Ctrl2SayApiError();
                }
                else
                {
                    JObject taskinfo = (JObject)((JArray)resultJObject["tasks_info"])[0];

                    if (taskinfo["task_status"].ToString() == "Success")
                    {
                        DebugMessage(string.Format("Baidu Long TTS生成成功,音频地址:{0}", taskinfo["task_result"]["speech_url"].ToString()));
                        StopGetLongTTSTimer();

                        string wavFileName = Guid.NewGuid().ToString() + ".wav";

                        const long BlockSize = 2 * 1024;
                        try
                        {
                            // 设置参数
                            HttpWebRequest request = WebRequest.Create(taskinfo["task_result"]["speech_url"].ToString()) as HttpWebRequest;
                            // 发送请求并获取相应回应数据
                            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                            // 直到request.GetResponse()程序才开始向目标网页发送Post请求
                            Stream responseStream = response.GetResponseStream();

                            // 创建本地文件写入流
                            Stream stream = new FileStream(Path.Combine(configJObect["industryQADataSetting"]["localFileDictionary"].ToString(), wavFileName), FileMode.Create);

                            byte[] bArr = new byte[BlockSize];
                            int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                            while (size > 0)
                            {
                                stream.Write(bArr, 0, size);
                                size = responseStream.Read(bArr, 0, (int)bArr.Length);
                            }
                            stream.Close();
                            responseStream.Close();

                            Ctrl2SayWav(wavFileName);
                        }
                        catch (Exception exception)
                        {
                            DebugMessage("长文本转语音，语音下载到本地出现异常，异常信息：" + exception.Message);
                            Ctrl2SayApiError();
                        }
                    }
                    else if (taskinfo["task_status"].ToString() == "Running")
                    {
                        DebugMessage("Baidu Long TTS生成中");
                    }
                }
            }
            catch (Exception ex)
            {
                Ctrl2SayApiError();
                DebugMessage(string.Format("调用Baidu Long TTS获取任务状态异常，异常信息:{0}", ex.Message), MSG_TYPE.ERROR, true);

                StopGetLongTTSTimer();
            }
        }
        #endregion

        #region 发送Audio2Face和数字人控制信息
        private void Ctrl2StandBy()
        {
            Audio2FaceMute();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2Play()
        {
            Audio2FacePlay();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.TakeAnim));
        }

        private void Ctrl2Pause()
        {
            Audio2FacePause();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayWelcome()
        {
            Audio2FaceSayWelcome();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayApiError()
        {
            Audio2FaceSayApiError();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayNoVoice()
        {
            Audio2FaceSayNoVoice();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayInAIReply()
        {
            Audio2FaceSayInAIReply();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void CtrlSayInAIImg()
        {
            Audio2FaceSayInAIImg();
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.StandBy));
        }

        private void Ctrl2SayWav(string wavName)
        {
            Thread.Sleep(500);
            Audio2FaceSayWav(wavName);
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.TakeAnim));
        }
        #endregion

        #region Audio2Face控制
        private void InitAudio2FaceServer()
        {
            Audio2FaceUtils.InitSettings(configJObect["audio2Face"]["serverAddress"].ToString());

            if (Audio2FaceUtils.GetAudio2FaceServerStatus())
            {
                DebugMessage("Audio2Face服务在线");
                SetAudio2FaceServerStatusLabel(true);

                if (Audio2FaceUtils.LoadAudio2FaceUSD(configJObect["audio2Face"]["usdFile"].ToString()))
                {
                    DebugMessage("Audio2Face服务加载 " + configJObect["audio2Face"]["usdFile"].ToString() + " 成功", MSG_TYPE.INFO);

                    Thread.Sleep(10000);

                    if (Audio2FaceUtils.ActivateAudio2FaceExporterStreamLiveLink())
                    {
                        DebugMessage("Audio2Face服务建立LveLink连接成功", MSG_TYPE.INFO);
                    }
                    else
                    {
                        DebugMessage("Audio2Face服务建立LveLink连接失败", MSG_TYPE.INFO);
                    }
                }
                else
                {
                    DebugMessage("Audio2Face服务加载 " + configJObect["audio2Face"]["usdFile"].ToString() + " 失败", MSG_TYPE.ERROR);
                }
            }
            else
            {
                DebugMessage("Audio2Face服务离线", MSG_TYPE.ERROR);
                SetAudio2FaceServerStatusLabel(false);
            }
        }

        private void Audio2FacePlay()
        {
            Audio2FaceUtils.ControlAudio2FacePlayerPlay();
        }

        private void Audio2FacePause()
        {
            Audio2FaceUtils.ControlAudio2FacePlayerPause();
        }

        private void Audio2FaceMute()
        {
            Audio2FaceUtils.SetAudio2FacePlayerTrack("mute.wav");
        }

        private void Audio2FaceSayWelcome()
        {
            if (new Random().Next(2) == 0)
            {
                Audio2FaceUtils.SetAudio2FacePlayerTrack("nihao.wav");
            }
            else
            {
                Audio2FaceUtils.SetAudio2FacePlayerTrack("wozai.wav");
            }
            Audio2FaceUtils.ControlAudio2FacePlayerPlay();
        }

        private void Audio2FaceSayApiError()
        {
            Audio2FaceUtils.SetAudio2FacePlayerTrack("apiErrpr.wav");
            Audio2FaceUtils.ControlAudio2FacePlayerPlay();
        }

        private void Audio2FaceSayNoVoice()
        {
            Audio2FaceUtils.SetAudio2FacePlayerTrack("noVoice.wav");
            Audio2FaceUtils.ControlAudio2FacePlayerPlay();
        }

        private void Audio2FaceSayInAIReply()
        {
            Audio2FaceUtils.SetAudio2FacePlayerTrack("inAIReply.wav");// 正在ai推理回答中
            Audio2FaceUtils.ControlAudio2FacePlayerPlay();
        }

        private void Audio2FaceSayInAIImg()
        {
            Audio2FaceUtils.SetAudio2FacePlayerTrack("inAIImg.wav");// 正在ai作画中
            Audio2FaceUtils.ControlAudio2FacePlayerPlay();
        }

        private void Audio2FaceSayWav(string wavName)
        {
            Audio2FaceUtils.SetAudio2FacePlayerTrack(wavName);
            Audio2FaceUtils.ControlAudio2FacePlayerPlay();
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
                udpServerIP = configJObect["udpServer"]["ip"].ToString();
                udpServerPort = int.Parse(configJObect["udpServer"]["port"].ToString());

                DebugMessage("读取UDP Server设置信息：IP：" + udpServerIP + "，端口：" + udpServerPort);

                virtualHumanSeverIP = configJObect["virtualHumanSever"]["ip"].ToString();
                virtualHumanSeverPort = int.Parse(configJObect["virtualHumanSever"]["port"].ToString());
                DebugMessage("读取Virtual Human Sever设置信息：IP：" + virtualHumanSeverIP + "，端口：" + virtualHumanSeverPort);

                multiScreenPlayerPort = int.Parse(configJObect["multiScreenPlayer"]["port"].ToString());
                DebugMessage("读取Multi Screen Player 设置信息：端口：" + multiScreenPlayerPort);

                udpServerCtrl = new UdpServerCtrl(udpServerIP, udpServerPort, OnMessage, DebugMessage, ';');
                udpServerCtrl.Init();
            }
            catch (Exception ex)
            {
                DebugMessage("初始化设置UDP服务失败:" + ex.Message, MSG_TYPE.ERROR, true);
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
            }
            else if (messageArray[0].Equals("Stream"))
            {
                string targetIP = messageArray[2];
                if (messageArray[1] == "Play")
                {
                    UdpSendMessage2MultiScreenPlayer(targetIP, StringUtils.GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Play));

                    if (!string.IsNullOrEmpty(multiScreenPlayerIP) && multiScreenPlayerIP != targetIP)
                    {
                        UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, StringUtils.GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Stop));
                    }

                    multiScreenPlayerIP = targetIP;
                }
                else if (messageArray[1] == "Stop")
                {
                    UdpSendMessage2MultiScreenPlayer(targetIP, StringUtils.GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Stop));
                    if (!string.IsNullOrEmpty(multiScreenPlayerIP) && multiScreenPlayerIP != targetIP)
                    {
                        UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, StringUtils.GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType.Stop));
                    }

                    multiScreenPlayerIP = "";
                }
            }
            else if (messageArray[0] == "Position")
            {
                if (!string.IsNullOrEmpty(multiScreenPlayerIP))
                {
                    UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, StringUtils.GetMultiScreenPlayerCtrlPositionStr(int.Parse(messageArray[1]), int.Parse(messageArray[2])));
                }
            }
            else if (messageArray[0] == "Size")
            {
                if (!string.IsNullOrEmpty(multiScreenPlayerIP))
                {
                    UdpSendMessage2MultiScreenPlayer(multiScreenPlayerIP, StringUtils.GetMultiScreenPlayerCtrlSizeStr(int.Parse(messageArray[1]), int.Parse(messageArray[2])));
                }
            }
        }

        private void UdpSendMessage2VirtualHumanSever(string jsonStr)
        {
            DebugMessage("UDP服务发送VirtualHumanSever消息，地址：" + virtualHumanSeverIP + ":" + virtualHumanSeverPort + " 发送消息" + jsonStr);
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
            UdpSendMessage2VirtualHumanSever(StringUtils.GetVirtualPersonCtrlStr(VirtualPersonCtrlType.HeartBeat));

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

        #region 文件下载器
        private void InitDownloader()
        {
            try
            {
                this.downloadTask = new DownloadTask(configJObect["industryQADataSetting"]["localFileDictionary"].ToString());
                this.downloader = new ThreadWorker(downloadTask);
                this.downloadTask.StartEvent += OnDownloadStart;
                this.downloadTask.StepEvent += OnDownloadStep;
                this.downloadTask.FinishEvent += OnDownloadFinish;
                this.downloadTask.ErrorEvent += OnDownloadError;
                this.downloader.Start();

                DebugMessage("初始化文件下载器完成，本地文件目录：" + configJObect["industryQADataSetting"]["localFileDictionary"].ToString());
            }
            catch (Exception exception)
            {
                DebugMessage("初始化文件下载器出现异常，异常信息：" + exception.Message, MSG_TYPE.ERROR);
            }
        }

        public void AddDownloadFile(string filePath)
        {
            downloadTask.AddDownloadFile(filePath);
        }

        public void OnDownloadStart(Object sender, DownloadMsgArgs msg)
        {
            
        }

        public void OnDownloadStep(Object sender, DownloadMsgArgs msg)
        {
            
        }

        public void OnDownloadFinish(Object sender, FileMsgArgs msg)
        {
            DebugMessage("文件'" + msg.fileName + "'下载完毕！");
        }

        public void OnDownloadError(Object sender, ErrorMsgArgs msg)
        {
            DebugMessage("文件'" + msg.fileName + "'下载出错:" + msg.errorMsg, MSG_TYPE.ERROR);
        }
        #endregion

        #region 加载配置文件中配置信息
        private void LoadConfig()
        {
            string jsonfile = System.IO.Path.Combine(Thread.GetDomain().BaseDirectory, "Config", "Config.json");//配置文件路径
            try
            {
                using (StreamReader file = File.OpenText(jsonfile))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        configJObect = (JObject)JToken.ReadFrom(reader);
                    }
                }
            }
            catch(Exception ex)
            {
                DebugMessage(string.Format("读取配置文件{0}出现异常，异常信息：{1}", jsonfile, ex.Message), MSG_TYPE.ERROR, true);
            }
        }
        #endregion

        #region 日志和信息
        /// <summary>
        /// 初始化Log4net和Notifier
        /// </summary>
        private void InitLog4netAndNotifier()
        {
            // 初始化设置Log4net
            log4net.Config.XmlConfigurator.Configure();

            // 初始化设置提示框
            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomCenter,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            //Log_ListView.ScrollChanged += (sender, e) =>
            //{
            //    // 如果滚动到了底部，就自动向下滚动
            //    if (e.VerticalOffset == listBox.ScrollableHeight)
            //    {
            //        listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
            //    }
            //};
        }

        /// <summary>
        /// 输出日志信息到窗体和日志文件和弹窗
        /// </summary>
        /// <param name="msg">日志信息</param>
        /// <param name="msgType">日志类型</param>
        /// <param name="isShowNotifier">是否显示日志弹窗</param>
        private void DebugMessage(string msg, MSG_TYPE msgType = MSG_TYPE.INFO, bool isShowNotifier = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString());
            sb.Append(":  ");

            if (msgType == MSG_TYPE.INFO)
            {
                sb.Append("INFO:  ");
                log.Info(msg);

                if (isShowNotifier)
                {
                    notifier.ShowInformation(msg);
                }
            }
            else if (msgType == MSG_TYPE.WARNNING)
            {
                sb.Append("WARNNING:  ");
                log.Warn(msg);

                if (isShowNotifier)
                {
                    notifier.ShowWarning(msg);
                }
            }
            else if (msgType == MSG_TYPE.ERROR)
            {
                sb.Append("ERROR:  ");
                log.Error(msg);

                if (isShowNotifier)
                {
                    notifier.ShowError(msg);
                }
            }

            sb.Append(msg);
            SetListBox(sb.ToString(), msgType);
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
