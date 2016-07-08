using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace BulkIngest
{
	class Program
	{
		static void Main(string[] args)
		{

#region Configuration
			// アップロード対象ファイルが格納されいているディレクトリ
			DirectoryInfo ingestDirectory = new DirectoryInfo(
				ConfigurationManager.AppSettings["watchFoler"]);
			StringBuilder buffer = new StringBuilder(1024);			
#endregion

			// 0. Windows Azure Media Services 接続
			var context = new CloudMediaContext(
					ConfigurationManager.AppSettings["accountName"],
					ConfigurationManager.AppSettings["accountKey"]
					);

			// 1. IngestManifest 作成
			var manifest = context.IngestManifests.Create(
				ingestDirectory.Name);

			buffer.AppendLine(string.Format("manifest id : {0}", manifest.Id));
			buffer.AppendLine(string.Format("manifest BlobStorageUriForUpload: {0}", manifest.BlobStorageUriForUpload));

			// 2. Asset 作成
			var asset = context.Assets.Create(ingestDirectory.Name, AssetCreationOptions.None);


			string[] fileList = new String[ingestDirectory.GetFiles().Count() ];
			int i = 0;
			// 3. IngestManifestAsset 作成
			foreach (var item in ingestDirectory.EnumerateFiles())
			{
				
				fileList[i] = item.Name;
				if (i == 1)
				{
					asset.Name = item.Name;
					asset.Update();
				}
				i++;

				buffer.AppendLine(string.Format("File : {0}", item.Name));
				buffer.AppendLine(string.Format("Asset id :{0}", asset.Id));
			}

			manifest.IngestManifestAssets.Create(asset, fileList);

			// IngestManifestID, AssetID 出力
			WriteToFile(Environment.GetEnvironmentVariable("USERPROFILE")
					+ @"\Desktop\manifest.txt",
					buffer.ToString());
			
			Console.WriteLine("** IngestManifest作成完了: ID {0}", manifest.Id);
			Console.WriteLine("   BlobStorageUriForUpload: {0}", manifest.BlobStorageUriForUpload);
			Console.WriteLine("** 別ツールでのファイルの転送を開始してください。ファイル転送状況を監視します。[Enter]キーを押してください");
			Console.ReadLine();

			/// 4. マニフェスト監視
			bool isFinished = true;
			while (isFinished)
			{
				manifest = context.IngestManifests.Where(m => m.Id == manifest.Id).FirstOrDefault();

				Console.WriteLine("** ファイル転送監視中 - {0}", DateTime.Now.ToLongTimeString());
				Console.WriteLine("  PendingFilesCount  : {0}", manifest.Statistics.PendingFilesCount);
				Console.WriteLine("  FinishedFilesCount : {0}", manifest.Statistics.FinishedFilesCount);
				Console.WriteLine("  {0}% complete.\n", (float)manifest.Statistics.FinishedFilesCount / (float)(manifest.Statistics.FinishedFilesCount + manifest.Statistics.PendingFilesCount) * 100);

				if (manifest.Statistics.PendingFilesCount == 0)
				{
					Console.WriteLine("ファイル転送完全完了!");
					isFinished = false;
					break;
				}

				if (manifest.Statistics.FinishedFilesCount < manifest.Statistics.PendingFilesCount)
					Console.WriteLine("10秒待機します。");
				Thread.Sleep(10000);
			}

			Console.WriteLine("全ての処理が終了しました。");
			Console.WriteLine("Enter キーを押してください");
			Console.ReadLine();

		}

		private static void WriteToFile(string outFilePath, string fileContent)
		{
			StreamWriter sr = File.CreateText(outFilePath);
			sr.Write(fileContent);
			sr.Close();
		}

	}
}
