using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VitualPersonSpeech.Model;

namespace VitualPersonSpeech.Utils.AIImgUtils
{
    class BaiduAIImg
    {
        public List<string> QAAIImgPatternList { get; set; } = new List<string> // ai作画正则匹配规则：做***图 作***图 画***图    做***画 作***画 画***画    做一张 作一张 画一张    做一幅 作一幅 画一幅
        {
             ".*做.*图.*", ".*作.*图.*", ".*画.*图.*",
             ".*做.*画.*", ".*作.*画.*", ".*画.*画.*",
             ".*做一张.*", ".*作一张.*", ".*画一张.*",
             ".*做一幅.*", ".*作一幅.*", ".*画一幅.*",
        };

        public BaiduAIImg(List<string> qaAIImgPatternList)
        {
            QAAIImgPatternList = qaAIImgPatternList;
        }

        private const int appId = 36763656;
        private const string apiKey = "0YSOMT5OGK1GtjWc3lsg1GML";
        private const string secretKey = "XkowHDaRjCULMTvdgdzoIXP543GYahwB";
        private int imgWidth = 1024;
        private int imgHeight = 1024;
        private string accessToken = "";// baidu AI作画AccessToken

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

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info($"Baidu AI Img接口获取accesstoken请求，结果:{response.Content}"));
                    JObject resultJObject = JObject.Parse(response.Content);

                    if (resultJObject.ContainsKey("error"))
                    {
                        OnSendMsgEvent(ResultMsg.Error($"Baidu AI Img接口获取accesstoken失败,失败信息:{resultJObject["error_description"]}"));
                    }
                    else
                    {
                        accessToken = resultJObject["access_token"].ToString();
                        OnSendMsgEvent(ResultMsg.Success($"Baidu AI Img接口获取accesstoken成功:{accessToken}"));
                    }
                }
                else
                {
                    OnSendMsgEvent(ResultMsg.Error($"Baidu AI Img接口获取accesstoken请求出现错误，response.StatusCode：{response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Baidu AI Img接口获取accesstoken出现异常，异常信息:{ex.Message}"));
            }
        }

        public bool MatchAIImg(string questionStr)
        {
            //做***图 作***图 画***图    做***画 作***画 画***画    做一张 作一张 画一张    做一幅 作一幅 画一幅
            for (int i = 0; i < QAAIImgPatternList.Count; i++)
            {
                if (Regex.IsMatch(questionStr, QAAIImgPatternList[i]))
                {
                    OnSendMsgEvent(ResultMsg.Success($"语句与AI作画正则表达式匹配成功，问题语句：{questionStr}, 正则规则：{QAAIImgPatternList[i]}"));
                    return true;
                }
            }
            return false;
        }

        public ResultMsg Text2Img(string txt)
        {
            ResultMsg resultResultMsg = new ResultMsg();

            ResultMsg createResultMsg = CreateTxt2Img(txt);
            if (createResultMsg.StatusCode == StatusCode.SUCCESS)
            {
                bool flag = true;
                while (flag)
                {
                    Thread.Sleep(1000);
                    ResultMsg queryResultMsg = GetTxt2Img(createResultMsg.Data.ToString());
                    if (queryResultMsg.StatusCode == StatusCode.SUCCESS || queryResultMsg.StatusCode == StatusCode.ERROR)
                    {
                        resultResultMsg = queryResultMsg;
                        flag = false;
                    }
                }
                return resultResultMsg;
            }
            else
            {
                return ResultMsg.Error("创建Baidu AI 作画任务失败，结束AI作画");
            }
        }

        private ResultMsg CreateTxt2Img(string txt)
        {
            string url = $"https://aip.baidubce.com/rpc/2.0/ernievilg/v1/txt2imgv2?access_token={accessToken}";
            JObject paramJObject = new JObject
            {
                {  "prompt", txt + ",写实"},
                {  "width", imgWidth},
                {  "height", imgHeight}
            };

            try
            {
                var client = new RestClient();
                var request = new RestRequest(url, Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(paramJObject.ToString());

                var response = client.Execute(request);

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info("Baidu AI作画添加作画任务请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("error_code"))
                    {
                        if (resultJObject.ContainsKey("error_msg"))
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Baidu AI作画添加作画任务请求，请求错误，错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}"));
                            return ResultMsg.Error($"Baidu AI作画添加作画任务请求，请求错误，错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_msg"]}");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Baidu AI作画添加作画任务请求，请求错误，错误码:{resultJObject["error_code"]}"));
                            return ResultMsg.Error($"Baidu AI作画添加作画任务请求，请求错误，错误码:{resultJObject["error_code"]}");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Success($"Baidu AI作画添加作画任务请求，请求成功，任务id:{resultJObject["data"]["task_id"]}"));
                        return ResultMsg.Success(resultJObject["data"]["task_id"]);
                    }
                }
                else
                {
                    OnSendMsgEvent(ResultMsg.Error($"Baidu AI作画添加作画任务请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}"));
                    return ResultMsg.Error($"Baidu AI作画添加作画任务请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Baidu AI作画添加作画任务请求出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg GetTxt2Img(string taskId)
        {
            string url = $"https://aip.baidubce.com/rpc/2.0/ernievilg/v1/getImgv2?access_token={accessToken}";
            JObject paramJObject = new JObject
            {
                {"task_id", taskId}
            };

            try
            {
                var client = new RestClient();
                var request = new RestRequest(url, Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddJsonBody(paramJObject.ToString());

                var response = client.Execute(request);

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info("Baidu AI作画获取作画进度请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("error_code"))
                    {
                        if (resultJObject.ContainsKey("error_description"))
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Baidu AI作画获取作画进度请求，请求错误，错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_description"]}"));
                            return ResultMsg.Error($"Baidu AI作画获取作画进度请求，请求错误，错误码:{resultJObject["error_code"]}, 错误信息:{resultJObject["error_description"]}");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Baidu AI作画获取作画进度请求，请求错误，错误码:{resultJObject["error_code"]}"));
                            return ResultMsg.Error($"Baidu AI作画获取作画进度请求，请求错误，错误码:{resultJObject["error_code"]}");
                        }
                    }
                    else
                    {
                        BaiduAIImgResult baiduAIImgResult = resultJObject.ToObject<BaiduAIImgResult>();
                        if (baiduAIImgResult.data.task_status == "SUCCESS")
                        {
                            OnSendMsgEvent(ResultMsg.Success($"Baidu AI作画获取作画进度请求，作画完成，画作地址:{baiduAIImgResult.data.sub_task_result_list[0].final_image_list[0].img_url}"));
                            return ResultMsg.Success(baiduAIImgResult.data.sub_task_result_list[0].final_image_list[0].img_url);
                        }
                        else if (baiduAIImgResult.data.task_status == "FAILED")
                        {
                            OnSendMsgEvent(ResultMsg.Error("Baidu AI作画获取作画进度请求，作画失败"));
                            return ResultMsg.Error("Baidu AI作画获取作画进度请求，作画失败");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Info("Baidu AI作画获取作画进度请求，正在作画"));
                            return ResultMsg.Info("Baidu AI作画获取作画进度请求，正在作画");
                        }
                    }
                }
                else
                {
                    return ResultMsg.Error($"Baidu AI作画获取作画进度请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Baidu AI作画获取作画进度请求出现异常，异常信息：{ex.Message}");
            }
        }
    }
}
