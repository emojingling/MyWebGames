namespace DrawGuess.SignalR
{
    /// <summary>
    /// 加入房间后的返回信息
    /// </summary>
    public class AddedGroupInfo
    {
        /// <summary>
        /// 房间名称
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// 房间是否已经开始
        /// </summary>
        public bool IsPlaying { get; set; }
        /// <summary>
        /// 房间内的用户信息
        /// </summary>
        public UserInfo[] UserInfos { get; set; }
    }
}