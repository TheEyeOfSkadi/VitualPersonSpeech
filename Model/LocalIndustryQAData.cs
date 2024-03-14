using MiniExcelLibs.Attributes;

namespace VitualPersonSpeech.Model
{
    public class LocalIndustryQAData
    {

        private string question;
        private string keyword;
        private string answer;
        private string[] keywordArray;

        [ExcelColumnName("问题")]
        public string Question 
        { 
            get => question; 
            set => question = value; 
        }
       
        [ExcelColumnName("关键词")]
        public string Keyword 
        { 
            get => keyword;
            set
            {
                keyword = value;
                keywordArray = value.Split('、');
            }
        }

        [ExcelColumnName("回答")]
        public string Answer 
        { 
            get => answer; 
            set => answer = value; 
        }

        public LocalIndustryQAData()
        {

        }

        public LocalIndustryQAData(string question, string keyword, string answer)
        {
            this.question = question;
            this.Keyword = keyword;
            this.answer = answer;   
        }

        public bool MatchQA(string questionStr)
        {
            bool flag = true;
            for (int i = 0; i < keywordArray.Length; i++)
            {
                if (!questionStr.Contains(keywordArray[i]))
                {
                    flag = false;
                }
            }
            return flag;
        }
    }
}
