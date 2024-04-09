namespace VitualPersonSpeech.Utils
{
    public class VirtualHumanCmdUtils
    {
        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType)
        {
            return string.Format("{0}", virtualPersonCtrlType);
        }

        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType, AIReplyProgress aiReplyProgress, string content = "")
        {
            return string.Format("{0}-{1}-{2}", virtualPersonCtrlType, aiReplyProgress, content);
        }

        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType, AIImgProgress aiImgProgress, string content = "")
        {
            return string.Format("{0}-{1}-{2}", virtualPersonCtrlType, aiImgProgress, content);
        }

        public static string GetVirtualPersonCtrlStr(VirtualPersonCtrlType virtualPersonCtrlType, IndustryQADataChatReply industryQAChatReply, string content = "")
        {
            return string.Format("{0}-{1}-{2}", virtualPersonCtrlType, industryQAChatReply, content);
        }

        // HeartBeat：心跳包
        // StandBy：站立不动
        // TakeAnim：演讲动作
        // SayWelcomeStr:欢迎词
        // SayNoVoice:没有听到说什么
        // SayAPIError:说后台接口请求错误
        public enum VirtualPersonCtrlType
        {
            HeartBeat,
            StandBy,
            TakeAnim,
            SayWelcomeStr,
            SayNoVoice,
            SayAPIError,
            AIReply,
            AIImg,
            TakeIndustryQAData
        }

        // AIReply: 应答
        //      InAIReply: 正在AI应答中正在ai推理生成回答中
        //      AIReplySuccess: AI应答成功 - ai应答结果
        //      AIReplyFail: AI应答失败 - ai应答失败原因
        public enum AIReplyProgress // AI问答进度
        {
            InAIReply,
            AIReplySuccess,
            AIReplyFail
        }

        // AIImage：AI作画
        //      InAIImg：正在AI作画中
        //      AIImgSuccess: AI作画成功 - 画作url
        //      AIImgFail: AI作画失败 - ai作画失败原因
        public enum AIImgProgress // AI作画进度
        {
            InAIImg,
            AIImgSuccess,
            AIImgFail
        }

        // IndustryQADataChatReply：行业知识库回复内容
        //      IndustryQADataChatReplyContent：行业知识库回复文字内容
        //      IndustryQADataChatReplyFileSource: 行业知识库回复音频文件
        public enum IndustryQADataChatReply // 行业知识库回答内容
        {
            IndustryQADataChatReplyContent,
            IndustryQADataChatReplyFileSource
        }
    }
}
