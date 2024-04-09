namespace VitualPersonSpeech.Utils
{
    public class MultiScreenPlayerCmdUtils
    {
        public static string GetMultiScreenPlayerCtrlPositionStr(int x, int y)
        {
            return string.Format("{0}-{1}-{2}", MultiScreenPlayerCtrlType.Position, x, y);
        }

        public static string GetMultiScreenPlayerCtrlSizeStr(int width, int height)
        {
            return string.Format("{0}-{1}-{2}", MultiScreenPlayerCtrlType.Size, width, height);
        }

        public static string GetMultiScreenPlayerCtrlStreamStr(StreamCtrlType streamCtrlType)
        {
            return string.Format("{0}-{1}", MultiScreenPlayerCtrlType.Stream, streamCtrlType);
        }

        public enum MultiScreenPlayerCtrlType
        {
            Position,
            Size,
            Stream
        }

        // StreamCtrlType: 串流显示控制
        //      Play: 播放
        //      Stop: 停止
        public enum StreamCtrlType // 串流显示控制
        {
            Play,
            Stop
        }
    }
}
