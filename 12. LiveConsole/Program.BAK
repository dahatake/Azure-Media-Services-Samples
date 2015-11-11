using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

using Microsoft.WindowsAzure.MediaServices.Client;

namespace AzureMediaServcicesLiveConsoleApp
{
	class Program
	{
		static string ChannelName = "dahatakeChannel"
					+ DateTime.Now.ToShortDateString().Replace("/", "")
					+ DateTime.Now.ToShortTimeString().Replace(":", "");

		static void Main(string[] args)
		{
			// 処理時間の計測
			var totalSw = new Stopwatch();
			totalSw.Start();
			var sw = new Stopwatch();

			var context = new CloudMediaContext(
				ConfigurationManager.AppSettings["accountName"],
				ConfigurationManager.AppSettings["accountKey"]
			);

			// 1. Channel 作成と開始
			Console.WriteLine("1. Channel 作成");

			sw.Start();
			var channel = context.Channels.Create(
				new ChannelCreationOptions(
					ChannelName,
					StreamingProtocol.RTMP, //RTMPで取り込み実施
					new List<IPRange>
					{
						new IPRange
						{
							Name = "All OK",
							Address = IPAddress.Parse("0.0.0.0"),
							SubnetPrefixLength = 0
						}
					}
				));

			sw.Stop();
			Console.WriteLine("   Channel 作成時間: {0}", sw.Elapsed.ToString());

			Console.WriteLine("2. Channel 開始");
			sw.Reset();
			sw.Start();
			channel.Start();
			sw.Stop();
			Console.WriteLine("   Channel 開始完了時間: {0}", sw.Elapsed.ToString());

			WriteToFile(
					ChannelName + "_取り込みURL.txt",
					channel.Input.Endpoints.FirstOrDefault().Url.ToString());
			WriteToFile(
					ChannelName + "_PreviewURL.txt",
					channel.Preview.Endpoints.FirstOrDefault().Url.ToString());

			Console.WriteLine("Azure上でのエンコーダーからの取り込みの準備が完了しました。");
			Console.WriteLine(" ***URL出力先 (デスクトップにあります)***");
			Console.WriteLine("    " + ChannelName + "_取り込みURL.txt ファイル");
			Console.WriteLine("    " + ChannelName + "_PreviewURL.txt ファイル");
			Console.WriteLine("");
			Console.WriteLine("[次の手順]");
			Console.WriteLine("エンコーダーからAzureへの配信を開始して、Azureに上で映像を受信できているのを確認してください。");
			Console.WriteLine("確認後、Entry Keyを押すことで、Azureからインターネットへ配信を開始します。");
			Console.ReadLine();

			Console.WriteLine("3. Program 作成");
			sw.Reset();
			sw.Start();

			// 2. アーカイブ および DVRWindow のための Asset 作成
			var archiveAsset = context.Assets.Create(
				ChannelName + "_Archive",
				AssetCreationOptions.None);

			// 3. プログラム作成と開始
			var program = channel.Programs.Create(
					ChannelName + "Program",
					TimeSpan.FromMinutes(5), //アーカイブ時間
					archiveAsset.Id);
			sw.Stop();
			Console.WriteLine("   Program 作成時間: {0}", sw.Elapsed.ToString());

			Console.WriteLine("4. Program 開始");
			sw.Reset();
			sw.Start();
			program.Start();
			sw.Stop();
			Console.WriteLine("   Program 開始完了時間: {0}", sw.Elapsed.ToString());

			// 4. Locator作成
			Console.WriteLine("5. Locator 作成");
			sw.Reset();
			sw.Start();
			var locator = context.Locators.CreateLocator(
				LocatorType.OnDemandOrigin,
				archiveAsset,
				context.AccessPolicies.Create(
					"Live Streaming",
					TimeSpan.FromHours(2), // コンテンツの公開期間: 1時間					
					AccessPermissions.Read
				));

			sw.Stop();
			Console.WriteLine("   Locator 作成時間: {0}", sw.Elapsed.ToString());

			WriteToFile(
					ChannelName + "_発行URL(Smooth).txt",
					locator.GetSmoothStreamingUri().AbsoluteUri);

			Console.WriteLine();
			Console.WriteLine("全ての処理が終了しました。発行URLがデスクトップに出力されていますので、ご確認ください。");
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
