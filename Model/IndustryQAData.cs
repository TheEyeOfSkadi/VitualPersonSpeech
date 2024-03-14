namespace VitualPersonSpeech.Model
{
    public class IndustryQAData
    {
        public string questStr;// 传入的问题
        public int? similarStrId; // 相似问题id
        public string similarStr;// 相似的问题
        public float? relevance;// ES查询的权重
        public float? similarity;// NLP返回的相似度
        public int? replyId;// 返回答案的id
        public string reply;// 返回的答案内容
        public string fileSource;// 返回的答案音频文件

        public IndustryQAData()
        {

        }

        public IndustryQAData(int? similarStrId, string questStr, string similarStr, float? relevance, float? similarity, int? replyId, string reply, string fileSource)
        {
            this.similarStrId = similarStrId;
            this.questStr = questStr;
            this.similarStr = similarStr;
            this.relevance = relevance;
            this.similarity = similarity;
            this.replyId = replyId;
            this.reply = reply;
            this.fileSource = fileSource;
        }
    }
}
