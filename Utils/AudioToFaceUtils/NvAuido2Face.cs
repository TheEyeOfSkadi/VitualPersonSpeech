using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitualPersonSpeech.Model;
using System.Net.Http;

namespace VitualPersonSpeech.Utils.AudioToFaceUtils
{
    public class NvAuido2Face
    {
        private string _serverAddress = "http://192.168.3.197:8011";
        private string _usdFile = "C:\\wav\\UE5MetaHuman.usd";
        private string _wavFile = "C:\\wav\\default.wav";

        public string ServerAddress
        {
            get => _serverAddress;
            set => _serverAddress = value;
        }
        public string UsdFile 
        { 
            get => _usdFile; 
            set => _usdFile = value; 
        }
        public string WavFile 
        { 
            get => _wavFile; 
            set => _wavFile = value; 
        }

        public NvAuido2Face(string serverAddress, string usdFile, string wavFile)
        {
            ServerAddress = serverAddress;
            UsdFile = usdFile;
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

        public async Task<ResultMsg> InitServer()
        {
            ResultMsg serverStatusResultMsg = await GetServerStatus();

            if (serverStatusResultMsg.StatusCode == StatusCode.SUCCESS)
            {
                await Task.Delay(5000);

                OnSendMsgEvent(ResultMsg.Success("Nv Audi2Face 获取状态成功，加载USD File"));
                ResultMsg loadUSDResultMsg = await LoadUSD(UsdFile);

                if (loadUSDResultMsg.StatusCode == StatusCode.SUCCESS)
                {
                    await Task.Delay(5000);

                    OnSendMsgEvent(ResultMsg.Success("Nv Audi2Face 加载 USD文件成功，激活StreamLive"));
                    ResultMsg activateStreamLiveLinkResultMsg = await ActivateStreamLiveLink();

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

        public async void Play()
        {
            await SetPlayerPlay();
        }

        public async void Pause()
        {
            await SetPlayerPause();
        }

        public async void Mute()
        {
            await SetPlayerTrack("mute.wav");
        }

        public async void SayWelcome()
        {
            if (new Random().Next(2) == 0)
            {
                await SetPlayerTrack("nihao.wav");
            }
            else
            {
                await SetPlayerTrack("wozai.wav");
            }
            await SetPlayerPlay();
        }

        public async void SayApiError()
        {
            await SetPlayerTrack("apiErrpr.wav");
            await SetPlayerPlay();
        }

        public async void SayNoVoice()
        {
            await SetPlayerTrack("noVoice.wav");
            await SetPlayerPlay();
        }

        public async void SayInAIReply()
        {
            await SetPlayerTrack("inAIReply.wav");// 正在ai推理回答中
            await SetPlayerPlay();
        }

        public async void SayInAIImg()
        {
            await SetPlayerTrack("inAIImg.wav");// 正在ai作画中
            await SetPlayerPlay();
        }

        public async void SayWav(string wavName)
        {
            await SetPlayerTrack(wavName);
            await SetPlayerPlay();
        }

        public async void LoadUSD()
        {
            await LoadUSD(UsdFile);
        }

        public async void SetDefaultWav()
        {
            await SetPlayerTrack(WavFile);
        }

        public async void ActicateStreamLivelink()
        {
            await ActivateStreamLiveLink();
        }

        // audio2face接口直接调用
        private async Task<ResultMsg> GetServerStatus()
        {
            string url = _serverAddress + "/status";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 发送请求并获取响应  
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取状态请求，结果：" + responseBody));

                        if (responseBody != null && responseBody.Contains("OK"))
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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取状态出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> LoadUSD(string usdFile)
        {
            string url = _serverAddress + "/A2F/USD/Load";

            JObject paramJObject = new JObject
            {
                {"file_name", usdFile}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 加载USD文件请求，结果：" + responseBody));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 加载USD文件出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> SetPlayerTrack(string wavfileName)
        {
            string url = _serverAddress + "/A2F/Player/SetTrack";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer},
                {"file_name", wavfileName}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info($"Nv Audio2Face 设置Track：{wavfileName} 请求，结果：" + responseBody));

                        if (responseBody != null && responseBody.Contains("OK"))
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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 设置Track:{wavfileName} 出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> GetPlayerRange()
        {
            string url = _serverAddress + "/A2F/Player/GetRange";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取range请求，结果：" + responseBody));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取range请求出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> GetPlayerTime()
        {
            string url = _serverAddress + "/A2F/Player/GetTime";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取time请求，结果：" + responseBody));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取time请求出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> SetPlayerTime(int time)
        {
            string url = _serverAddress + "/A2F/Player/SetTime";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer},
                {"time", time}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face 获取time请求，结果：" + responseBody));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face 获取time请求出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> SetPlayerPlay()
        {
            string url = _serverAddress + "/A2F/Player/Play";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face Player Set Play请求，结果：" + responseBody));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face Player Set Play请求出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> SetPlayerPause()
        {
            string url = _serverAddress + "/A2F/Player/Pause";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face Player Set Pause请求，结果：" + responseBody));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face Player Set Pause请求出现异常，异常信息：{ex.Message}");
            }
        }

        private async Task<ResultMsg> ActivateStreamLiveLink()
        {
            string url = _serverAddress + "/A2F/Exporter/ActivateStreamLivelink";

            JObject paramJObject = new JObject
            {
                {"node_path", audio2FaceStreamLiveLinkNode},
                {"value", true}
            };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent httpContent = new StringContent(paramJObject.ToString(), Encoding.UTF8, "application/json");

                    // 发送请求并获取响应  
                    var response = await client.PostAsync(url, httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info("Nv Audio2Face Activate Stream LiveLink请求，结果：" + responseBody));

                        JObject resultJObject = (JObject)JsonConvert.DeserializeObject(responseBody);

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
            }
            catch (Exception ex)
            {
                return ResultMsg.Error($"Nv Audio2Face Activate Stream LiveLink请求出现异常，异常信息：{ex.Message}");
            }
        }
    }
}
