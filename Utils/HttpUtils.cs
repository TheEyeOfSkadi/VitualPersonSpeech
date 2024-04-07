using log4net;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VitualPersonSpeech.Model;

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

        // 下载文件
        public static async Task<ResultMsg> DownloadFileAsync(string url, string localFileName)
        {
            try
            {
                HttpClient client = new HttpClient();
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    if(response.IsSuccessStatusCode)
                    {

                    }
                    response.EnsureSuccessStatusCode(); // 确保请求成功  

                    // 获取响应内容流  
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        // 创建一个本地文件流来写入下载的内容  
                        using (var fileStream = new FileStream(localFileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                }
                return ResultMsg.Info(localFileName);
            }
            catch(Exception exception)
            {
                return ResultMsg.Error("下载文件出现异常，异常信息：" + exception.Message);
            }
        }
    }
}
