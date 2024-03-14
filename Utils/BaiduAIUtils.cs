using Baidu.Aip.Speech;

namespace VitualPersonSpeech.Utils
{
    class BaiduAIUtils
    {
        private static BaiduAIUtils instance = null;

        private string AppId = "36012261";
        private string ApiKey = "iQxBR94puMGVjLSOsUfghba5";
        private string SecrectKey = "oEgvDNFTz9huud3Xb22wdeEaojLYw8hW";
        private Asr asr;
        private Tts tts;

        public static BaiduAIUtils Instance
        {
            get 
            {
                if (instance == null)
                {
                    instance = new BaiduAIUtils();
                }

                return instance; 
            }
        }

        public Asr GetAsr()
        {
            if (asr == null)
            {
                asr = new Asr(AppId, ApiKey, SecrectKey);
            }

            return asr;
        }

        public Tts GetTTS()
        {
            if (tts == null)
            {
                tts = new Tts(ApiKey, SecrectKey);
            }

            return tts;
        }
    }
}


