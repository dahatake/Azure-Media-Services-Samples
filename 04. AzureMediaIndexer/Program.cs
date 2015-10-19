using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;


using Microsoft.WindowsAzure.MediaServices.Client;


namespace AzureMediaIndexer
{
	class Program
	{

		private static string TranslatorClientID = ConfigurationManager.AppSettings["TranslatorClientID"];
		private static string TranslatorClientSecret = ConfigurationManager.AppSettings["TranslatorClientSecret"];
		private static string language = ConfigurationManager.AppSettings["language"];
		private static string IndexingConfigurationFile = ConfigurationManager.AppSettings["IndexingConfigurationFile"];
		private static string uploadFile = ConfigurationManager.AppSettings["uploadfile"];
		
		static void Main(string[] args)
		{
			// 処理時間の計測
			var totalSw = new Stopwatch();
			var sw = new Stopwatch();

			totalSw.Start();

			var context = new CloudMediaContext(
					ConfigurationManager.AppSettings["accountName"],
					ConfigurationManager.AppSettings["accountKey"]
					);

			Console.WriteLine("*** 1. ファイルアップロード ***");
			sw.Start();

			var asset = context.Assets.CreateFromFile(
				uploadFile,
				AssetCreationOptions.None,
				(a, p) =>
				{
					Console.WriteLine("  経過 {0}%", p.Progress);
				});

			sw.Stop();
			Console.WriteLine("  アップロード処理完了");
			Console.WriteLine("  アップロード処理時間: {0}", sw.Elapsed.ToString());

			Console.WriteLine("*** 2. Indexing 実行 ***");

			// 2.1. ジョブ作成
			var configuration = File.ReadAllText(IndexingConfigurationFile);

			var job = context.Jobs.CreateWithSingleTask(
				"Azure Media Indexer",
				configuration,
				asset,
				asset.Name + "-Indexed",
				AssetCreationOptions.None);

			sw.Reset();
			sw.Start();
			// 2.2. ジョブ実行.
			job.Submit();
			job = job.StartExecutionProgressTask(
				j =>
				{
					Console.WriteLine("   状態: {0}", j.State);
					Console.WriteLine("   経過: {0:0.##}%", j.GetOverallProgress());
				},
				System.Threading.CancellationToken.None).Result;

			sw.Stop();
			Console.WriteLine("  Indexing 完了");
			Console.WriteLine("  Indexing 時間: {0}", sw.Elapsed.ToString());
			var outputAsset = job.OutputMediaAssets.FirstOrDefault();

			Console.WriteLine("***** 3. 配信ポイント作成 *****");
			// Progressive Download
			context.Locators.CreateLocator(
				LocatorType.Sas,
				asset,
				context.AccessPolicies.Create(
							"Streaming Access Policy",
							TimeSpan.FromDays(7),
							AccessPermissions.Read)
			);

			WriteToFile(String.Format("{0}_SASURL.txt",
										asset.Name),
						asset.AssetFiles.FirstOrDefault().GetSasUri().ToString());

			Console.WriteLine("*** 4. ファイルダウンロード ***");
			sw.Reset();
			sw.Start();

			var TTMLFile = "";
			foreach (IAssetFile file in outputAsset.AssetFiles)
			{
				Console.WriteLine(" ファイルダウンロード中: {0}", file.Name);
				file.Download(Environment.GetEnvironmentVariable("USERPROFILE") +
					@"\Desktop\" +
					file.Name);

				// ttml は翻訳対象
				if (file.Name.EndsWith(".ttml"))
				{
					TTMLFile = Environment.GetEnvironmentVariable("USERPROFILE")
						+ @"\Desktop\"
						+ file.Name;
				}

			}

			Console.WriteLine("*** 5. Microsoft Translator での ttml 機械翻訳 ***");
			sw.Reset();
			sw.Start();


			// Translator Language Codes:
			// http://msdn.microsoft.com/en-us/library/hh456380.aspx
			var langs = language.Split(',');

			BingTranalatorSvc translator = new BingTranalatorSvc();
			translator.clientID = TranslatorClientID;
			translator.clientSecret = TranslatorClientSecret;
			int i = 0;

			foreach (var lang in langs)
			{
				var TTMLjp = XDocument.Load(TTMLFile);
				XNamespace ttmlns = "http://www.w3.org/ns/ttml"; //LINQ to XMLでクエリするためには、xmlnsの指定は必須
				var transTargets = from p in TTMLjp.Descendants(ttmlns + "p")
								   select p;
				translator.to = lang;

				foreach (var item in transTargets)
				{
					try
					{
						Console.WriteLine("  機械翻訳: {0}:{1}/{2}", 
							lang,
							i, 
							item.Value);

						item.SetValue(translator.Translate(item.Value));
						i++;
					}
					catch (WebException we)
					{
						ProcessWebException(we);
						break;
					}
				}

				TTMLjp.Save(TTMLFile.Replace(".ttml", 
					string.Format(".{0}.ttml",lang)));
				Console.WriteLine("  機械翻訳 完了");
				Console.WriteLine("  機械翻訳数:  {0}:{1}/{2}",
							lang,
							i,
							transTargets.Count());
		 
			}

			Console.WriteLine("  機械翻訳時間: {0}", sw.Elapsed.ToString());

			
			Console.WriteLine();
			Console.WriteLine("全ての処理が終了しました。Indexing 結果がデスクトップに出力されていますので、ご確認ください。");
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


		private static void ProcessWebException(WebException e)
		{
			Console.WriteLine(">>> Error: {0}", e.ToString());
			// Obtain detailed error information
			string strResponse = string.Empty;
			using (HttpWebResponse response = (HttpWebResponse)e.Response)
			{
				using (Stream responseStream = response.GetResponseStream())
				{
					using (StreamReader sr = new StreamReader(responseStream, System.Text.Encoding.ASCII))
					{
						strResponse = sr.ReadToEnd();
					}
				}
			}
			Console.WriteLine(">>> Http status code={0}, error message={1}", e.Status, strResponse);
		}



	}
}
