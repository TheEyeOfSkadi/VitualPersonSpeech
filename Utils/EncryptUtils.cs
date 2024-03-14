using log4net;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace VitualPersonSpeech.Utils
{
    class EncryptUtils
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetMd5Code(string text)
        {
            try
            {
                //MD5类是抽象类
                MD5 md5 = MD5.Create();
                //需要将字符串转成字节数组
                byte[] buffer = Encoding.Default.GetBytes(text);
                //加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择
                byte[] md5buffer = md5.ComputeHash(buffer);
                string str = null;
                // 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得
                foreach (byte b in md5buffer)
                {
                    //得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 
                    //但是在和对方测试过程中，发现我这边的MD5加密编码，经常出现少一位或几位的问题；
                    //后来分析发现是 字符串格式符的问题， X 表示大写， x 表示小写， 
                    //X2和x2表示不省略首位为0的十六进制数字；
                    str += b.ToString("x2");
                }
                Console.WriteLine(str);//202cb962ac59075b964b07152d234b70
                return str;
            }
            catch (Exception e)
            {
                log.Error("GetMd5Code exception:" + e.Message);
            }
            return null;
        }

        /// <summary>
        /// HMACSHA256加密  对二进制数据转Base64后再返回
        /// </summary>
        /// <param name="text">要加密的原串</param>
        /// <param name="key">私钥</param>
        /// <returns></returns>
        public static string HMACSHA256Text(string text, string key)
        {
            try
            {
                HMACSHA256 hMACSHA256 = new HMACSHA256
                {
                    Key = Encoding.UTF8.GetBytes(key)
                };

                byte[] dataBuffer = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = hMACSHA256.ComputeHash(dataBuffer);
                    
                return Convert.ToBase64String(hashBytes);
            }
            catch (Exception e)
            {   
                 log.Error("HMACSHA1Text exception:" + e.Message);
            }
            return null;
        }
    }
}
