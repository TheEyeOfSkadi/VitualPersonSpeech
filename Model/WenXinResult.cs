using Newtonsoft.Json;

namespace VitualPersonSpeech.Model
{
    /// <summary>
    /// 百度文心一言结果
    /// </summary>
    class WenXinResult
    {
        public string id { get; set; }
        [JsonProperty(PropertyName = "object")]
        public string objectStr { get; set; }
        public long created { get; set; }
        public string result { get; set; }
        public bool is_truncated { get; set; }
        public bool need_clear_history { get; set; }

        public WenXinResult()
        {
        }

        public WenXinResult(string id, string objectStr, long created, string result, bool is_truncated, bool need_clear_history)
        {
            this.id = id;
            this.objectStr = objectStr;
            this.created = created;
            this.result = result;
            this.is_truncated = is_truncated;
            this.need_clear_history = need_clear_history;
        }
    }

    class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }

        public Usage()
        {
        }

        public Usage(int prompt_tokens, int completion_tokens, int total_tokens)
        {
            this.prompt_tokens = prompt_tokens;
            this.completion_tokens = completion_tokens;
            this.total_tokens = total_tokens;
        }
    }
}
