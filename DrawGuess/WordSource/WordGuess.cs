namespace DrawGuess.WordSource
{
    /// <summary>
    /// 给出的带猜测词条
    /// </summary>
    public class WordGuess
    {
        /// <summary>
        /// 待猜词
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// 提示文字
        /// </summary>
        public string Hint { get; set; }

        public WordGuess(string word, string hint)
        {
            Word = word;
            Hint = hint;
        }
    }
}