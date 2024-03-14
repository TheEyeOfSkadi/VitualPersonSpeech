using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VitualPersonSpeech
{
    /// <summary>
    /// VirtualHumanBg.xaml 的交互逻辑
    /// </summary>
    public partial class VirtualHumanBg : Window
    {
        BitmapImage bitmap = new BitmapImage();


        public delegate void ClientMsgDelegate(string str);
        public ClientMsgDelegate clientMsg;

        public VirtualHumanBg(ClientMsgDelegate clientMsg)
        {
            InitializeComponent();
            this.clientMsg = clientMsg;
        }

        public void ShowImg(string fileName)
        {
            string fileFullName = Path.Combine(Thread.GetDomain().BaseDirectory, "Data", fileName);
            if (!File.Exists(fileFullName)) // 文件不存在则下载
            { 

            }

        }
    }
}
