using System;
using System.Collections.Generic;

namespace DrawGuess.WordSource
{
    /// <summary>
    /// 简单的数据源，数据值为固定项
    /// </summary>
    public class SimpleWordSource : IWordSource
    {
        /// <summary>词库</summary>
        private static string[] _words;
        /// <summary>提示库</summary>
        private static string[] _hints;
        /// <summary>随机数生成器</summary>
        private static Random _ran;

        public SimpleWordSource()
        {
            if (_words == null)
            {
                _words = new[]
                {
                    "蓝天", "台灯", "黑土", "教科书", "小汽车", "战争", "孔乙己", "电脑", "侦探", "柯南", "苹果手机", "建设银行", "五斗柜", "大连市"
                };
                _hints = new []
                {
                    "户外", "电器", "户外", "学习", "机器", "不好的大规模状态", "人物", "设备", "职业", "虚构的人物", "常见的个人设备", "财力雄厚", "家具", "行政区域"
                };
                _ran = new Random();
            }
        }

        public WordGuess GetOne(int level = 0)
        {
            int length = _words.Length;
            int index = _ran.Next(0, length);
            return new WordGuess(_words[index], _hints[index]);
        }

        public IEnumerable<WordGuess> GetGroup(int count = 4)
        {
            int length = _words.Length;
            if (count > length) return null;

            var indexs = GetRanInts(0, length, count);
            List<WordGuess> list = new List<WordGuess>();
            foreach (int index in indexs)
            {
                list.Add(new WordGuess(_words[index], _hints[index]));
            }
            return list;
        }

        /// <summary>得到一组非重复的随机整数</summary>
        /// <param name="min">随机数最小值</param>
        /// <param name="max">随机数最大值</param>
        /// <param name="count">寻求数量</param>
        /// <returns>非重复的随机整数组</returns>
        private List<int> GetRanInts(int min, int max, int count)
        {
            List<int> list = new List<int>(count);
            while (list.Count < count)
            {
                int val = _ran.Next(min, max);
                if (!list.Contains(val)) list.Add(val);
            }
            return list;
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
        }

        #endregion
    }
}