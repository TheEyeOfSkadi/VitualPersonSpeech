namespace VitualPersonSpeech.Model
{
    public class ResultMsg
    {
        private StatusCode _statusCode;
        private string _msg;
        private object _data;

        public StatusCode StatusCode 
        { 
            get => _statusCode;
            set => _statusCode = value; 
        }
        public string Msg 
        { 
            get => _msg; 
            set => _msg = value; 
        }
        public object Data 
        { 
            get => _data; 
            set => _data = value; 
        }

        public ResultMsg() { }

        public ResultMsg(StatusCode statusCode, string msg, object data)
        {
            StatusCode = statusCode;
            Msg = msg;
            Data = data;
        }

        public static ResultMsg Success(object data)
        {
            return new ResultMsg(StatusCode.SUCCESS, "", data);
        }

        public static ResultMsg Error(string msg)
        {
            return new ResultMsg(StatusCode.ERROR, msg, null);
        }


        public static ResultMsg Info(string msg)
        {
            return new ResultMsg(StatusCode.INFO, msg, null);
        }
    }

    public enum StatusCode
    {
        SUCCESS,
        INFO,
        ERROR
    }
}
