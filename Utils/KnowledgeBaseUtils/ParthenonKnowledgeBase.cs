using MiniExcelLibs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VitualPersonSpeech.Model;

namespace VitualPersonSpeech.Utils.KnowledgeBaseUtils
{
    public class ParthenonKnowledgeBase
    {
        private float _relevanceThreshold = 0.1f; // 权重阈值：要求结果不低于阈值
        private float _similarityThreshold = 0.1f; // 相似度阈值：要求结果不低于阈值
        private string _localFileDictionary = "C:\\wav\\";
        private string _getTokenAPIUrl = "http://192.168.3.99:8082/api/get-token?username={0}&password={1}";
        private string _getAllReplysAndScenesAPIUrl = "http://192.168.3.197:8082/chat-reply/get-all-replys-and-scenes?token={0}";
        private string _similarAPIUrl = "http://192.168.3.99:8082/chat-quest/answer-quest?content={0}&token={1}";
        private string _downloadFileAPIUrl = "http://192.168.3.197:9000/{0}";

        public float RelevanceThreshold 
        { 
            get => _relevanceThreshold; 
            set => _relevanceThreshold = value; 
        }
        public float SimilarityThreshold 
        { 
            get => _similarityThreshold; 
            set => _similarityThreshold = value; 
        }
        public string LocalFileDictionary 
        { 
            get => _localFileDictionary; 
            set => _localFileDictionary = value; 
        }
        public string GetTokenAPIUrl 
        { 
            get => _getTokenAPIUrl; 
            set => _getTokenAPIUrl = value; 
        }
        public string GetAllReplysAndScenesAPIUrl 
        { 
            get => _getAllReplysAndScenesAPIUrl; 
            set => _getAllReplysAndScenesAPIUrl = value; 
        }
        public string SimilarAPIUrl 
        { 
            get => _similarAPIUrl; 
            set => _similarAPIUrl = value; 
        }
        public string DownloadFileAPIUrl 
        { 
            get => _downloadFileAPIUrl; 
            set => _downloadFileAPIUrl = value; 
        }

        public ParthenonKnowledgeBase(float relevanceThreshold, float similarityThreshold, string localFileDictionary, string getTokenAPIUrl, string getAllReplysAndScenesAPIUrl, string similarAPIUrl, string downloadFileAPIUrl)
        {
            RelevanceThreshold = relevanceThreshold;
            SimilarityThreshold = similarityThreshold;
            LocalFileDictionary = localFileDictionary;
            GetTokenAPIUrl = getTokenAPIUrl;
            GetAllReplysAndScenesAPIUrl = getAllReplysAndScenesAPIUrl;
            SimilarAPIUrl = similarAPIUrl;
            DownloadFileAPIUrl = downloadFileAPIUrl;
        }

        private string userName = "1";
        private string password = "1";
        private string accessToken = "";

        private LocalIndustryQAData[] localKnowledgeBaseDatas;

        private Dictionary<int, IndustryQAChatReply> industryQAChatReplyDic = new Dictionary<int, IndustryQAChatReply>();

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
            string url = string.Format(_getTokenAPIUrl, userName, password);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.SendAsync(request);

                    if(response.IsSuccessStatusCode)
                    {
                        // 读取响应内容  
                        string responseBody = await response.Content.ReadAsStringAsync();

                        JObject resultJObject = JObject.Parse(responseBody);

                        if (resultJObject != null && resultJObject.ContainsKey("code"))
                        {
                            if (resultJObject["code"].ToString() == "SUCCESS")
                            {
                                OnSendMsgEvent(ResultMsg.Success($"Parthenon KnowledgeBase 接口获取accesstoken成功,信息:{resultJObject["data"]}"));
                                accessToken = resultJObject["data"].ToString();
                            }
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口获取accesstoken失败,返回结果不含code"));
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口获取accesstoken请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}"));
                    }                    
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase接口获取accesstoken出现异常，异常信息:{ex.Message}"));
            }
        }

        public async Task DownloadAllKnowledgeBaseFile()
        {
            string url = string.Format(_getAllReplysAndScenesAPIUrl, accessToken);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        industryQAChatReplyDic = new Dictionary<int, IndustryQAChatReply>();

                        // 读取响应内容  
                        string responseBody = await response.Content.ReadAsStringAsync();

                        JObject resultJObject = JObject.Parse(responseBody);

                        if (resultJObject != null && resultJObject.ContainsKey("code") && resultJObject["code"].ToString() == "SUCCESS")
                        {
                            List<IndustryQAChatReply> industryQAChatReplyList = resultJObject["data"]["chatReplys"].ToObject<List<IndustryQAChatReply>>();

                            foreach (IndustryQAChatReply industryQAChatReply in industryQAChatReplyList)
                            {
                                if (!string.IsNullOrEmpty(industryQAChatReply.audioFileSource))
                                {
                                    string audioFileName = Path.GetFileName(industryQAChatReply.audioFileSource);
                                    string localAudioFileFullName = Path.Combine(_localFileDictionary, Path.GetFileName(industryQAChatReply.audioFileSource));

                                    if (!File.Exists(localAudioFileFullName))
                                    {
                                        OnSendMsgEvent(ResultMsg.Info($"本地不存在音频文件：{audioFileName}，进行下载"));
                                        await HttpUtils.DownloadFileAsync(string.Format(_downloadFileAPIUrl, industryQAChatReply.audioFileSource), localAudioFileFullName);
                                    }
                                    OnSendMsgEvent(ResultMsg.Info($"本地已存在音频文件：{audioFileName}，不进行下载"));
                                }

                                foreach (IndustryQAChatReplyScene industryQAChatReplyScene in industryQAChatReply.industryQAChatReplyScenes)
                                {
                                    if (!string.IsNullOrEmpty(industryQAChatReplyScene.fileSourceUrl))
                                    {
                                        string replySceneFileName = Path.GetFileName(industryQAChatReplyScene.fileSourceUrl);
                                        string localReplySceneFileName = Path.Combine(_localFileDictionary, Path.GetFileName(industryQAChatReplyScene.fileSourceUrl));

                                        if (!File.Exists(localReplySceneFileName))
                                        {
                                            OnSendMsgEvent(ResultMsg.Info($"本地不存在场景文件：{replySceneFileName}，进行下载"));
                                            await HttpUtils.DownloadFileAsync(string.Format(_downloadFileAPIUrl, industryQAChatReplyScene.fileSourceUrl), localReplySceneFileName);
                                        }
                                        OnSendMsgEvent(ResultMsg.Info($"本地已存在场景文件：{replySceneFileName}，不进行下载"));
                                    }
                                }

                                industryQAChatReplyDic.Add(industryQAChatReply.id, industryQAChatReply);
                            }
                            OnSendMsgEvent(ResultMsg.Info("Parthenon KnowledgeBase 接口所有音频文件和场景文件下载完成"));
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口获取所有音频文件和场景文件失败,返回结果不含code或不为success"));
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口获取所有音频文件和场景请求HTTP请求出现错误，response.StatusCode：{response.StatusCode}"));
                    }
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口获取所有音频文件和场景出现异常，异常信息:{ex.Message}"));
            }
        }

        public async Task<ResultMsg> AnswerQuest(string questionStr)
        {
            string url = string.Format(_similarAPIUrl, questionStr, accessToken);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        // 读取响应内容  
                        string responseBody = await response.Content.ReadAsStringAsync();
                        OnSendMsgEvent(ResultMsg.Info($"Parthenon KnowledgeBase 接口回答问题请求,结果{responseBody}"));
                        JObject resultJObject = JObject.Parse(responseBody);

                        if (resultJObject != null && resultJObject.ContainsKey("code") && resultJObject["code"].ToString() == "SUCCESS")
                        {
                            OnSendMsgEvent(ResultMsg.Success("Parthenon KnowledgeBase 接口回答问题请求，结果成功"));
                            return ResultMsg.Success(resultJObject["data"].ToObject<IndustryQASimilarResult>());
                        }
                        else
                        {
                            OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口回答问题请求，结果错误"));
                            return ResultMsg.Error($"Parthenon KnowledgeBase 接口回答问题请求，结果错误");
                        }
                    }
                    else
                    {
                        OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口回答问题HTTP请求出现错误，response.StatusCode：{response.StatusCode}"));
                        return ResultMsg.Error($"Parthenon KnowledgeBase 接口回答问题HTTP请求出现错误，response.StatusCode：{response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Error($"Parthenon KnowledgeBase 接口回答问题出现异常，异常信息:{ex.Message}"));
                return ResultMsg.Error($"Parthenon KnowledgeBase 接口回答问题出现异常，异常信息:{ex.Message}");
            }
        }

        public IndustryQAChatReply GetIndustryQAChatReplyById(int id)
        {
            if (industryQAChatReplyDic.ContainsKey(id))
            {
                return industryQAChatReplyDic[id];
            }
            else
            {
                
            }
        }

        #region 本地知识库数据，Excel文件
        public void GetKnowledgeBaseByExcel()
        {
            string path = Path.Combine(Thread.GetDomain().BaseDirectory, "Data", "行业知识库.xlsx");//配置文件路径
            try
            {
                localKnowledgeBaseDatas = MiniExcel.Query<LocalIndustryQAData>(path, sheetName: "行业知识库").ToArray();
                OnSendMsgEvent(ResultMsg.Success($"加载本地行业知识库文件{path}成功，共有信息{localKnowledgeBaseDatas.Length}条"));
            }
            catch (Exception ex)
            {
                OnSendMsgEvent(ResultMsg.Success($"加载本地行业知识库文件{path}出现异常，异常信息：{ex.Message}"));
            }
        }

        public LocalIndustryQAData MatchLocalKnowledgeBaseData(string questionStr)
        {
            for (int i = 0; i < localKnowledgeBaseDatas.Length; i++)
            {
                bool isMatch = localKnowledgeBaseDatas[i].MatchQA(questionStr);
                if (isMatch)
                {
                    return localKnowledgeBaseDatas[i];
                }
            }
            return null;
        }
        #endregion
    }
}
