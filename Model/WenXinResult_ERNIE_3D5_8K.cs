using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitualPersonSpeech.Model
{
    public class WenXinResult_ERNIE_3D5_8K
    {
        public string id { get;set; }//        id string 本轮对话的id
        [JsonProperty(PropertyName = "object")]
        public string objectStr { get; set; }//object string 回包类型
                                                    //chat.completion：多轮对话返回
        public long created { get; set; }
        public bool is_truncated { get; set; }//is_truncated    bool 当前生成的结果是否被截断
        public string finish_reason { get; set; }//finish_reason string 输出内容标识，说明：
                                                     //· normal：输出内容完全由大模型生成，未触发截断、替换
                                                     //· stop：输出结果命中入参stop中指定的字段后被截断
                                                     //· length：达到了最大的token数，根据EB返回结果is_truncated来截断
                                                     //· content_filter：输出内容被截断、兜底、替换为** 等
                                                     //· function_call：调用了funtion call功能
        public string result { get;set; }

        

        
        

        

        //search_info search_info 搜索数据，当请求参数enable_citation为true并且触发搜索时，会返回该字段
        //result  string 对话返回结果
        //need_clear_history bool 表示用户输入是否存在安全风险，是否关闭当前会话，清理历史会话信息
        //· true：是，表示用户输入存在安全风险，建议关闭当前会话，清理历史会话信息
        //· false：否，表示用户输入无安全风险
        //flag    int 说明：
        //· 0：正常返回
        //· 其他：非正常
        //ban_round   int 当need_clear_history为true时，此字段会告知第几轮对话有敏感信息，如果是当前问题，ban_round=-1
        //usage usage   token统计信息
        //function_call   function_call 由模型生成的函数调用，包含函数名称，和调用参数
    }
}
