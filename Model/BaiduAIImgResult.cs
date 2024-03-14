using Newtonsoft.Json;
using System.Collections.Generic;

namespace VitualPersonSpeech.Model
{
    /// <summary>
    /// 百度AI作画结果
    /// </summary>
    class BaiduAIImgResult
    {
        public string log_id { get; set; }
        [JsonProperty(PropertyName = "data")]
        public BaiduAIImgData data { get; set; }

        public BaiduAIImgResult()
        {

        }

        public BaiduAIImgResult(string log_id, BaiduAIImgData data)
        {
            this.log_id = log_id;
            this.data = data;
        }
    }

    class BaiduAIImgData
    {
        public long task_id { get; set;}
        public string task_status { get; set; }
        public int task_progress { get; set; }
        public List<sub_task_result> sub_task_result_list { get; set; }
    }

    class sub_task_result
    {
        public string sub_task_status { get; set; }
        public int sub_task_progress { get; set; }
        public int sub_task_error_code { get; set; }
        public List<final_image> final_image_list { get; set; }
    }

    class final_image
    {
        public string img_approve_conclusion { get; set; }
        public string img_url { get; set; }
        public int width { get; set;}
        public int height { get; set; }
    }
}
