using System;
using System.Collections.Generic;

namespace DrawGuess.WordSource
{
    /// <summary>
    /// 猜词库接口
    /// </summary>
    public interface IWordSource : IDisposable
    {
        /// <summary>
        /// 得到一个新词
        /// </summary>
        /// <param name="level">难度级别，数值越大，词语越难。推荐范围从0至9</param>
        /// <returns></returns>
        WordGuess GetOne(int level = 0);

        /// <summary>
        /// 得到一组新词
        /// </summary>
        /// <param name="count">分组数量</param>
        /// <returns></returns>
        IEnumerable<WordGuess> GetGroup(int count = 4);
    }
}
