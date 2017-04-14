using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using NLog;

namespace DrawGuess.SignalR
{
    public class GuessHub : Hub
    {
        private readonly DrawDispatcher _dispatcher;
        private readonly bool _debugMode;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public GuessHub()
            : this(DrawDispatcher.Instance)
        {
        }

        public GuessHub(DrawDispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            _debugMode = Convert.ToBoolean(_dispatcher.GetAppSettings("DebugMode"));
        }

        public void UploadLine(string jsonInfo)
        {
            if (string.IsNullOrEmpty(jsonInfo)) return;
            LineInfo info = JsonConvert.DeserializeObject<LineInfo>(jsonInfo);

            string connectionId = Context.ConnectionId;
            _dispatcher.UploadLine(connectionId, info);
        }

        public string AddToGroup(string groupName, string userName)
        {
            if (_debugMode)
            {
                Logger.Debug("AddToGroup - groupName: {0}, userName: {1}", groupName, userName);
            }

            string connectionId = Context.ConnectionId;
            return _dispatcher.AddToGroup(groupName, connectionId, userName);
        }

        public void StartNewGame(string groupName, string guessingWord)
        {
            if (_debugMode)
            {
                Logger.Debug("StartNewGame - groupName: {0}, guessingWord: {1}", groupName, guessingWord);
            }

            _dispatcher.StartNewGame(groupName, guessingWord);
        }

        public void EndGame(string groupName, string winId = null)
        {
            _dispatcher.EndGame(groupName, winId);
        }

        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _dispatcher.ExitGroup(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }
    }
}