using System.Collections.Generic;

namespace DrawGuess.WordSource
{
    /// <summary>
    /// 猜词库接口
    /// </summary>
    interface IWordSource
    {
        /// <summary>
        /// 得到一个新词
        /// </summary>
        /// <returns></returns>
        WordGuess GetOne();

        /// <summary>
        /// 得到一组新词
        /// </summary>
        /// <param name="count">分组数量</param>
        /// <returns></returns>
        IEnumerable<WordGuess> GetGroup(int count = 4);
    }
}
