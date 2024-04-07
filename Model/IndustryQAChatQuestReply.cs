using Newtonsoft.Json;
using System.Collections.Generic;

namespace VitualPersonSpeech.Model
{
    public class IndustryQAChatReply
    {
        public int id { get; set; }
        public string replyContent { get; set; }
        public string audioFileSource { get; set; }
        [JsonProperty(PropertyName = "replyScenes")]
        public List<IndustryQAChatReplyScene> industryQAChatReplyScenes { get; set; }

        public IndustryQAChatReply() { }

        public IndustryQAChatReply(int id, string replyContent, string audioFileSource, List<IndustryQAChatReplyScene> industryQAChatReplyScenes)
        {
            this.id = id;
            this.replyContent = replyContent;
            this.audioFileSource = audioFileSource;
            this.industryQAChatReplyScenes = industryQAChatReplyScenes;
        }
    }

    public class IndustryQAChatReplyScene
    {
        public int id { get; set; }
        public int time { get; set; }
        public string fileSourceUrl { get; set; }
        public int chatReplyId { get; set; }

        public IndustryQAChatReplyScene() { }

        public IndustryQAChatReplyScene(int id, int time, string fileSourceUrl, int chatReplyId)
        {
            this.id = id;
            this.time = time;
            this.fileSourceUrl = fileSourceUrl;
            this.chatReplyId = chatReplyId;
        }
    }
}
