using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SwitchVideoPlayerURL
{

	[HubName("MonitorHub")]
	public class MonitorHub : Hub
	{

		private static int _concurrentConnection = 0;
		private static string _currentURL = "";

		[HubMethodName("SendBroadcastMessage")]		
		public void SendBroadcastMessage(string Url)
		{
			if (Url.Length > 0)
			{
				_currentURL = Url;
			}
			var message = _concurrentConnection;
			Clients.All.sendBroadcastMessage(_currentURL, message);

		}

		public override System.Threading.Tasks.Task OnConnected()
		{
			_concurrentConnection += 1;
			Clients.All.broadcastMessage("", _concurrentConnection);
			return base.OnConnected();
		}

		public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
		{
			_concurrentConnection -= 1;
			Clients.All.broadcastMessage("", _concurrentConnection);
			return base.OnDisconnected(stopCalled);
		}


	}
}