using System.Collections.Generic;

namespace VitualPersonSpeech.Model
{
    /// <summary>
    /// 百度语音识别结果
    /// </summary>
    class BaiduAsrResult
    {
        public int err_no { get; set; }
        public string err_msg { get; set; }
        public string corpus_no { get; set; }
        public string sn { get; set; }
        public List<string> result { get; set; }
    }
}
