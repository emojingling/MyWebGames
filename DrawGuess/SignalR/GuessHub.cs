using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

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

        public void UpdateLine(LineInfo info)
        {
            string connectionId = Context.ConnectionId;
            _dispatcher.UpdateLine(connectionId, info);
        }

        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            return base.OnDisconnected(stopCalled);
        }
    }
}