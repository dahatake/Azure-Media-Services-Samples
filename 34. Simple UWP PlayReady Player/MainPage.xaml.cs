using System;
using Windows.UI.Xaml.Controls;

using Windows.Media.Protection.PlayReady;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace SimplePlayReady
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

			this.initialiseMediaProtectionManager(mediaElement);

			mediaElement.AreTransportControlsEnabled = true;
			//mediaElement.Source = new Uri("http://wams.edgesuite.net/media/SintelTrailer_Smooth_from_WAME_720p_Main_Profile_CENC/CENC/sintel_trailer-720p.ism/manifest(format=mpd-time-csf).mpd");
			mediaElement.Source = new Uri("http://dahatakemediademo.streaming.mediaservices.windows.net/03a70a4a-780b-45a9-8718-bb99e4e7c7cd/Take%20Your%20Free%20Ride_720.ism/manifest(format=mpd-time-csf).mpd");

		}


		private void initialiseMediaProtectionManager(MediaElement mediaElement)
		{
			var mediaProtectionManager = new Windows.Media.Protection.MediaProtectionManager();
			mediaProtectionManager.Properties["Windows.Media.Protection.MediaProtectionContainerGuid"] = "{9A04F079-9840-4286-AB92-E65BE0885F95}"; // Setup the container GUID for CFF

			var cpsystems = new Windows.Foundation.Collections.PropertySet();
			cpsystems["{F4637010-03C3-42CD-B932-B48ADF3A6A54}"] = "Windows.Media.Protection.PlayReady.PlayReadyWinRTTrustedInput"; // PlayReady
			mediaProtectionManager.Properties["Windows.Media.Protection.MediaProtectionSystemIdMapping"] = cpsystems;
			mediaProtectionManager.Properties["Windows.Media.Protection.MediaProtectionSystemId"] = "{F4637010-03C3-42CD-B932-B48ADF3A6A54}";

			mediaElement.ProtectionManager = mediaProtectionManager;

			mediaProtectionManager.ServiceRequested += MediaProtectionManager_ServiceRequested;
		}

		private async void MediaProtectionManager_ServiceRequested(Windows.Media.Protection.MediaProtectionManager sender, 
			Windows.Media.Protection.ServiceRequestedEventArgs e)
		{
			var completionNotifier = e.Completion;

			IPlayReadyServiceRequest request = (IPlayReadyServiceRequest)e.Request;

			////TODO: retrieve service request type from Microsoft.Media.Protection.PlayReady
			//if (request.Type != new Guid("c6b344bd-6017-4199-8474-694ac3ec0b3f"))
			//{
			//	request.Uri = new Uri(licenseUrl);
			//}

			try
			{
				await request.BeginServiceRequest();

				completionNotifier.Complete(true);
			}
			catch (Exception ex)
			{
				completionNotifier.Complete(false);
			}
		}
	}
}
