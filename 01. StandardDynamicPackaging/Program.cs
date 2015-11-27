using System;
using System.Configuration;
using System.Diagnostics;

using System.IO;

using Microsoft.WindowsAzure.MediaServices.Client;

namespace DynamicPackaging
{
	class Program
	{
		static void Main(string[] args)
		{
			var targetFile = new FileInfo(ConfigurationManager.AppSettings["uploadfile"]);

			// 処理時間の計測
			var totalSw = new Stopwatch();
			totalSw.Start();

			var sw = new Stopwatch();

			Console.WriteLine("***** 1. Azure メディアサービス 接続 *****");
			var context = new CloudMediaContext(
					ConfigurationManager.AppSettings["accountName"],
					ConfigurationManager.AppSettings["accountKey"]
					);
			context.ParallelTransferThreadCount = 10;
			context.NumberOfConcurrentTransfers = 5;

			Console.WriteLine("***** 2. ファイルアップロード *****");
			sw.Start();

			var asset = context.Assets.CreateFromFile(
				targetFile.FullName,
				AssetCreationOptions.None,
				(a, p) =>
				{
					Console.WriteLine("  経過 {0}%", p.Progress);
				});

			// --- この段階でファイルの転送が完了 ---
			// ***** 非同期実行終わり ****

			sw.Stop();
			Console.WriteLine("  アップロード処理完了");
			Console.WriteLine("  アップロード処理時間: {0}", sw.Elapsed.ToString());
			sw.Reset();

			Console.WriteLine("***** 3. トランスコード *****");
			var outputAsset = asset;

			Console.WriteLine("  トランスコード開始");
			// 3.b.1. ジョブ作成
			var job = context.Jobs.CreateWithSingleTask(
				"Media Encoder Standard",
				"H264 Multiple Bitrate 4x3 SD", 
					// エンコード設定 https://msdn.microsoft.com/en-us/library/azure/mt269960.aspx
					// Stitching: http://msdn.microsoft.com/en-us/library/dn640504.aspx
				asset,
				asset.Name + "-ForStreaming",
				AssetCreationOptions.None);

			sw.Start();
			// 3.b.2. ジョブ実行.
			job.Submit();
			job = job.StartExecutionProgressTask(
				j =>
				{
					Console.WriteLine("   状態: {0}", j.State);
					Console.WriteLine("   経過: {0:0.##}%", j.GetOverallProgress());
				},
				System.Threading.CancellationToken.None).Result;


			sw.Stop();
			Console.WriteLine("  トランスコード完了");
			Console.WriteLine("  トランスコード時間: {0}", sw.Elapsed.ToString());
			outputAsset = job.OutputMediaAssets[0];

			sw.Reset();

			Console.WriteLine("***** 3. 配信 *****");
			// Adaptive ストリーミング
			context.Locators.CreateLocator(
				LocatorType.OnDemandOrigin,
				outputAsset,
				context.AccessPolicies.Create(
							"Streaming Access Policy",
							TimeSpan.FromDays(7),
							AccessPermissions.Read)
			);

			// 5. URLを出力
			WriteToFile(string.Format("Smooth_{0}.txt", asset.Name),
				outputAsset.GetSmoothStreamingUri().AbsoluteUri);
			WriteToFile(string.Format("HLS_{0}.txt", asset.Name),
				outputAsset.GetHlsUri().AbsoluteUri);
			WriteToFile(string.Format("DASH_{0}.txt", asset.Name),
				outputAsset.GetMpegDashUri().AbsoluteUri);


			Console.WriteLine();
			Console.WriteLine("全ての処理が終了しました。配信URLがデスクトップに出力されていますので、ご確認ください。");
			Console.WriteLine("総処理時間: {0}", totalSw.Elapsed.ToString());
			Console.WriteLine("何かキーを押してください。");
			Console.ReadLine();

		}

		/// <summary>
		/// Utility: 文字列のファイル出力
		/// </summary>
		/// <param name="outFileName"></param>
		/// <param name="fileContent"></param>
		static void WriteToFile(string outFileName, string fileContent)
		{

			System.IO.StreamWriter sr = System.IO.File.CreateText(
				Environment.GetEnvironmentVariable("USERPROFILE") +
				@"\Desktop\" +
				outFileName);
			sr.Write(fileContent);
			sr.Flush();
			sr.Close();
		}
	}
}
