namespace VitualPersonSpeech.Model
{
    public class EventHandlerMsg
    {
        public string msg { get; set; }
        public MSG_TYPE msgType { get; set; }

        public EventHandlerMsg() { }

        public EventHandlerMsg(string msg, MSG_TYPE msgType)
        {
            this.msg = msg;
            this.msgType = msgType;
        }

        public static EventHandlerMsg SetInfoMsg(string msg)
        {
            return new EventHandlerMsg(msg, MSG_TYPE.INFO);
        }

        public static EventHandlerMsg SetWarnningMsg(string msg)
        {
            return new EventHandlerMsg(msg, MSG_TYPE.WARNNING);
        }

        public static EventHandlerMsg SetErrorMsg(string msg)
        {
            return new EventHandlerMsg(msg, MSG_TYPE.ERROR);
        }
    }
}
