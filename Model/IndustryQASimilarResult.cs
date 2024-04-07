using Newtonsoft.Json;

namespace VitualPersonSpeech.Model
{
    public class IndustryQASimilarResult
    {
        public int? similarQuestId; // 相似问题id
        public string questStr;// 传入的问题
        public string similarStr;// 相似的问题
        public float? relevance;// ES查询的权重
        public float? similarity;// NLP返回的相似度
        [JsonProperty(PropertyName = "chatReply")]
        public IndustryQAChatReply industryQAChatReply;

        public IndustryQASimilarResult()
        {

        }

        public IndustryQASimilarResult(string questStr, int? similarQuestId, string similarStr, float? relevance, float? similarity, IndustryQAChatReply industryQAChatReply)
        {
            this.questStr = questStr;
            this.similarQuestId = similarQuestId;
            this.similarStr = similarStr;
            this.relevance = relevance;
            this.similarity = similarity;
            this.industryQAChatReply = industryQAChatReply;
        }
    }
}
