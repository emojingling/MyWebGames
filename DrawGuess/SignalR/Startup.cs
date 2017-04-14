using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(DrawGuess.SignalR.Startup))]

namespace DrawGuess.SignalR
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.Configuration.DefaultMessageBufferSize = 500;
            app.MapSignalR();
        }
    }
}
