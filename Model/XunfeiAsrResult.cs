using Newtonsoft.Json;

namespace VitualPersonSpeech.Model
{
    /// <summary>
    /// 讯飞流式识别结果
    /// </summary>
    class XunfeiAsrResult
    {
        public string sid { get; set; }
        public int? code { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }

    class Data
    {
        public int? status { get; set; }
        public Result result { get; set; }
    }

    class Result
    {
        public int? sn { get; set; }
        public bool? ls { get; set; }
        [JsonProperty(PropertyName = "ws")]
        public Ws[] wss { get; set; }
        public string pgs { get; set; } // 判断是替换还是添加
    }

    class Ws
    {
        [JsonProperty(PropertyName = "cw")]
        public Cw[] cws { get; set; }
    }

    class Cw
    {
        public string w { get; set;}
    }
}
