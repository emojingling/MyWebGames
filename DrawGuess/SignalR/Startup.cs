using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(DrawGuess.SignalR.Startup))]

namespace DrawGuess.SignalR
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
