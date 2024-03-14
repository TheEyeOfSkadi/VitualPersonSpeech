using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VitualPersonSpeech.Utils
{
    public class CommonUtils
    {
        /// <summary>
        /// 将两个byte数组合在一起
        /// </summary>
        /// <param name="sourceBytes"></param>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        public static byte[] CombineBytes(byte[] sourceBytes, byte[] dataBytes)
        {
            if (sourceBytes == null || dataBytes == null || dataBytes.Length == 0)
            {
                return null;
            }

            byte[] combinedBytes = new byte[sourceBytes.Length + dataBytes.Length];
            Buffer.BlockCopy(sourceBytes, 0, combinedBytes, 0, sourceBytes.Length);
            Buffer.BlockCopy(dataBytes, 0, combinedBytes, sourceBytes.Length, dataBytes.Length);

            return combinedBytes;
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        /// <returns></returns>
        public static string GenerateTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        /// 指针转字符串
        /// </summary>
        /// <param name="p">指向非托管代码字符串的指针</param>
        /// <returns>返回指针指向的字符串</returns>
        public static string PtrToStr(IntPtr p)
        {
            List<byte> lb = new List<byte>();
            try
            {
                while (Marshal.ReadByte(p) != 0)
                {
                    lb.Add(Marshal.ReadByte(p));
                    p = p + 1;
                }
            }
            catch (AccessViolationException)
            {
                return null;
            }
            return Encoding.UTF8.GetString(lb.ToArray());
        }
    }
}
