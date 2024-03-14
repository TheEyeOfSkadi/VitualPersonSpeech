using log4net;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace VitualPersonSpeech.Utils
{
    class HttpUtils
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // get方式发送请求
        public static string Post(string postUrl, string authorization = null)
        {
            try
            {
                HttpWebRequest req = WebRequest.Create(postUrl) as HttpWebRequest;
                req.Method = "POST";
                if (authorization != null)
                    req.Headers.Add("Authorization", authorization);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                log.Error("Post请求发送失败,失败信息:" + e.Message);
                return null;
            }
        }

        // post方式发送请求
        public static string Post(string postUrl, string postData, string authorization = null)
        {
            try
            {
                HttpWebRequest req = WebRequest.Create(postUrl) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/json";
                if (authorization != null)
                    req.Headers.Add("Authorization", authorization);
                byte[] data = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = data.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                log.Error("Post请求发送失败,失败信息:" + e.Message);
                return null;
            }
        }

        // post方式发送请求
        public static string Post(string postUrl, byte[] postData, string authorization = null)
        {
            try
            {
                HttpWebRequest req = WebRequest.Create(postUrl) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/json";
                if (authorization != null)
                    req.Headers.Add("Authorization", authorization);
                req.ContentLength = postData.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(postData, 0, postData.Length);
                    reqStream.Close();
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                log.Error("Post请求发送失败,失败信息:" + e.Message);
                return null;
            }
        }

        // get方式发送请求
        public static string Get(string getUrl, string authorization = null)
        {
            try
            {
                HttpWebRequest req = WebRequest.Create(getUrl) as HttpWebRequest;
                req.Method = "GET";
                if (authorization != null)
                    req.Headers.Add("Authorization", authorization);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                log.Error("Get请求发送失败,失败信息:" + e.Message);
                return null;
            }
        }
    }
}
