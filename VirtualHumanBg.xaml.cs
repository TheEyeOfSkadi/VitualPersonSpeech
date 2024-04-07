using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Timers;
using VitualPersonSpeech.Model;
using VitualPersonSpeech.Utils;
using Newtonsoft.Json.Linq;

namespace VitualPersonSpeech
{
    /// <summary>
    /// VirtualHumanBg.xaml 的交互逻辑
    /// </summary>
    public partial class VirtualHumanBg : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public delegate void ClientMsgDelegate(string msg);
        public ClientMsgDelegate clientMsg;

        private string industryQALocalFileDictionary;

        private BitmapImage _myImage;
        public BitmapImage MyImage
        {
            get { return _myImage; }
            set
            {
                _myImage = value;
                OnPropertyChanged(nameof(MyImage));
            }
        }

        private readonly ConcurrentQueue<(IndustryQAChatReplyScene industryQAChatReplyScene, DateTime ExecuteAt)> _queue = new ConcurrentQueue<(IndustryQAChatReplyScene, DateTime)>();
        private System.Timers.Timer _timer;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;

        //Queue<IndustryQAChatReplyScene> 

        public VirtualHumanBg(string industryQALocalFileDictionary, ClientMsgDelegate clientMsg)
        {
            InitializeComponent();
            this.industryQALocalFileDictionary = industryQALocalFileDictionary;
            this.clientMsg = clientMsg;

            _cancellationTokenSource = new CancellationTokenSource();

            //_timer = new System.Timers.Timer(CheckQueue, AutoReset: false);
            _timer = new System.Timers.Timer();
            _timer.Elapsed += CheckQueue;
            _timer.AutoReset = false;
        }

        public void EnqueueIndustryQAChatReplyScenes(List<IndustryQAChatReplyScene> industryQAChatReplyScenes)
        {
            foreach(IndustryQAChatReplyScene industryQAChatReplyScene in industryQAChatReplyScenes)
            {
                _queue.Enqueue((industryQAChatReplyScene, DateTime.Now.Add(TimeSpan.FromSeconds(industryQAChatReplyScene.time))));
            }
           
            StartTimerIfNeeded();
        }

        public void Start()
        {
            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            StartTimerIfNeeded();
        }

        public void Stop()
        {
            _isRunning = false;
            _timer.Stop();
            _cancellationTokenSource.Cancel();
        }

        private void StartTimerIfNeeded()
        {
            if (!_isRunning || _timer.Enabled) return;

            _timer.Start();
        }

        private void CheckQueue(object sender, ElapsedEventArgs e)
        {
            if (_cancellationTokenSource.IsCancellationRequested) return;

            while (_queue.TryDequeue(out var queueData))
            {
                if (queueData.ExecuteAt > DateTime.Now)
                {
                    // 如果任务执行时间还未到，则重新安排计时器  
                    int delayInMilliseconds = (int)(queueData.ExecuteAt - DateTime.Now).TotalMilliseconds;
                    _timer.Interval = delayInMilliseconds;
                    _timer.Start();
                    return;
                }

                // 执行任务  
                Task.Run(() => DoShowImg(queueData.industryQAChatReplyScene, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }

            // 如果队列为空，则停止计时器  
            _timer.Stop();
        }

        async void DoShowImg(IndustryQAChatReplyScene industryQAChatReplyScene, CancellationToken token)
        {
            while (!token.IsCancellationRequested)  
            {
                string fileFullName = industryQALocalFileDictionary + Path.GetFileName(industryQAChatReplyScene.fileSourceUrl);
                if (!File.Exists(fileFullName)) // 文件不存在则下载
                {
                    ResultMsg resultMsg = await HttpUtils.DownloadFileAsync("", fileFullName);

                    if (resultMsg.msgType == MSG_TYPE.ERROR)
                    {
                        return;
                    }
                }

                await Task.Delay(industryQAChatReplyScene.time * 1000);

                try
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    // 使用FileStream打开图片文件  
                    using (FileStream stream = new FileStream(fileFullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // 初始化BitmapImage并设置StreamSource  
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = stream;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 当图片加载后，立即释放资源  
                        bitmapImage.EndInit();
                    }
                    MyImage = bitmapImage;
                }
                catch (Exception ex)
                {
                    // 处理异常，例如文件未找到、路径错误等  
                    clientMsg("读取本地文件：" + fileFullName + " 出现异常，异常信息：" + ex.Message);
                }
            } 
        }  
    }
}
