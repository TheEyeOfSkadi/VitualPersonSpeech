using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using VitualPersonSpeech.Model;

namespace VitualPersonSpeech.Utils.AIReplyUtils
{
    class BaiduWenXinAIReply
    {
        // AI应答
        public BaiduWenXinAIReply()
        {

        }

        // 百度文心一言接口appkey和secret
        private string apiKey = "yfHH6GDV8g8ETcDyFKHMIbfN";
        private string secretKey = "NGKZeanmHF41q0yPtjufqyqWx3ma3cNK";
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

        public void GetAccessToken()
        {
            string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";
            try
            {
                var client = new RestClient();
                var request = new RestRequest(url, Method.Post);

                var response = client.Execute(request);

                if(response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info($"Baidu WenXin接口获取accesstoken请求，结果:{response.Content}"));
                    JObject resultJObject = JObject.Parse(response.Content);

                    if (resultJObject.ContainsKey("error"))
                    {
                        OnSendMsgEvent(ResultMsg.Error($"Baidu WenXin接口获取accesstoken失败,失败信息:{resultJObject["error_description"]}"));
                    }
                    else
                    {
                        accessToken = resultJObject["access_token"].ToString();
                        OnSendMsgEvent(ResultMsg.Success($"Baidu WenXin接口获取accesstoken成功:{accessToken}"));
                    }
                }
                else
                {
                    OnSendMsgEvent(ResultMsg.Error($"Baidu WenXin接口获取accesstoke请求出现错误，response.StatusCode：{response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu Wenxin接口获取accesstoken出现异常，异常信息:{ex.Message}"));
            }
        }

        public ResultMsg Chat_ERNIE_3D5_8K(string askStr)
        {
            string url = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/completions?access_token={accessToken}";
            try
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
                    {"user_id", "suzhouxunishuziren"},
                     {"max_output_tokens", 150 }

                };

                var client = new RestClient();
                var request = new RestRequest(url, Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(paramJObject.ToString());

                var response = client.Execute(request);

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info($"Baidu Wenxin AI应答结果,结果信息:{response.Content}"));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

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
                        WenXinResult_ERNIE_Lite_8K_0922 wenXinResult = resultJObject.ToObject<WenXinResult_ERNIE_Lite_8K_0922>();
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
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu Long TTS文本转语音请求出现异常，异常信息：{ex.Message}"));
                return ResultMsg.Error($"BaiduTTS短文本转语音出现异常，异常信息：{ex.Message}");
            }
        }

        public ResultMsg Chat_ERNIE_Lite_8K_0922(string askStr)
        {
            string url = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/eb-instant?access_token={accessToken}";
            try
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
                    {"user_id", "suzhouxunishuziren"},
                    {"max_output_tokens", 80 }

                };

                var client = new RestClient();
                var request = new RestRequest(url, Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(paramJObject.ToString());

                var response = client.Execute(request);

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info($"Baidu Wenxin AI应答结果,结果信息:{response.Content}"));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

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
                        WenXinResult_ERNIE_Lite_8K_0922 wenXinResult = resultJObject.ToObject<WenXinResult_ERNIE_Lite_8K_0922>();
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
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu Long TTS文本转语音请求出现异常，异常信息：{ex.Message}"));
                return ResultMsg.Error($"BaiduTTS短文本转语音出现异常，异常信息：{ex.Message}");
            }
        }
    }
}
