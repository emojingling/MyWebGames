using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;

namespace DrawGuess.SignalR
{
    public class GuessHub : Hub
    {
        private readonly DrawDispatcher _dispatcher;

        public GuessHub()
            : this(DrawDispatcher.Instance)
        {
        }

        public GuessHub(DrawDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
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
            string str = "AddToGroup - groupName: " + groupName + ", userName: " + userName;
            using (FileStream fs = new FileStream(@"E:\rec3.txt", FileMode.Append))
            {
                byte[] bytes1 = Encoding.ASCII.GetBytes(str + Environment.NewLine);
                fs.Write(bytes1, 0, bytes1.Length);
                fs.Close();
            }

            string connectionId = Context.ConnectionId;
            return _dispatcher.AddToGroup(groupName, connectionId, userName);
        }

        public void StartNewGame(string groupName, string guessingWord)
        {
            string str = "StartNewGame - groupName: " + groupName + ", guessingWord: " + guessingWord;
            using (FileStream fs = new FileStream(@"E:\rec3.txt", FileMode.Append))
            {
                byte[] bytes1 = Encoding.ASCII.GetBytes(str + Environment.NewLine);
                fs.Write(bytes1, 0, bytes1.Length);
                fs.Close();
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