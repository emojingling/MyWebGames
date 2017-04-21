using System;
using System.Web.Configuration;

namespace DrawGuess.WordSource
{
    /// <summary>
    /// 单例执行词库
    /// </summary>
    public class WordSource
    {
        private static IWordSource _instance;

        /// <summary>
        /// 获取词库的单例
        /// </summary>
        public static IWordSource Instance
        {
            get
            {
                if (_instance == null)
                {
                    IntiInstance();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 初始化实例
        /// </summary>
        public static void IntiInstance()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
            }

            var className = WebConfigurationManager.AppSettings.Get("WordSourceName");
            if (string.IsNullOrEmpty(className)) return;

            var type = Type.GetType(className);
            var obj = type?.Assembly.CreateInstance(className);
            if (!(obj is IWordSource)) return;
            _instance = obj as IWordSource;
        }
    }
}