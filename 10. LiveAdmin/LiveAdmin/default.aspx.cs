using System;
using System.Linq;
using System.Web.UI.WebControls;
using System.Configuration;

using Microsoft.WindowsAzure.MediaServices.Client;
using System.Text;

namespace LiveAdmin
{
	public partial class _default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			ReRenderingPage();
		}

		private void ReRenderingPage()
		{
			tableListChannel.Controls.Clear();
			BuildTableHeader();
			ListupChannel();

		}

		private void BuildTableHeader()
		{
			// Table Header
			string[] columnTitles = {"Channel名",
										"状態",
										"取り込みURL",
										"Preview",
										"Origin"};

			var headerRow = new TableHeaderRow();
			foreach (var item in columnTitles)
			{
				var headerItem = new TableHeaderCell();
				headerItem.Text = item;
				headerRow.Controls.Add(headerItem);
			}
			tableListChannel.Controls.Add(headerRow);

		}

		private void ListupChannel()
		{

			var AccountName = ConfigurationManager.AppSettings["accountName"];
			var AccountKey = ConfigurationManager.AppSettings["accountKey"];
			var context = new CloudMediaContext(new MediaServicesCredentials(AccountName, AccountKey));

			foreach (var channel in context.Channels)
			{
				// 1件のチャネル
				var channelRow = new TableRow();

				var channelNameCell = new TableCell();
				channelNameCell.Text = channel.Name;
				channelNameCell.Width = Unit.Percentage(10);

				var ingestURLCell = new TableCell();
				var ingestURLCellText = new TextBox();
				ingestURLCellText.Text = channel.Input.Endpoints.FirstOrDefault().Url.AbsoluteUri;
				ingestURLCell.Controls.Add(ingestURLCellText);

				var channelStatus = new TableCell();
				var channelStatusBackgroundColor = "green";

				switch (channel.State)
				{
					case ChannelState.Running:
						break;

					case ChannelState.Deleting:
					case ChannelState.Starting:
					case ChannelState.Stopping:
						channelStatusBackgroundColor = "purple";
						break;

					case ChannelState.Stopped:
						channelStatusBackgroundColor = "red";
						break;

					default:
						break;
				}

				Literal channelStatusText = new Literal();
				channelStatusText.Text = channel.State.ToString();
				channelStatus.Controls.Add(channelStatusText);
				channelStatus.Style.Add("background-color", channelStatusBackgroundColor);
				channelStatus.Width = Unit.Percentage(5);

				channelRow.Controls.Add(channelNameCell);
				channelRow.Controls.Add(channelStatus);
				channelRow.Controls.Add(ingestURLCell);

				if (channel.State == ChannelState.Running)
				{
					// start channel
					var stopChannelButton = new Button();
					stopChannelButton.Text = "チャネル停止";
					stopChannelButton.ToolTip = channel.Name; 
					stopChannelButton.Click += StopChannelButton_Click;
					channelStatus.Controls.Add(stopChannelButton);

					var resetButton = new Button();
					resetButton.Text = "リセット";
					resetButton.ToolTip = channel.Name;
					resetButton.Click += ResetButton_Click;
					channelStatus.Controls.Add(resetButton);

					string PreviewURL = channel.Preview.Endpoints.FirstOrDefault().Url.AbsoluteUri;
					var previewPlayerURI = new Literal();
					previewPlayerURI.Text = BuildPlayerHTML(PreviewURL, 280, 500,
						@" data-setup='{""streamingFormats"": ""SMOOTH"", ""disableUrlRewriter"": true}'");
					var previewPlayer = new TableCell();
					previewPlayer.Width = Unit.Percentage(15);
					previewPlayer.Controls.Add(previewPlayerURI);

					channelRow.Controls.Add(previewPlayer);

					if (channel.Programs.Count() > 0)
					{
						var program = channel.Programs.FirstOrDefault();

                        var url = (from u in context.Locators
								   where u.AssetId == program.AssetId
								   select u).FirstOrDefault();

						var assets = (from a in context.Assets
									  where a.Id == channel.Programs.FirstOrDefault().AssetId
									  select a).FirstOrDefault();

						var assetFile = (from af in assets.AssetFiles
										 where af.Name.EndsWith("ism")
										 select af).FirstOrDefault();

						var originURL = new StringBuilder(512)
								.Append(new UriBuilder(url.Path).Uri.AbsoluteUri)
								.Append(assetFile.Name)
								.Append(@"/manifest")
								.ToString();

						var publishPlayerURI = new Literal();
						publishPlayerURI.Text = BuildPlayerHTML(originURL, 280, 500);
						var publishPlayerCell = new TableCell();
						publishPlayerCell.Width = Unit.Percentage(15);
						publishPlayerCell.Controls.Add(publishPlayerURI);

						var stopButton = new Button();
						stopButton.Text = "配信終了";
						stopButton.ToolTip = channel.Name;
						stopButton.Click += StopButton_Click; ;

						publishPlayerCell.Controls.Add(stopButton);
						channelRow.Controls.Add(publishPlayerCell);

					}
					else
					{
						var startProgramButton = new Button();
						startProgramButton.Text = "配信開始";
						startProgramButton.ToolTip = channel.Name;
						startProgramButton.Click += StartProgramButton_Click; ;

						previewPlayer.Controls.Add(startProgramButton);

					}

				}
				else
				{
					// start channel
					var startChannelButton = new Button();
					startChannelButton.Text = "チャネル開始";
					startChannelButton.ToolTip = channel.Name;
					startChannelButton.Click += StartChannelButton_Click;
					channelStatus.Controls.Add(startChannelButton);

				}

				tableListChannel.Controls.Add(channelRow);

			}

		}

		private void StartProgramButton_Click(object sender, EventArgs e)
		{
			var AccountName = ConfigurationManager.AppSettings["accountName"];
			var AccountKey = ConfigurationManager.AppSettings["accountKey"];
			var context = new CloudMediaContext(new MediaServicesCredentials(AccountName, AccountKey));
			var channel = queryChannel(((Button)sender).ToolTip);
			var asset = context.Assets.Create(channel.Name,
							AssetCreationOptions.None);
			var program = channel.Programs.Create(channel.Name,
									TimeSpan.FromHours(1),
									asset.Id);
			program.Start();

			var locator = context.Locators.CreateLocator(LocatorType.OnDemandOrigin,
						asset,
						context.AccessPolicies.Create(
							"live Streaming Policy",
							TimeSpan.FromDays(365),
							AccessPermissions.Read
						));
		}

		private void StartChannelButton_Click(object sender, EventArgs e)
		{
			var channel = queryChannel(((Button)sender).ToolTip);
			if (channel.Programs.Count() == 0
				&& channel.State == ChannelState.Stopped)
			{
				channel.Start();
			}
			ReRenderingPage();
		}

		private void StopButton_Click(object sender, EventArgs e)
		{
			var channel = queryChannel(((Button)sender).ToolTip);
			var program = channel.Programs.FirstOrDefault();
			program.Stop();
			program.DeleteAsync();

			ReRenderingPage();
		}

		private void ResetButton_Click(object sender, EventArgs e)
		{
			var channel = queryChannel(((Button)sender).ToolTip);
			if (channel.Programs.Count() == 0)
			{
				channel.Reset();
			}
			ReRenderingPage();
		}

		private void StopChannelButton_Click(object sender, EventArgs e)
		{
			var channel = queryChannel(((Button)sender).ToolTip);
			if (channel.Programs.Count() == 0)
			{
				channel.Stop();
			}
			ReRenderingPage();
		}

		private IChannel queryChannel(string ChannelName)
		{
			var AccountName = ConfigurationManager.AppSettings["accountName"];
			var AccountKey = ConfigurationManager.AppSettings["accountKey"];
			var context = new CloudMediaContext(new MediaServicesCredentials(AccountName, AccountKey));
			return (from a in context.Channels
						   where a.Name.ToLower() == ChannelName.ToLower()
						   select a).FirstOrDefault();
		}

		private string BuildPlayerHTML(string manifestURL,
					int height,
					int weight)
		{
			return BuildPlayerHTML(manifestURL, height, weight, "");
        }


		private string BuildPlayerHTML(string manifestURL,
					int height,
					int weight,
					string SourceOption)
		{
			return new StringBuilder(1024)
				.Append(@"<video class=""azuremediaplayer amp-default-skin"" autoplay controls width=""")
				.Append(weight)
				.Append(@""" height=""")
				.Append(height)
				.Append(@""" data-setup='{""nativeControlsForTouch"": false")
				.AppendLine(@"}'>")
				.Append(@"<source src =""")
				.Append(manifestURL)
				.Append(@""" type=""application/vnd.ms-sstr+xml"" ")
				.Append(SourceOption)
				.AppendLine(@" />")
				.AppendLine(@"<p class=""amp-no-js"">")
				.AppendLine(@"JavaScriptを有効化するか、HTML5ビデオのサポートされているブラウザーで視聴してください")
				.AppendLine(@"</p>")
				.AppendLine(@"</video>")
				.ToString();

		}
	}
}