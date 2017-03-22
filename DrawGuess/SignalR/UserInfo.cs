namespace DrawGuess.SignalR
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// 链接页面的SignalR ID
        /// </summary>
        public string ConnectionId { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        public string Name { get; set; }

        public UserInfo(UserInfo source)
        {
            ConnectionId = source.ConnectionId;
            Name = source.Name;
        }

        public UserInfo()
        {
            
        }
    }
}