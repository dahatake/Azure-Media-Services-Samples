using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.ServiceBus;

[assembly: OwinStartup(typeof(SwitchVideoPlayerURL.Startup))]

namespace SwitchVideoPlayerURL
{
	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{

			//// Backplane
			//string sbConnectionString = "<<Service Bus の接続文字列>>";
			//GlobalHost.DependencyResolver.UseServiceBus(sbConnectionString, "dahatakeSignalRTest");

			app.MapSignalR();
		}
	}
}
