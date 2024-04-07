using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VitualPersonSpeech.Utils
{
    public class Audio2FaceUtils
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static string audio2FaceServerAddress = "";
        private static string audio2FacePlayer = "/World/audio2face/Player";
        private static string audio2FaceStreamLiveLinkNode = "/World/audio2face/StreamLivelink";

        public static void InitSettings(string Audio2FaceServerAddress)
        {
            audio2FaceServerAddress = Audio2FaceServerAddress;
        }

        // audio2face接口直接调用
        public static bool GetAudio2FaceServerStatus()
        {
            string url = audio2FaceServerAddress + "/status";
            string result = HttpUtils.Get(url);
            
            if (result != null && result.Contains("OK"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool LoadAudio2FaceUSD(string usdFile)
        {
            string url = audio2FaceServerAddress + "/A2F/USD/Load";

            JObject paramJObject = new JObject
            {
                {"file_name", usdFile}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face加载USD文件请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face加载USD文件请求成功");
                    return true;
                }
                else
                {
                    log.Info("Audio2Face加载USD文件请求失败");
                    return false;
                }
            }
            return false;
        }

        public static bool SetAudio2FacePlayerTrack(string fileName)
        {
            string url = audio2FaceServerAddress + "/A2F/Player/SetTrack";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer},
                {"file_name", fileName}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face Player设置Track： "+ fileName +" 请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face Player设置Track:"+ fileName +" 请求成功");
                    return true;
                }
                else
                {
                    log.Info("Audio2Face Player设置Track:" + fileName + " 请求失败");
                    return false;
                }
            }
            return false;
        }

        public static int GetAudio2FacePlayerRange() 
        {
            string url = audio2FaceServerAddress + "/A2F/Player/GetRange";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face Player获取range请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face Player获取range请求，请求成功");
                    int length = int.Parse(((JArray)resultJObject["result"]["work"])[2].ToString());
                    return length;
                }
                else
                {
                    log.Info("Audio2Face Player获取range请求，请求失败");
                    return -1;
                }
            }
            return -1;
        }

        public static int GetAudio2FacePlayerTime()
        {
            string url = audio2FaceServerAddress + "/A2F/Player/GetTime";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face Player获取time请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face Player获取time请求，请求成功");
                    int length = int.Parse(resultJObject["result"].ToString());
                    return length;
                }
                else
                {
                    log.Info("Audio2Face Player获取time请求，请求失败");
                    return -1;
                }
            }
            return -1;
        }

        public static bool SetAudio2FacePlayerTime(int time)
        {
            string url = audio2FaceServerAddress + "/A2F/Player/SetTime";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer},
                {"time", time}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face Player设置当前时间: " + time + " 请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face Player设置当前时间: " + time + " 请求，请求成功");
                    return true;
                }
                else
                {
                    log.Info("Audio2Face Player设置当前时间: " + time + " 请求，请求失败");
                    return false;
                }
            }
            return false;
        }

        public static bool ControlAudio2FacePlayerPlay()
        {
            string url = audio2FaceServerAddress + "/A2F/Player/Play";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face Player控制播放请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face Player控制播放请求，请求成功");
                    return true;  
                }
                else
                {
                    log.Info("Audio2Face Player控制播放请求，请求失败");
                    return false;
                }
            }
            return false;
        }

        public static bool ControlAudio2FacePlayerPause()
        {
            string url = audio2FaceServerAddress + "/A2F/Player/Pause";

            JObject paramJObject = new JObject
            {
                {"a2f_player", audio2FacePlayer}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face Player控制暂停请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face Player控制暂停请求，请求成功");
                    return true;
                }
                else
                {
                    log.Info("Audio2Face Player控制暂停请求，请求失败");
                    return false;
                }
            }
            return false;
        }
 
        public static bool ActivateAudio2FaceExporterStreamLiveLink()
        {
            string url = audio2FaceServerAddress + "/A2F/Exporter/ActivateStreamLivelink";

            JObject paramJObject = new JObject
            {
                {"node_path", audio2FaceStreamLiveLinkNode},
                {"value", true}
            };
            string result = HttpUtils.Post(url, paramJObject.ToString(), null);

            log.Info("Audio2Face Exporter激活Stream Live Link请求，请求结果：" + result);

            JObject resultJObject = (JObject)JsonConvert.DeserializeObject(result);

            if (resultJObject.ContainsKey("status"))
            {
                if (resultJObject["status"].ToString() == "OK")
                {
                    log.Info("Audio2Face Exporter激活Stream Live Link请求，请求成功");
                    return true;
                }
                else
                {
                    log.Info("Audio2Face Exporter激活Stream Live Link请求，请求失败");
                    return false;
                }
            }
            return false;
        }
    }
}
