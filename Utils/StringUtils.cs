using System;
using System.Text;
using VitualPersonSpeech.Model;

namespace VitualPersonSpeech.Utils
{
    class StringUtils
    {
        /// <summary>
        /// 字节数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        public static string UrlEncode(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = Encoding.UTF8.GetBytes(str); //默认是System.Text.Encoding.Default.GetBytes(str)
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(byStr[i], 16));
            }

            return (sb.ToString());
        }

        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType) 
        {
            return string.Format("{0}", virtualPersonCtrlType);
        }

        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType, AIReplyProgress aiReplyProgress, string content = "")
        {
            return string.Format("{0}-{1}-{2}", virtualPersonCtrlType, aiReplyProgress, content);
        }

        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType, AIImgProgress aiImgProgress, string content = "")
        {
            return string.Format("{0}-{1}-{2}", virtualPersonCtrlType, aiImgProgress, content);
        }

        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType, IndustryQADataChatReply industryQAChatReply, string content = "")
        {
            return string.Format("{0}-{1}-{2}", virtualPersonCtrlType, industryQAChatReply, content);
        }

        public static string GetMultiScreenPlayerCtrlPositionStr(int x, int y)
        {
            return string.Format("{0}-{1}-{2}", MultiScreenPlayerCtrlType.Position, x, y);
        }

        public static string GetMultiScreenPlayerCtrlSizeStr(int width, int height)
        {
            return string.Format("{0}-{1}-{2}", MultiScreenPlayerCtrlType.Size, width, height);
        }

        public static string GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType streamCtrlType)
        {
            return string.Format("{0}-{1}", MultiScreenPlayerCtrlType.Stream, streamCtrlType);
        }

    }
}
