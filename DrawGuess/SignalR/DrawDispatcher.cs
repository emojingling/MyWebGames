using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace DrawGuess.SignalR
{
    /// <summary>
    /// 绘图分发器类（单例模式）
    /// </summary>
    public class DrawDispatcher
    {
        private static readonly Lazy<DrawDispatcher> _instance =
            new Lazy<DrawDispatcher>(() => new DrawDispatcher());
        public static DrawDispatcher Instance => _instance.Value;

        private readonly IHubContext _hubContext;
        private Timer _broadcastLoop;
        private readonly ConcurrentDictionary<string,GroupDetail> _dicGroup = new ConcurrentDictionary<string, GroupDetail>();

        private static Random _ran = new Random();  //随机数生成器

        private readonly TimeSpan BroadcastInterval = TimeSpan.FromMilliseconds(40);        //每秒最多更新25次绘制信息
        public const int MaxIdInGroup = 8;          //房间内的最大人数
        public const int MinGroupNum = 1;           //最小房间号
        public const int MaxGroupNum = 100;         //最大房间号

        public IHubConnectionContext<dynamic> Clients => _hubContext.Clients;

        public DrawDispatcher()
        {
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<GuessHub>();
            _dicGroup.Clear();
            for (int i = MinGroupNum; i <= MaxGroupNum; i++)
            {
                _dicGroup.TryAdd(i.ToString(), new GroupDetail());
            }

            _broadcastLoop = new Timer(
                BroadcastDrawing,
                null,
                BroadcastInterval,
                BroadcastInterval);
        }

        /// <summary>判断房间是否已满员</summary>
        /// <param name="detail">房间信息</param>
        /// <returns>房间是否已满</returns>
        public bool IsGroupFull(GroupDetail detail)
        {
            if (detail == null) return true;
            return detail.ListConnectionId.Count >= MaxIdInGroup;
        }

        /// <summary>根据页面ID得到房间信息，未找到返回null</summary>
        /// <param name="connectionId">页面ID</param>
        /// <returns>房间信息，未找到返回null</returns>
        public GroupDetail GetGroupDetail(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return null;

            GroupDetail detail = null;
            foreach (KeyValuePair<string, GroupDetail> pair in _dicGroup)
            {
                var list = pair.Value.ListConnectionId;
                foreach (UserInfo info in list)
                {
                    if (info.ConnectionId.Equals(connectionId))
                        detail = pair.Value;
                    break;
                }
            }
            return detail;
        }

        /// <summary>广播每个房间的绘画更新信息</summary>
        /// <param name="state">无用信息， 可传入null</param>
        public void BroadcastDrawing(object state)
        {
            foreach (KeyValuePair<string, GroupDetail> pair in _dicGroup)
            {
                GroupDetail detail = pair.Value;
                if (detail.InfoUpdated || !detail.IsPlaying || detail.ListConnectionId.Count == 0) continue;

                _hubContext.Clients.Group(pair.Key, detail.LastUpdateId).updateLine(detail.LastInfo);
                detail.InfoUpdated = true;
            }
        }

        /// <summary>前台向后台更新绘制线</summary>
        /// <param name="connectionId">页面ID</param>
        /// <param name="info">绘制线信息</param>
        public void UpdateLine(string connectionId, LineInfo info)
        {
            if (string.IsNullOrEmpty(connectionId) || info == null) return;

            GroupDetail detail = GetGroupDetail(connectionId);
            if (detail == null) return;

            detail.LastInfo = info;
            detail.LastUpdateId = connectionId;
            detail.InfoUpdated = false;
        }

        /// <summary>前台向后台更新绘制线</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="connectionId">页面ID</param>
        /// <param name="info">绘制线信息</param>
        public void UpdateLine(string groupName, string connectionId, LineInfo info)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(connectionId) || info == null) return;
            if (!_dicGroup.ContainsKey(groupName)) return;

            var detail = _dicGroup[groupName];
            if (Enumerable.Any(detail.ListConnectionId, userInfo => userInfo.ConnectionId.Equals(connectionId)))
            {
                detail.LastInfo = info;
                detail.LastUpdateId = connectionId;
                detail.InfoUpdated = false;
            }
        }

        /// <summary>复制房间信息（深拷贝）</summary>
        /// <param name="oriDetail">数据源</param>
        /// <returns>复制项</returns>
        public GroupDetail CopyGroupDetail(GroupDetail oriDetail)
        {
            GroupDetail copyDetail = new GroupDetail
            {
                InfoUpdated = oriDetail.InfoUpdated,
                LastUpdateId = oriDetail.LastUpdateId,
                LastInfo = oriDetail.LastInfo,
                IsPlaying =  oriDetail.IsPlaying,
                GuessingWord = oriDetail.GuessingWord
            };
            copyDetail.ListConnectionId.AddRange(oriDetail.ListConnectionId);
            copyDetail.ListPlayingId.AddRange(oriDetail.ListPlayingId);
            copyDetail.ListGuessedId.AddRange(oriDetail.ListGuessedId);
            return copyDetail;
        }

        /// <summary>退出房间</summary>
        /// <param name="connectionId">页面ID</param>
        public void ExitGroup(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            foreach (KeyValuePair<string, GroupDetail> pair in _dicGroup)
            {
                bool exist = false;
                var detail = pair.Value;
                int index = -1;
                foreach (UserInfo info in detail.ListConnectionId)
                {
                    if (info.ConnectionId.Equals(connectionId))
                    {
                        exist = true;
                        break;
                    }
                    index++;
                }
                if (exist && index  != -1)
                {
                    UserInfo ui = new UserInfo(detail.ListConnectionId[index]);
                    detail.ListConnectionId.RemoveAt(index);
                    if (detail.ListPlayingId.Contains(connectionId)) detail.ListPlayingId.Remove(connectionId);
                    if (detail.ListGuessedId.Contains(connectionId)) detail.ListPlayingId.Remove(connectionId);
                    if (detail.IsPlaying)
                        _hubContext.Clients.Group(pair.Key).recLeaveMsg(ui);
                }
            }
        }

        /// <summary>加入一个指定的房间</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="connectionId">页面ID</param>
        /// <param name="userName">用户名</param>
        /// <returns>操作状态</returns>
        /// <remarks>返回值有四种：Empty, Added, Playing, Waiting</remarks>
        public string AddToGroup(string groupName, string connectionId, string userName)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(userName))
                return "Empty";
            if (!_dicGroup.ContainsKey(groupName)) return "Empty";

            var detail = _dicGroup[groupName];
            var list = detail.ListConnectionId;
            UserInfo info = list.FirstOrDefault(ui => ui.ConnectionId.Equals(connectionId));
            if (info != null)
            {
                info.Name = userName;
                return "Added";
            }

            ExitGroup(connectionId);
            detail.ListConnectionId.Add(new UserInfo() {ConnectionId = connectionId, Name = userName});
            return detail.IsPlaying ? "Playing" : "Waiting";
        }

        /// <summary>加入一个随机的房间</summary>
        /// <param name="connectionId">页面ID</param>
        /// <returns>房间信息</returns>
        /// <remarks>输入项为空时，返回null</remarks>
        public AddedGroupInfo AddToRandomGroup(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return null;
            ExitGroup(connectionId);

            GroupDetail detail = null;
            string groupName = null;
            while (detail == null)
            {
                groupName = _ran.Next(MinGroupNum, MaxGroupNum + 1).ToString();
                if (!IsGroupFull(_dicGroup[groupName]))
                    detail = _dicGroup[groupName];
            }
            AddedGroupInfo info = new AddedGroupInfo
            {
                GroupName = groupName,
                IsPlaying = detail.IsPlaying,
                UserInfos = detail.ListConnectionId.ToArray()
            };
            return info;
        }

        /// <summary>判断房间是否为空房间</summary>
        /// <param name="groupName">房间名称</param>
        /// <returns></returns>
        public bool IsGroupEmpty(string groupName)
        {
            GroupDetail detail;
            if (!_dicGroup.TryGetValue(groupName, out detail)) return true;

            return detail.ListConnectionId.Count == 0;
        }

        /// <summary>开始一局游戏</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="guessingWord">本轮猜词</param>
        public void StartNewGame(string groupName, string guessingWord)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(guessingWord)) return;
            int groupNum;
            if (!int.TryParse(groupName, out groupNum)) return;
            if (groupNum < MinGroupNum || groupNum > MaxGroupNum) return;
            if (IsGroupEmpty(groupName)) return;

            GroupDetail detail = _dicGroup[groupName];
            if (detail == null) return;

            detail.InfoUpdated = true;
            detail.IsPlaying = true;
            detail.GuessingWord = guessingWord;
            detail.ListGuessedId.Clear();
            detail.ListPlayingId.Clear();
            detail.ListPlayingId.AddRange(detail.ListConnectionId.Select(l => l.ConnectionId));

            _hubContext.Clients.Group(groupName).startGame();
        }

        /// <summary>结束一局游戏</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="winId">赢家ID， 为空时表示为时间到结束的游戏</param>
        public void EndGame(string groupName, string winId = null)
        {
            if (string.IsNullOrEmpty(winId))      //没有赢家ID，证明为时间到结束游戏，应进行检查
            {
                if (string.IsNullOrEmpty(groupName)) return;
                if (IsGroupEmpty(groupName)) return;
            }

            GroupDetail detail;
            if (_dicGroup.TryGetValue(groupName, out detail))
            {
                detail.InfoUpdated = true;
                detail.IsPlaying = false;
                detail.GuessingWord = null;
                detail.ListGuessedId.Clear();
                detail.ListPlayingId.Clear();

                _hubContext.Clients.Group(groupName).endGame(winId);
            }
        }

        /// <summary>响应猜词</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="connectionId">页面ID</param>
        /// <param name="playerWord">玩家猜词</param>
        public void GuessWord(string groupName, string connectionId, string playerWord)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(connectionId) ||
                string.IsNullOrEmpty(playerWord)) return;
            if (IsGroupEmpty(groupName)) return;

            GroupDetail detail;
            if (_dicGroup.TryGetValue(groupName, out detail))
            {
                if (detail.IsPlaying && detail.GuessingWord.Equals(playerWord))
                {
                    detail.LastUpdateId = connectionId;
                    EndGame(groupName, connectionId);
                }
            }
        }
    }

    /*
     * 客户端需编写的函数：
     * 1. updateLine(LineInfo info) - 被动更新绘制线信息。
     * 2. startGame() - 开始一局游戏。
     * 3. endGame(string winId) - winId为空表示时间到结束游戏；非空表示有玩家答对结束游戏。
     * 4. recLeaveMsg(UserInfo info) - 被动接收其他用户的离开信息
     */
}