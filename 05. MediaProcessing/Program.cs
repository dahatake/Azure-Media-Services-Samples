using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GeneralMediaProcessing
{
	class Program
	{
		static void Main(string[] args)
		{
			var MPtype = ConfigurationManager.AppSettings["MPType"];
			var targetFile = new FileInfo(ConfigurationManager.AppSettings["uploadfile"]);
			var downloadLocation = new FileInfo(ConfigurationManager.AppSettings["downloadLocation"]);
			var DoDownload = Boolean.Parse(new FileInfo(ConfigurationManager.AppSettings["DoDownload"]).ToString());

			// 処理時間の計測
			var totalSw = new Stopwatch();
			totalSw.Start();

			Console.WriteLine("***** 1. Azure メディアサービス 接続 *****");
			var context = new CloudMediaContext(
					ConfigurationManager.AppSettings["accountName"],
					ConfigurationManager.AppSettings["accountKey"]
					);

			Console.WriteLine("***** 2. ファイルアップロード *****");
			var asset = context.Assets.CreateFromFile(
				targetFile.FullName,
				AssetCreationOptions.None,
				(a, p) =>
				{
					Console.WriteLine("  経過 {0}%", p.Progress);
				});


			Console.WriteLine("***** 3. Media Processor 実行 *****");
			var mediaProcessor = (from mp in context.MediaProcessors
								  where mp.Name.Contains(MPtype)
								  orderby mp.Name
								  select mp
								  ).First();
			var configText = File.ReadAllText("config" + MPtype.Replace(" ","") + ".txt");

			var job = context.Jobs.CreateWithSingleTask(
				mediaProcessor.Name,
				configText,
				asset,
				asset.Name + "-" + MPtype.Replace(" ", "") + "-Output",
				AssetCreationOptions.None);
			job.Submit();
			job = job.StartExecutionProgressTask(j =>
			{
				Console.WriteLine("   状態: {0}", j.State);
				Console.WriteLine("   経過: {0:0.##}%", j.GetOverallProgress());
			},
			System.Threading.CancellationToken.None).Result;

			var outputAsset = job.OutputMediaAssets[0];

			Console.WriteLine("***** 4. ダウンロード用設定 *****");

			var locatorType = LocatorType.Sas;
			if (MPtype.Equals("Encoder Standard")) {
				locatorType = LocatorType.OnDemandOrigin;
			}

			context.Locators.CreateLocator(
				locatorType,
				outputAsset,
				context.AccessPolicies.Create(
					"Streaming Access Policy",
					TimeSpan.FromDays(7),
				AccessPermissions.Read)
			);

			if (DoDownload && !MPtype.Equals("Encoder Standard"))
			{
				Console.WriteLine("***** 5. ファイルダウンロード *****");
				outputAsset.DownloadToFolder(downloadLocation.FullName);
			}

			Console.WriteLine("***** 処理終了 *****");
			Console.WriteLine("総処理時間: {0}", totalSw.Elapsed.ToString());
			Console.WriteLine("何かキーを押してください。");
			Console.ReadLine();


		}
	}
}
