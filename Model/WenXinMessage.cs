namespace VitualPersonSpeech.Model
{
    /// <summary>
    /// 百度文心一言接口参数中的Message信息
    /// </summary>
    class WenXinMessage
    {
        public string role { get; set; }//当前支持以下：user: 表示用户、assistant: 表示对话助手
        public string content { get; set; }

        public WenXinMessage() { }

        public WenXinMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
}
