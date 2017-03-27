using System;
using System.IO;

namespace MyHelpers
{
    /// <summary>
    /// 日志记录类
    /// </summary>
    /// <remarks>chenz, 2017.03.27</remarks>
    public static class LogHelper
    {
        private const string DefaultOtherMsgs = "No Other Messages";
        private const string StrApart = "||";

        #region 日志目录

        private static string _logDir = AppDomain.CurrentDomain.BaseDirectory.Trim() + "\\log";
        public static string GetLogDir() { return _logDir; }

        public static void SetLogDir(string logDir)
        {
            if (string.IsNullOrWhiteSpace(logDir)) return;
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            _logDir = logDir;
        }

        #endregion

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="functionName">记录日志的函数名称</param>
        /// <param name="e">错误信息</param>
        /// <param name="otherMsgs">附加信息</param>
        public static void WriteLog(string functionName, Exception e, string otherMsgs)
        {
            string info = e.ToString();
            WriteLog(functionName, info, otherMsgs);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="functionName">记录日志的函数名称</param>
        /// <param name="info">记录信息</param>
        /// <param name="otherMsgs">附加信息</param>
        public static void WriteLog(string functionName, string info, string otherMsgs)
        {
            //得到当前时间及文件路径
            DateTime time = DateTime.Now;
            string fileName = time.ToString("yyyy-MM") + ".log";
            string filePath = _logDir + "\\" + fileName; //每月一个记录
            //确保目录被创建
            if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);
            //创建/打开.log文件
            FileStream fStream = File.Exists(filePath) ? File.Open(filePath, FileMode.Append) : File.Create(filePath);
            //增加数据行
            string timeNow = time.ToShortDateString() + " " + time.ToLongTimeString();
            string writeLineNow = string.Format("{0}{1}{2}{1}{3}{1}{4}{5}", timeNow, StrApart, functionName, info,
                otherMsgs, Environment.NewLine); //将所有信息合为一行
            byte[] functionNames = System.Text.Encoding.UTF8.GetBytes(writeLineNow);
            fStream.Write(functionNames, 0, functionNames.Length);
            //关闭文件
            fStream.Close();
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="functionName">记录日志的函数名称</param>
        /// <param name="e">错误信息</param>
        public static void WriteLog(string functionName, Exception e)
        {
            WriteLog(functionName, e, DefaultOtherMsgs);
        }
    }
}
