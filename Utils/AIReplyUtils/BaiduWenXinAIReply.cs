using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VitualPersonSpeech.Model;

namespace VitualPersonSpeech.Utils.AIReplyUtils
{
    class BaiduWenXinAIReply
    {
        // AI应答
        // 百度文心一言接口appkey和secret
        private int appId = 35271065;
        private string apiKey = "yfHH6GDV8g8ETcDyFKHMIbfN";
        private string secretKey = "NGKZeanmHF41q0yPtjufqyqWx3ma3cNK";

        public BaiduWenXinAIReply()
        {

        }

        private string accessToken = "";
        private List<WenXinMessage> messages = new List<WenXinMessage>();

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
                OnSendMsgEvent(ResultMsg.Info($"Baidu WenXin接口获取accesstoken请求，结果:{responseBody}"));
                JObject resultJObject = JObject.Parse(responseBody);

                if (resultJObject.ContainsKey("error"))
                {
                    OnSendMsgEvent(ResultMsg.Error($"Baidu WenXin接口获取accesstoken失败,失败信息:{resultJObject["error_description"].ToString()}"));
                }
                else
                {
                    accessToken = resultJObject["access_token"].ToString();
                    OnSendMsgEvent(ResultMsg.Success($"Baidu WenXin接口获取accesstoken成功:{accessToken}"));
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu Wenxin接口获取accesstoken出现异常，异常信息:{ex.Message}"));
            }
        }

        public async Task<ResultMsg> Chat(string askStr)
        {
            string url = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/eb-instant?access_token={accessToken}";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (messages.Count >= 10)
                    {
                        messages.RemoveRange(0, 2);
                    }
                    messages.Add(new WenXinMessage("user", askStr));

                    JObject paramJObject = new JObject
                    {
                        {"messages", JArray.FromObject(messages)},
                        {"stream", false},
                        {"user_id", "suzhouxunishuziren"}

                    };

                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        OnSendMsgEvent(ResultMsg.Info($"Baidu Wenxin AI应答结果,结果信息:{responseBody}"));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

                        if (resultJObject.ContainsKey("error_code"))
                        {
                            if (resultJObject.ContainsKey("error_msg"))
                            {
                                OnSendMsgEvent(ResultMsg.Error($"Baidu Wenxin AI应答结果错误,错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}"));
                                messages.RemoveAt(messages.Count - 1);
                                return ResultMsg.Error($"Baidu Wenxin AI应答结果错误,错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}");
                            }
                            else
                            {
                                OnSendMsgEvent(ResultMsg.Error($"Baidu Wenxin AI应答结果错误,错误码:{resultJObject["error_code"]}"));
                                messages.RemoveAt(messages.Count - 1);
                                return ResultMsg.Error($"Baidu Wenxin AI应答结果错误,错误码:{resultJObject["error_code"]}");
                            }
                        }
                        else
                        {
                            WenXinResult wenXinResult = resultJObject.ToObject<WenXinResult>();
                            messages.Add(new WenXinMessage("assistant", wenXinResult.result));

                            OnSendMsgEvent(ResultMsg.Success($"Baidu Wenxin AI应答结果成功：{wenXinResult.result}"));
                            return ResultMsg.Success(wenXinResult.result);
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error($"Baidu Wenxin AI应答HTTP请求出现错误，response.StatusCode：{response.StatusCode}"));
                        return ResultMsg.Error($"Baidu Wenxin AI应答HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                    }

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
