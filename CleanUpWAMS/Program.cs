using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

using Microsoft.WindowsAzure.MediaServices.Client;

namespace CleanUpWAMS
{
	class Program
	{

		private static CloudMediaContext _context = null;
		private static readonly string _accountName01 = ConfigurationManager.AppSettings["accountName01"];
		private static readonly string _accountKey01 = ConfigurationManager.AppSettings["accountKey01"];
		private static readonly string _accountName02 = ConfigurationManager.AppSettings["accountName02"];
		private static readonly string _accountKey02 = ConfigurationManager.AppSettings["accountKey02"];

		static void Main(string[] args)
		{
			Console.WriteLine("start: {0}", _accountName01 );
			CleanUpAllAll(_accountName01, _accountKey01);
            if (_accountName02.Length > 0)
            {
                Console.WriteLine("start: {0}", _accountName02);
                CleanUpAllAll(_accountName02, _accountKey02);
            }
			Console.ReadLine();
		}

		static void CleanUpAllAll(string accountname, string accountKey )
		{
			// Windows Azure Media Services と接続
			_context = new CloudMediaContext(accountname, accountKey);
			_context.ParallelTransferThreadCount = 100;

			Console.WriteLine("Access Policy 削除中...");
			DeleteAllAccessPolicies();
			Console.WriteLine("Job 削除中...");
			DeleteAllJob();
			Console.WriteLine("Manifest 削除中");
			DeleteAllManigests();

			Console.WriteLine("Channel 削除中");
			foreach (var channel in _context.Channels)
			{
				DeleteAllChannel(channel);
			}

			Console.WriteLine("AssetDeliveryPolicies 削除中");
			DeleteAllAssetDeliveryPolicies();

			Console.WriteLine("Asset 削除中...");
			DeleteAllAssets();

			Console.WriteLine("");
			Console.WriteLine("----------------------------------------------------");
			Console.WriteLine("全ての処理が終了しました。");
			Console.WriteLine("----------------------------------------------------");

		}

		private static void DeleteAllAssetDeliveryPolicies()
		{
			foreach (var item in _context.AssetDeliveryPolicies)
			{
				try {
					item.Delete();
				} catch (Exception e){
					Console.WriteLine(e.Message);
				}
			}
		}

		static void DeleteAllAssets()
		{
			foreach (IAsset asset in _context.Assets)
			{
				Exception ex = null;

				// Use a try/catch block to handle deletes. 
				try
				{
					// You must revoke all locators to delete an asset. 
					foreach (ILocator locator in asset.Locators)
					{
						locator.Delete();
					}

					foreach (var item in asset.DeliveryPolicies)
					{
						asset.DeliveryPolicies.Remove(item);
						if (asset.DeliveryPolicies.Count == 0) break;
						item.Delete();
					}

					foreach (IContentKey contentKey in asset.ContentKeys)
					{
						asset.ContentKeys.Remove(contentKey);
						if (asset.ContentKeys.Count == 0) break;

					}


					asset.Delete();
					Console.WriteLine(" Assets has been deleted.");
				}

				catch (InvalidOperationException invalidEx)
				{
					ex = invalidEx;
				}
				catch (ArgumentNullException nullEx)
				{
					ex = nullEx;
				}
				// If there's an exception, log or notify as needed.
				
				if (ex != null)
				{
					// Log or notify of assets that cannot be deleted. 
					Console.WriteLine(" The current asset cannot be deleted.");
					if (asset != null)
						Console.WriteLine(" Asset Id: " + asset.Id);
					Console.WriteLine(" Reason asset could not be deleted: ");
					Console.Write(ex.Message);
				}
			}
		}

		static void DeleteAllAccessPolicies()
		{
			foreach (var policy in _context.AccessPolicies)
				try
				{
					policy.Delete();
					Console.WriteLine(" Access Policy has been deleted.");
				}
				catch (Exception e)
				{
					//
				}
		}

		static void DeleteAllJob()
		{
			foreach (var currentJob in _context.Jobs)
			{
				bool jobDeleted = false;
				string jobID = currentJob.Id;

				while (!jobDeleted)
				{

					IJob theJob = GetJob(_context, jobID);
					switch (theJob.State)
					{
						case JobState.Finished:
						case JobState.Canceled:
							theJob.Delete();
							jobDeleted = true;
							Console.WriteLine(" Job has been deleted.");
							break;
						case JobState.Canceling:
							Console.WriteLine(" Job is cancelling and will be deleted "
								+ "when finished.");
							Console.WriteLine(" Wait while job finishes canceling...");
							Thread.Sleep(5000);
							break;
						case JobState.Queued:
						case JobState.Scheduled:
						case JobState.Processing:
							theJob.Cancel();
							Console.WriteLine(" Job is pending or processing and will "
								+ "be canceled, then deleted.");
							break;
						case JobState.Error:
							// Log error as needed.
							Console.WriteLine(" Error Job");
							Console.WriteLine(" Assets   :"
								+ theJob.InputMediaAssets[0].Name);
							foreach (var task in theJob.Tasks)
							{
								if (task.ErrorDetails.Count > 0)
								{
									Console.WriteLine(" TaskName: {0}", task.Name);
									foreach (var error in task.ErrorDetails)
									{
										Console.WriteLine(" Message: {0}", error.Message);
									}
								}
							}
							theJob.Delete();
							jobDeleted = true;
							break;
						default:
							break;
					}

				}
			}

		}

		static void DeleteAllManigests()
		{
			foreach (var item in _context.IngestManifests)
			{
				item.Delete();
				Console.WriteLine(" Manifest has been deleted.");
			}

		}

		private static IJob GetJob(CloudMediaContext context,
						string jobId)
		{
			// You sometimes need to query for a fresh 
			// reference to a job during threaded operations. 

			// Use a Linq select query to get an updated 
			// reference by Id. 
			var job =
				from j in context.Jobs
				where j.Id == jobId
				select j;
			// Return the job reference as an Ijob. 
			IJob theJob = job.FirstOrDefault();

			// Confirm whether job exists, and return. 
			if (theJob != null)
			{
				return theJob;
			}
			else
				Console.WriteLine("Job does not exist.");
			return null;

		}

		public static void DeleteAllChannel(IChannel channel)
		{

			IAsset asset;
			if (channel != null)
			{
				foreach (var program in channel.Programs)
				{
					asset = _context.Assets.Where(se => se.Id == program.AssetId)
											.FirstOrDefault();

					// To end your event, stop the Program which will cause it to stop pushing the stream into your asset.
					// After you stop the event, the stream will be available for on-demand viewing using the same URLs.
					if (program.State == ProgramState.Running)
					{
						program.Stop();
					}
					program.Delete();

					// Delete the asset if you do not want to keep it for on-demand viewing.
					if (asset != null)
					{
						foreach (var l in asset.Locators)
							l.Delete();

						asset.Delete();
					}
				}

				if (channel.State == ChannelState.Running)
				{
					channel.Stop();
				}
				channel.Delete();
			}
		}


	}
}
