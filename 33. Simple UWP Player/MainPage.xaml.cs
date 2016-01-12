using System;
using Windows.UI.Xaml.Controls;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace Simple_UWP_Player
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

			mediaElement.AreTransportControlsEnabled = true;
			mediaElement.Source = new Uri("http://wams.edgesuite.net/media/SintelTrailer_MP4_from_WAME/sintel_trailer-1080p.ism/manifest(format=mpd-time-csf).mpd");
        }
    }
}
