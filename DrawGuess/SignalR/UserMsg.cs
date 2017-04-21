namespace DrawGuess.SignalR
{
    /// <summary>
    /// 储存用户键入的信息
    /// </summary>
    public class UserMsg
    {
        /// <summary>
        /// 链接页面的SignalR ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// 信息内容
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 信息类型
        /// </summary>
        public UserMsgType MsgType { get; set; }

        public UserMsg(UserMsg source)
        {
            ConnectionId = source.ConnectionId;
            Msg = source.Msg;
            MsgType = source.MsgType;
        }

        public UserMsg()
        {
            MsgType = UserMsgType.NoUse;
        }
    }

    /// <summary>
    /// 用户信息的类型
    /// </summary>
    public enum UserMsgType
    {
        NoUse = 0,
        GuessingWord = 1,
        Chat = 2,
        ServiceBroadcast = 3
    }
}