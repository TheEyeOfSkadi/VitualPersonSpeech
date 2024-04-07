using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Threading;
using VitualPersonSpeech.Model;

namespace MessageCtrl
{
    public delegate void OutputDebugMsg(string str, MSG_TYPE msgType = MSG_TYPE.INFO);

    public abstract class MessageCtrlBase
    {
        public OutputDebugMsg outputDebugMsg;
        
        public MessageCtrlBase(OutputDebugMsg outputDebugMsg)
        {
            this.outputDebugMsg += outputDebugMsg;
        }

        public void Close()
        {
            outputDebugMsg = null;
            doClose();
            Thread.Sleep(10);
        }

        protected abstract void doClose();
    }
}
