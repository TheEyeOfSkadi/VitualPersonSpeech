using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using VitualPersonSpeech.Model;
using RestSharp;
using System.Threading;

namespace VitualPersonSpeech.Utils.AudioToFaceUtils
{
    public class NvAuido2Face
    {
        public string ServerAddress { get; set; } = "http://192.168.3.197:8011";
        public string USDFile { get; set; } = "C:\\wav\\UE5MetaHuman.usd";
        public string WavFile { get; set; } = "C:\\wav\\default.wav";

        public NvAuido2Face(string serverAddress, string usdFile, string wavFile)
        {
            ServerAddress = serverAddress;
            USDFile = usdFile;
            WavFile = wavFile;
        }

        private string audio2FacePlayer = "/World/audio2face/Player";
        private string audio2FaceStreamLiveLinkNode = "/World/audio2face/StreamLivelink";

        // 发送消息委托  
        public delegate void SendMsgDelegate(ResultMsg resultMsg);
        public SendMsgDelegate SendMsgEvent;

        // 触发发送消息委托的方法  
        protected virtual void OnSendMsgEvent(ResultMsg resultMsg)
        {
            SendMsgEvent?.Invoke(resultMsg); // 安全地调用委托，避免空引用异常  
        }

        public ResultMsg InitServer()
        {
            ResultMsg serverStatusResultMsg = GetServerStatus();

            if (serverStatusResultMsg.StatusCode == StatusCode.SUCCESS)
            {
                Thread.Sleep(5000);

                OnSendMsgEvent(ResultMsg.Success("Nv Audi2Face 获取状态成功，加载USD File"));
                ResultMsg loadUSDResultMsg = LoadUSD(USDFile);

                if (loadUSDResultMsg.StatusCode == StatusCode.SUCCESS)
                {
                    Thread.Sleep(5000);

                    OnSendMsgEvent(ResultMsg.Success("Nv Audi2Face 加载 USD文件成功，激活StreamLive"));
                    ResultMsg activateStreamLiveLinkResultMsg = ActivateStreamLiveLink();

                    if(activateStreamLiveLinkResultMsg.StatusCode == StatusCode.SUCCESS)
                    {
                        OnSendMsgEvent(ResultMsg.Success("Nv Audi2Face 激活StreamLive成功，完成Nv Audio2Face所有设置"));
                        return ResultMsg.Success("Nv Audi2Face 初始化设置成功");
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audi2Face 激活StreamLive失败"));
                        return ResultMsg.Error("Nv Audi2Face 激活StreamLive失败");
                    }
                }
                else
                {
                    OnSendMsgEvent(ResultMsg.Error("Nv Audi2Face 加载 USD文件失败，不激活StreamLive"));
                    return ResultMsg.Error("Nv Audi2Face 加载 USD文件失败，不激活StreamLive");
                }
            }
            else
            {
                OnSendMsgEvent(ResultMsg.Error("Nv Audi2Face 获取状态失败，不加载USD File"));
                return ResultMsg.Error("Nv Audi2Face 获取状态失败，不加载USD File");
            }
        }

        public void Play()
        {
            SetPlayerPlay();
        }

        public void Pause()
        {
            SetPlayerPause();
        }

        public void Mute()
        {
            SetPlayerTrack("mute.wav");
        }

        public void SayWelcome()
        {
            if (new Random().Next(2) == 0)
            {
                SetPlayerTrack("nihao.wav");
            }
            else
            {
                SetPlayerTrack("wozai.wav");
            }
            SetPlayerPlay();
        }

        public void SayApiError()
        {
            SetPlayerTrack("apiErrpr.wav");
            SetPlayerPlay();
        }

        public  void SayNoVoice()
        {
            SetPlayerTrack("noVoice.wav");
            SetPlayerPlay();
        }

        public void SayInAIReply()
        {
            SetPlayerTrack("inAIReply.wav");// 正在ai推理回答中
            SetPlayerPlay();
        }

        public void SayInAIImg()
        {
            SetPlayerTrack("inAIImg.wav");// 正在ai作画中
            SetPlayerPlay();
        }

        public void SayWav(string wavName)
        {
            SetPlayerTrack(wavName);
            SetPlayerPlay();
        }

        public void LoadUSD()
        {
            LoadUSD(USDFile);
        }

        public void SetDefaultWav()
        {
            SetPlayerTrack(WavFile);
        }

        public void ActicateStreamLivelink()
        {
            ActivateStreamLiveLink();
        }

        // audio2face接口直接调用
        private ResultMsg GetServerStatus()
        {
            string url = ServerAddress + "/status";

            try
            {
                var client = new RestClient();
                var request = new RestRequest(url, Method.Get);

                var response = client.Execute(request);

                if (response.IsSuccessStatusCode)
                {
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取状态请求，结果：" + response.Content));

                    if (response.Content != null && response.Content.Contains("OK"))
                    {
                        return ResultMsg.Success("Nv Audio2Face 获取状态成功，服务在线");
                    }
                    else
                    {
                        return ResultMsg.Error("Nv Audio2Face 获取状态错误，服务离线");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face 获取状态HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取状态出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg LoadUSD(string usdFile)
        {
            string url = ServerAddress + "/A2F/USD/Load";

            JObject paramJObject = new JObject
            {
                {"file_name", usdFile}
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
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 加载USD文件请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("status"))
                    {
                        if (resultJObject["status"].ToString() == "OK")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Nv Audio2Face 加载USD文件请求成功"));
                            return ResultMsg.Success("Nv Audio2Face 加载USD文件请求成功");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 加载USD文件请求失败"));
                            return ResultMsg.Error("Nv Audio2Face 加载USD文件请求失败");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 加载USD文件请求结果中不包含 status,数据错误"));
                        return ResultMsg.Error("Nv Audio2Face 加载USD文件请求结果中不包含 status,数据错误");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face 加载USD文件HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 加载USD文件出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg SetPlayerTrack(string wavfileName)
        {
            string url = ServerAddress + "/A2F/Player/SetTrack";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer},
                {"file_name", wavfileName}
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
                    OnSendMsgEvent(ResultMsg.Info($"Nv Audio2Face 设置Track：{wavfileName} 请求，结果：" + response.Content));

                    if (response.Content != null && response.Content.Contains("OK"))
                    {
                        return ResultMsg.Success($"Nv Audio2Face 设置Track:{wavfileName}，请求成功");
                    }
                    else
                    {
                        return ResultMsg.Error($"Nv Audio2Face 设置Track:{wavfileName}，请求失败");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face 设置Track:{wavfileName} HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 设置Track:{wavfileName} 出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg GetPlayerRange()
        {
            string url = ServerAddress + "/A2F/Player/GetRange";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
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
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取range请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("status"))
                    {
                        if (resultJObject["status"].ToString() == "OK")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Nv Audio2Face 获取range请求，请求成功"));
                            return ResultMsg.Success("Nv Audio2Face 获取range请求，请求成功");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 获取range请求，请求失败"));
                            return ResultMsg.Error("Nv Audio2Face 获取range请求，请求失败");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 获取range请求结果中不包含 status,数据错误"));
                        return ResultMsg.Error("Nv Audio2Face 获取range请求结果中不包含 status,数据错误");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face 获取range请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                } 
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取range请求出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg GetPlayerTime()
        {
            string url = ServerAddress + "/A2F/Player/GetTime";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
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
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取time请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("status"))
                    {
                        if (resultJObject["status"].ToString() == "OK")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Nv Audio2Face 获取time请求，请求成功"));
                            return ResultMsg.Success("Nv Audio2Face 获取time请求，请求成功");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 获取time请求，请求失败"));
                            return ResultMsg.Error("Nv Audio2Face 获取time请求，请求失败");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 获取time请求结果中不包含 status,数据错误"));
                        return ResultMsg.Error("Nv Audio2Face 获取time请求结果中不包含 status,数据错误");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face 获取time请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取time请求出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg SetPlayerTime(int time)
        {
            string url = ServerAddress + "/A2F/Player/SetTime";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer},
                {"time", time}
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
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取time请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("status"))
                    {
                        if (resultJObject["status"].ToString() == "OK")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Nv Audio2Face 获取time请求，请求成功"));
                            return ResultMsg.Success("Nv Audio2Face 获取time请求，请求成功");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 获取time请求，请求失败"));
                            return ResultMsg.Error("Nv Audio2Face 获取time请求，请求失败");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face 获取time请求结果中不包含 status,数据错误"));
                        return ResultMsg.Error("Nv Audio2Face 获取time请求结果中不包含 status,数据错误");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face 获取time请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取time请求出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg SetPlayerPlay()
        {
            string url = ServerAddress + "/A2F/Player/Play";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
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
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face Player Set Play请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("status"))
                    {
                        if (resultJObject["status"].ToString() == "OK")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Nv Audio2Face Player Set Play请求，请求成功"));
                            return ResultMsg.Success("Nv Audio2Face Player Set Play请求，请求成功");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face Player Set Play请求，请求失败"));
                            return ResultMsg.Error("Nv Audio2Face Player Set Play请求，请求失败");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face Player Set Play请求结果中不包含 status,数据错误"));
                        return ResultMsg.Error("Nv Audio2Face Player Set Play请求结果中不包含 status,数据错误");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face Player Set Play请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face Player Set Play请求出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg SetPlayerPause()
        {
            string url = ServerAddress + "/A2F/Player/Pause";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
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
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face Player Set Pause请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("status"))
                    {
                        if (resultJObject["status"].ToString() == "OK")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Nv Audio2Face Player Set Pause请求，请求成功"));
                            return ResultMsg.Success("Nv Audio2Face Player Set Pause请求，请求成功");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face Player Set Pause请求，请求失败"));
                            return ResultMsg.Error("Nv Audio2Face Player Set Pause请求，请求失败");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face Player Set Pause请求结果中不包含 status,数据错误"));
                        return ResultMsg.Error("Nv Audio2Face Player Set Pause请求结果中不包含 status,数据错误");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face Player Set Pause请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face Player Set Pause请求出现异常，异常信息：{ex.Message}");
            }
        }

        private ResultMsg ActivateStreamLiveLink()
        {
            string url = ServerAddress + "/A2F/Exporter/ActivateStreamLivelink";

            JObject paramJObject = new JObject
            {
                {"node_path", audio2FaceStreamLiveLinkNode},
                {"value", true}
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
                    OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face Activate Stream LiveLink请求，结果：" + response.Content));

                    JObject resultJObject = (JObject)JsonConvert.DeserializeObject(response.Content);

                    if (resultJObject.ContainsKey("status"))
                    {
                        if (resultJObject["status"].ToString() == "OK")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Nv Audio2Face Activate Stream LiveLink请求，请求成功"));
                            return ResultMsg.Success("Nv Audio2Face Activate Stream LiveLink请求，请求成功");
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face Activate Stream LiveLink请求，请求失败"));
                            return ResultMsg.Error("Nv Audio2Face Activate Stream LiveLink请求，请求失败");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error("Nv Audio2Face Activate Stream LiveLink请求结果中不包含 status,数据错误"));
                        return ResultMsg.Error("Nv Audio2Face Activate Stream LiveLink请求结果中不包含 status,数据错误");
                    }
                }
                else
                {
                    return ResultMsg.Error($"Nv Audio2Face Activate Stream LiveLink请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face Activate Stream LiveLink请求出现异常，异常信息：{ex.Message}");
            }
        }
    }
}
