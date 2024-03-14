namespace VitualPersonSpeech.Model
{
    public class IndustryQAChatQuestReply
    {
        public int id { get; set; }
        public string questContent { get; set; }
        public IndustryQAChatReply chatReply { get; set; }

        public IndustryQAChatQuestReply() { }

        public IndustryQAChatQuestReply(int id, string questContent, IndustryQAChatReply chatReply) 
        {
            this.id = id;
            this.questContent = questContent;
            this.chatReply = chatReply;
        }
    }

    public class IndustryQAChatReply
    {
        public int id { get; set; }
        public string content { get; set; }
        public string fileSource { get; set; }

        public IndustryQAChatReply() { }

        public IndustryQAChatReply(int id, string content, string fileSource) 
        {
            this.id = id;
            this.content = content;
            this.fileSource = fileSource;
        }
    }
}
