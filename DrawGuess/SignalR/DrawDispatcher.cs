using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace DrawGuess.SignalR
{
    public class DrawDispatcher
    {
        private static readonly Lazy<DrawDispatcher> _instance =
            new Lazy<DrawDispatcher>(() => new DrawDispatcher());
        public static DrawDispatcher Instance => _instance.Value;
        // We're going to broadcast to all clients a maximum of 25 times per second
        private readonly TimeSpan BroadcastInterval = TimeSpan.FromMilliseconds(40);

        private readonly IHubContext _hubContext;
        private Timer _broadcastLoop;
        private readonly ConcurrentDictionary<string,GroupDetail> _dicGroup = new ConcurrentDictionary<string, GroupDetail>();

        public IHubConnectionContext<dynamic> Clients => _hubContext.Clients;

        public DrawDispatcher()
        {
            // Save our hub context so we can easily use it 
            // to send to its connected clients
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<GuessHub>();
            _dicGroup.Clear();
            // Start the broadcast loop
            _broadcastLoop = new Timer(
                BroadcastDrawing,
                null,
                BroadcastInterval,
                BroadcastInterval);
        }

        /// <summary>广播每个房间的绘画更新信息</summary>
        /// <param name="state">无用信息， 可传入null</param>
        public void BroadcastDrawing(object state)
        {
            foreach (KeyValuePair<string, GroupDetail> pair in _dicGroup)
            {
                if (pair.Value.InfoUpdated || !pair.Value.IsPlaying) continue;

                _hubContext.Clients.Group(pair.Key, pair.Value.LastUpdateId).updateLine(pair.Value.LastInfo);
                pair.Value.InfoUpdated = true;
            }
        }

        /// <summary>前台向后台更新绘制线</summary>
        /// <param name="connectionId">页面ID</param>
        /// <param name="info">绘制线信息</param>
        public void UpdateLine(string connectionId, LineInfo info)
        {
            if (string.IsNullOrEmpty(connectionId) || info == null) return;

            foreach (KeyValuePair<string, GroupDetail> pair in _dicGroup)
            {
                if (pair.Value.ListConnectionId.Contains(connectionId))
                {
                    pair.Value.LastInfo = info;
                    pair.Value.LastUpdateId = connectionId;
                    pair.Value.InfoUpdated = false;
                    break;
                }
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

        /// <summary>添加一个连接</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="connectionId">页面ID</param>
        public void AddGroup(string groupName, string connectionId)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(connectionId)) return;

            var detail = new GroupDetail();
            detail.ListConnectionId.Add(connectionId);
            if (!_dicGroup.TryAdd(groupName, detail))
            {
                GroupDetail oriDetail;
                if (_dicGroup.TryGetValue(groupName, out oriDetail) && !oriDetail.ListConnectionId.Contains(connectionId))
                {
                    detail = CopyGroupDetail(oriDetail);
                    detail.ListConnectionId.Add(connectionId);
                    _dicGroup.TryUpdate(groupName, detail, oriDetail);
                }
            }
        }

        /// <summary>移除一个连接</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="connectionId">页面ID</param>
        public void RemoveGroup(string groupName, string connectionId)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(connectionId)) return;

            GroupDetail detail;
            if (! _dicGroup.TryGetValue(groupName, out detail)) return;
            if (detail.ListConnectionId.Contains(connectionId)) detail.ListConnectionId.Remove(connectionId);
            if (detail.ListConnectionId.Count == 0) _dicGroup.TryRemove(groupName, out detail);
        }

        /// <summary>判断房间是否为空房间</summary>
        /// <param name="groupName">房间名称</param>
        /// <returns></returns>
        public bool IsGroupEmpty(string groupName)
        {
            GroupDetail detail;
            if (!_dicGroup.TryGetValue(groupName, out detail)) return true;
            if (detail.ListConnectionId.Count == 0)
            {
                _dicGroup.TryRemove(groupName, out detail);
                return true;
            }
            return false;
        }

        /// <summary>开始一局游戏</summary>
        /// <param name="groupName">房间名称</param>
        /// <param name="guessingWord">本轮猜词</param>
        public void StartNewGame(string groupName, string guessingWord)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(guessingWord)) return;
            if (IsGroupEmpty(groupName)) return;

            GroupDetail detail;
            if (_dicGroup.TryGetValue(groupName, out detail))
            {
                detail.InfoUpdated = true;
                detail.IsPlaying = true;
                detail.GuessingWord = guessingWord;
                detail.ListGuessedId.Clear();
                detail.ListPlayingId.Clear();
                detail.ListPlayingId.AddRange(detail.ListConnectionId);

                _hubContext.Clients.Group(groupName).startGame();
            }
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
     */
}