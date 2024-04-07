using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VitualPersonSpeech.Model;

namespace VitualPersonSpeech.Utils.TTSUtils
{
    // 百度文本转语音
    public class BaiduTTS
    {
        public string LocalFileDictionary { get; set; } = "";

        public BaiduTTS(string localFileDictionary)
        {
            LocalFileDictionary = localFileDictionary;
        }

        private string apiKey = "iQxBR94puMGVjLSOsUfghba5";
        private string secretKey = "oEgvDNFTz9huud3Xb22wdeEaojLYw8hW";
        private string accessToken = "";// 百度语音文本在线合成accesstoken

        // 发送消息委托  
        public delegate void SendMsgDelegate(ResultMsg resultMsg);
        public SendMsgDelegate SendMsgEvent;

        // 触发发送消息委托的方法  
        protected virtual void OnSendMsgEvent(ResultMsg resultMsg)
        {
            SendMsgEvent?.Invoke(resultMsg); // 安全地调用委托，避免空引用异常  
        }

        public async Task GetAccessToken()
        {
            string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.SendAsync(request);

                // 确保请求成功  
                response.EnsureSuccessStatusCode();

                // 读取响应内容  
                string responseBody = await response.Content.ReadAsStringAsync();
                OnSendMsgEvent(ResultMsg.Info($"Baidu TTS接口获取accesstoken请求，结果:{responseBody}"));
                JObject resultJObject = JObject.Parse(responseBody);

                if (resultJObject.ContainsKey("error"))
                {
                    OnSendMsgEvent(ResultMsg.Error($"Baidu TTS接口获取accesstoken失败,失败信息:{resultJObject["error_description"].ToString()}"));
                }
                else
                {
                    accessToken = resultJObject["access_token"].ToString();
                    Console.WriteLine(accessToken);
                    OnSendMsgEvent(ResultMsg.Success($"Baidu TTS接口获取accesstoken成功:{resultJObject["access_token"]}"));
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu TTS接口获取accesstoken出现异常，异常信息:{ex.Message}"));
            }
        }

        // 合成的文本，文本长度必须小于1024GBK字节。建议每次请求文本不超过120字节，约为60个汉字或者字母数字。
        // 请注意计费统计依据：120个GBK字节以内（含120个）记为1次计费调用；每超过120个GBK字节则多记1次计费调用。
        public ResultMsg TTS(string text)
        {
            if (text.Length <= 500)
            {
                return ShortTTS(text);
            }
            else
            {
                return LongTTS(text);
            }
        }

        private ResultMsg ShortTTS(string text)
        {
            //DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，小于等于500，采用短文本转语音", asrResult.Length));
            string txt2AudioUrl = $"https://tsn.baidu.com/text2audio";
            try
            {
                var client = new RestClient();
                var request = new RestRequest(txt2AudioUrl, Method.Post);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Accept", "*/*");

                request.AddParameter("tex", text);
                request.AddParameter("tok", accessToken);
                request.AddParameter("cuid", "Ia28RO38BZpxfPkma9kOqgvbQz73QJQq");
                request.AddParameter("ctp", "1");
                request.AddParameter("lan", "zh");
                request.AddParameter("spd", "5");
                request.AddParameter("pit", "5");
                request.AddParameter("vol", "5");
                request.AddParameter("per", "5118");
                request.AddParameter("aue", "6");

                var response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    // 解析响应内容类型  
                    var contentType = response.ContentType;

                    if (contentType == "application/json")
                    {
                        return ResultMsg.Error($"BaiduTTS短文本转语音出现错误，返回结果：{response.Content}");
                    }
                    else
                    {
                        var wavBytes = response.RawBytes;
                        string wavFileName = Path.Combine(LocalFileDictionary, Guid.NewGuid().ToString("N") + ".wav");
                        File.WriteAllBytes(wavFileName, wavBytes);
                        return ResultMsg.Success(wavFileName);
                    }
                }
                else
                {
                    return ResultMsg.Error($"BaiduTTS短文本转语音HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"BaiduTTS短文本转语音出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg LongTTS(string text)
        {
            ResultMsg resultResultMsg = new ResultMsg();

            ResultMsg createResultMsg = CreateLongTTS(text);
            if (createResultMsg.StatusCode == StatusCode.SUCCESS) //创建长文本转语音任务成功
            {
                bool flag = true;
                while (flag)
                {
                    Thread.Sleep(1000);
                    ResultMsg queryResultMsg = QueryLongTTS(createResultMsg.Data.ToString());
                    if (queryResultMsg.StatusCode == StatusCode.SUCCESS) // 长文本转换语音完成，下载文件
                    {
                        string wavFileName = Guid.NewGuid().ToString() + ".wav"; //创建音频文件文件名
                        ResultMsg downloadFileResultMsg = HttpUtils.DownloadFile(queryResultMsg.Data.ToString(), Path.Combine(LocalFileDictionary, wavFileName));

                        if (downloadFileResultMsg.StatusCode == StatusCode.SUCCESS)
                        {
                            OnSendMsgEvent(ResultMsg.Success($"音频文件下载成功，文件名：{wavFileName}"));
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"音频文件下载错误，错误信息：{downloadFileResultMsg.Msg}"));
                        }
                        resultResultMsg = downloadFileResultMsg;
                        flag = false;
                    }
                    else if (queryResultMsg.StatusCode == StatusCode.ERROR)
                    {

                        resultResultMsg = ResultMsg.Error("查询Baidu Long TTS任务失败，结束长文本转语音");
                        flag = false;
                    }
                }
                return resultResultMsg;
            }
            else
            {
                return ResultMsg.Error("创建Baidu Long TTS任务失败，结束长文本转语音");
            }
        }

        private ResultMsg CreateLongTTS(string text)
        {
            //DebugMessage(string.Format("文心AI应答接口返回文字信息长度:{0}，大于于500，采用长文本转语音", asrResult.Length));

            string createLongTTSUrl = $"https://aip.baidubce.com/rpc/2.0/tts/v1/create?access_token={accessToken}";
            try
            {
                JObject paramJObject = new JObject
                {
                    {"text",  text},
                    {"format",  "wav"},
                    {"voice", 0},
                    {"lang", "zh"}
                };

                var client = new RestClient();
                var request = new RestRequest(createLongTTSUrl, Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(paramJObject.ToString());

                var response = client.Execute(request);

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info($"创建Baidu Long TTS任务,结果信息:{response.Content}"));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("error_code"))
                    {
                        if (resultJObject.ContainsKey("error_msg"))
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Baidu Create Long TTS任务错误,错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}"));
                            return ResultMsg.Error($"Baidu Create Long TTS任务错误,错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Baidu Create Long TTS任务错误,错误码:{resultJObject["error_code"]}"));
                            return ResultMsg.Error($"Baidu Create Long TTS任务错误,错误码:{resultJObject["error_code"]}");
                        }
                    }
                    else
                    {
                        if (resultJObject["task_status"].ToString() == "Created")
                        {
                            OnSendMsgEvent(ResultMsg.Success($"Baidu Create Long TTS任务成功,task id:{resultJObject["task_id"]}"));
                            return ResultMsg.Success(resultJObject["task_id"].ToString());
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("未添加Baidu Long TTS任务，请检查返回信息"));
                            return ResultMsg.Error("未添加Baidu Long TTS任务，请检查返回信息");
                        }
                    }
                }
                else
                {
                    OnSendMsgEvent(ResultMsg.Error($"Baidu Create Long TTS文本转语音HTTP请求出现错误，response.StatusCode：{response.StatusCode}"));
                    return ResultMsg.Error($"Baidu Create Long TTS文本转语音HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu Create Long TTS文本转语音请求出现异常，异常信息：{ex.Message}"));
                return ResultMsg.Error($"Baidu Create Long TTS文本转语音出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg QueryLongTTS(string taskId)
        {
            string queryLongTTSUrl = $"https://aip.baidubce.com/rpc/2.0/tts/v1/query?access_token={accessToken}";
            try
            {
                JObject paramJObject = new JObject
                {
                    {"task_ids", JArray.FromObject(new List<string>() { taskId })}
                };

                var client = new RestClient();
                var request = new RestRequest(queryLongTTSUrl, Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(paramJObject.ToString());

                var response = client.Execute(request);

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info($"获取Baidu Long TTS结果,结果信息:{response.Content}"));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("error_code"))
                    {
                        if (resultJObject.ContainsKey("error_msg"))
                        {
                            OnSendMsgEvent(ResultMsg.Error($"获取Baidu Long TTS结果错误,错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}"));
                            return ResultMsg.Error($"获取Baidu Long TTS结果错误,错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"获取Baidu Long TTS结果错误,错误码:{resultJObject["error_code"]}"));
                            return ResultMsg.Error($"获取Baidu Long TTS结果错误,错误码:{resultJObject["error_code"]}");
                        }
                    }
                    else
                    {
                        JObject taskinfo = (JObject)((JArray)resultJObject["tasks_info"])[0];

                        if (taskinfo["task_status"].ToString() == "Success")
                        {
                            OnSendMsgEvent(ResultMsg.Success($"Baidu Long TTS生成成功,音频地址:{taskinfo["task_result"]["speech_url"]}"));
                            return ResultMsg.Success(taskinfo["task_result"]["speech_url"].ToString());
                        }
                        else if (taskinfo["task_status"].ToString() == "Running")
                        {
                            OnSendMsgEvent(ResultMsg.Info($"Baidu Long TTS正在生成中，持续等待"));
                            return ResultMsg.Info($"Baidu Long TTS正在生成中，持续等待");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Baidu Long TTS生成失败"));
                            return ResultMsg.Error($"Baidu Long TTS生成失败");
                        }
                    }

                }
                else
                {
                    OnSendMsgEvent(ResultMsg.Error($"Baidu Quert TTS文本转语音HTTP请求出现错误，response.StatusCode：{response.StatusCode}"));
                    return ResultMsg.Error($"Baidu Quert TTS文本转语音HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu Long TTS文本转语音请求出现异常，异常信息：{ex.Message}"));
                return ResultMsg.Error($"BaiduTTS短文本转语音出现异常，异常信息：{ex.Message}");
            }
        }
    }
}
