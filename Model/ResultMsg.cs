namespace VitualPersonSpeech.Model
{
    public class ResultMsg
    {
        public MSG_TYPE msgType { get; set; }
        public string msg { get; set; }

        public ResultMsg() { }

        public ResultMsg(MSG_TYPE msgType, string msg)
        {
            this.msgType = msgType;
            this.msg = msg;
        }

        public static ResultMsg Info()
        {
            return new ResultMsg(MSG_TYPE.INFO, "");
        }

        public static ResultMsg Info(string msg)
        {
            return new ResultMsg(MSG_TYPE.INFO, msg);
        }

        public static ResultMsg Warnning()
        {
            return new ResultMsg(MSG_TYPE.WARNNING, "");
        }

        public static ResultMsg Warnning(string msg)
        {
            return new ResultMsg(MSG_TYPE.WARNNING, msg);
        }

        public static ResultMsg Error()
        {
            return new ResultMsg(MSG_TYPE.ERROR, "");
        }

        public static ResultMsg Error(string msg)
        {
            return new ResultMsg(MSG_TYPE.ERROR, msg);
        }
    }
}
