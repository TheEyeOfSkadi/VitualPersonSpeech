using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace VitualPersonSpeech.Tasks
{
    public class FileMsgArgs : EventArgs
    {
        public FileMsgArgs(string fileName)
        {
            this.fileName = fileName;
        }

        public string fileName { get; set; }
    }

    public class DownloadMsgArgs : FileMsgArgs
    {
        public DownloadMsgArgs(string fileName, long fileSize, long blockSize) : base(fileName)
        {
            this.fileSize = fileSize;
            this.blockSize = blockSize;
        }
        public long fileSize { get; set; }
        public long blockSize { get; set; }
    }

    public class ErrorMsgArgs : FileMsgArgs
    {
        public ErrorMsgArgs(string fileName, string errorMsg) : base(fileName)
        {
            this.fileName = fileName;
            this.errorMsg = errorMsg;
        }

        public string errorMsg { get; set; }
    }

    public class DownloadTask : Task
    {
        private string localFolder;

        private Queue<string> downloadFiles = new Queue<string>();

        private ManualResetEvent mre = new ManualResetEvent(false);

        public event EventHandler<DownloadMsgArgs> StartEvent;
        public event EventHandler<DownloadMsgArgs> StepEvent;
        public event EventHandler<FileMsgArgs> FinishEvent;
        public event EventHandler<ErrorMsgArgs> ErrorEvent;

        public DownloadTask(string localFolder)
        {
            if (!Directory.Exists(localFolder))
                Directory.CreateDirectory(localFolder);

            this.localFolder = localFolder;
        }

        public string GetLocalPath(string fileName)
        {
            return localFolder + "\\" + fileName;
        }

        public override void doWork()
        {
            mre.WaitOne();

            while (!base.stop && downloadFiles.Count > 0)
            {
                string fileName;
                lock (downloadFiles)
                {
                    fileName = downloadFiles.Dequeue();
                }
                HttpDownloadFile(fileName);
            }
        }

        public void HttpDownloadFile(string fileName)
        {
            const long BlockSize = 2 * 1024;
            string url = fileName;
            try
            {
                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                // 发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                // 直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();

                // 创建本地文件写入流
                Stream stream = new FileStream(GetLocalPath(fileName), FileMode.Create);

                FireStartEvent(fileName, response.ContentLength, BlockSize);

                byte[] bArr = new byte[BlockSize];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (!base.stop && size > 0)
                {
                    stream.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);

                    FireStepEvent(fileName, response.ContentLength, BlockSize);
                }
                stream.Close();
                responseStream.Close();

                FireFinishEvent(fileName);
            }
            catch (Exception ex)
            {
                FireErrorEvent(fileName, ex.Message);
            }
        }

        public void FireStartEvent(string fileName, long fileSize, long blockSize)
        {
            StartEvent?.Invoke(this, new DownloadMsgArgs(fileName, fileSize, blockSize));
        }

        public void FireStepEvent(string fileName, long fileSize, long blockSize)
        {
            StepEvent?.Invoke(this, new DownloadMsgArgs(fileName, fileSize, blockSize));
        }

        public void FireFinishEvent(string fileName)
        {
            FinishEvent?.Invoke(this, new FileMsgArgs(fileName));
        }

        public void FireErrorEvent(string fileName, string errorMsg)
        {
            ErrorEvent?.Invoke(this, new ErrorMsgArgs(fileName, errorMsg));
        }

        public void AddDownloadFile(string file)
        {
            lock (downloadFiles)
            {
                downloadFiles.Enqueue(file);
            }
            mre.Set();
        }

        protected override void doStop()
        {
            mre.Set();
        }

        public override void CleanUp()
        {
            mre.Close();
        }
    }
}
