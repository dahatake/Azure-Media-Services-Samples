using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
using System.Xml.Linq;
using System.Web;

namespace DynamicPackaging
{
	class Program
	{

		// Token creation related fields. 
		private const string MediaServicesAccessScope = "urn:Nimbus";
		private const string IssuerEndpoint = "https://nimbuslkgglobacs.accesscontrol.windows.net";
		private const string SignaturePrefix = "&HMACSHA256=";
		private const string SymmetricKeyString = "IRPQMJ006zlzV/Y1gbyoKJPKwLGOCAO7M5/17gfh4XU=";

		public const string Template =
			@"urn:microsoft:azure:mediaservices:contentkeyidentifier=CONTENTKEY&urn%3aServiceAccessible=service&http%3a%2f%2fschemas.microsoft.com%2faccesscontrolservice%2f2010%2f07%2fclaims%2fidentityprovider=https%3a%2f%2fnimbusvoddev.accesscontrol.windows.net%2f&Audience=SCOPE&ExpiresOn=EXPIRY&Issuer=ISSUER";

		private static readonly DateTime SwtBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
		private static readonly byte[] SymmetricKeyBytes = Convert.FromBase64String(SymmetricKeyString);
		//


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
				AssetCreationOptions.StorageEncrypted,
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
				MediaProcessorNames.AzureMediaEncoder,
				MediaEncoderTaskPresetStrings.H264AdaptiveBitrateMP4SetSD4x3, // エンコード設定
				// XML文字列全体の参照: http://msdn.microsoft.com/en-us/library/dn619392.aspx
				// Stitching: http://msdn.microsoft.com/en-us/library/dn640504.aspx
				asset,
				asset.Name + "-ForStreaming",
				AssetCreationOptions.StorageEncrypted);

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

			// 暗号化設定
			var key = CreateEnvelopeTypeContentKey(context, outputAsset);
			AddTokenRestrictedAuthorizationPolicy(context, key);
			CreateAssetDeliveryPolicy(context, outputAsset, key);

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

			Console.WriteLine();
			Console.WriteLine("全ての処理が終了しました。配信URLがデスクトップに出力されていますので、ご確認ください。");
			Console.WriteLine("総処理時間: {0}", totalSw.Elapsed.ToString());
			Console.WriteLine("何かキーを押してください。");
			Console.ReadLine();

		}

		// Dynamic Encryption用設定
		static private byte[] GetRandomBuffer(int size)
		{
			byte[] randomBytes = new byte[size];
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(randomBytes);
			}

			return randomBytes;
		}


		static public IContentKey CreateEnvelopeTypeContentKey(
									CloudMediaContext context,
									IAsset asset)
		{
			// Create envelope encryption content key
			Guid keyId = Guid.NewGuid();
			byte[] contentKey = GetRandomBuffer(16);

			IContentKey key = context.ContentKeys.Create(
									keyId,
									contentKey,
									"ContentKey",
									ContentKeyType.CommonEncryption);
			// Associate the key with the asset.
			asset.ContentKeys.Add(key);

			return key;
		}

		public static void AddTokenRestrictedAuthorizationPolicy(
								CloudMediaContext context,
								IContentKey contentKey)
		{
			// Token should be in the following XML form:
			//
			//<TokenRestriction issuer='https://some.accesscontrol.windows.net/'
			//        audience='https://licensedelivery.mediaservices.azure.com; >
			//    <VerificationKeys>
			//        <VerificationKey type='Symmetric'
			//            value='0RizXh/a2BYCSMi+ee7jdpbouJg2oDuJkC4PLv+XRkc='
			//            IsPrimary='true' />
			//    </VerificationKeys>
			//    <RequiredClaims>
			//        <Claim type='urn:microsoft:azure:mediaservices:contentkeyidentifier'/>
			//    </RequiredClaims>
			//</TokenRestriction>

			string tokenRequirements = GenerateTokenRequirements(
										"Symmetric",
										SymmetricKeyString, 
										true, true);

			IContentKeyAuthorizationPolicy policy = context.
									ContentKeyAuthorizationPolicies.
									CreateAsync("no restricted authorization policy").Result;

			List<ContentKeyAuthorizationPolicyRestriction> restrictions =
					new List<ContentKeyAuthorizationPolicyRestriction>();

			ContentKeyAuthorizationPolicyRestriction restriction =
					new ContentKeyAuthorizationPolicyRestriction
					{
						Name = "Token Authorization Policy",
						KeyRestrictionType = (int)ContentKeyRestrictionType.Open,
						Requirements = tokenRequirements
					};

			restrictions.Add(restriction);

			////You could have multiple options 
			//IContentKeyAuthorizationPolicyOption policyOption =
			//	context.ContentKeyAuthorizationPolicyOptions.Create(
			//		"No Authorization Option",
			//		ContentKeyDeliveryType.BaselineHttp,
			//		restrictions,
			//		null
			//		);

			//policy.Options.Add(policyOption);

			// Add ContentKeyAutorizationPolicy to ContentKey
			contentKey.AuthorizationPolicyId = policy.Id;
			contentKey.UpdateAsync();
		}

		static private string GenerateTokenRequirements(string verificationType,
							   string keyValue,
							   bool isPrimary,
							   bool isKeyVerificationRequired,
							   string scope = null,
							   string issuer = null)
		{
			if (string.IsNullOrEmpty(issuer))
			{
				issuer = IssuerEndpoint;
			}
			if (string.IsNullOrEmpty(scope))
			{
				scope = HttpUtility.UrlDecode(MediaServicesAccessScope);

			}

			var tokenRestriction = new XElement("TokenRestriction",
												new XAttribute("issuer", issuer),
												new XAttribute("audience", scope));

			tokenRestriction.Add(new XElement("VerificationKeys",
											  new XElement("VerificationKey",
														   new XAttribute("type", verificationType),
														   new XAttribute("value", keyValue),
														   new XAttribute("IsPrimary", isPrimary.ToString().ToLower()))));
			if (isKeyVerificationRequired)
			{
				tokenRestriction.Add(new XElement("RequiredClaims",
												  new XElement("Claim",
															   new XAttribute("type",
																			  "urn:microsoft:azure:mediaservices:contentkeyidentifier"))));
			}

			return tokenRestriction.ToString();
		}


		static public void CreateAssetDeliveryPolicy(CloudMediaContext context,
												IAsset asset, 
												IContentKey key)
		{
//			Uri HLSkeyAcquisitionUri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.PlayReadyLicense);
////			Uri PlayReadykeyAcquisitionUri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.PlayReadyLicense);

//			string envelopeEncryptionIV = Convert.ToBase64String(GetRandomBuffer(16));

//			// The following policy configuration specifies: 
//			//   key url that will have KID=<Guid> appended to the envelope and
//			//   the Initialization Vector (IV) to use for the envelope encryption.
//			Dictionary<AssetDeliveryPolicyConfigurationKey, string> assetDeliveryPolicyConfiguration =
//				new Dictionary<AssetDeliveryPolicyConfigurationKey, string>
//			{
//				{AssetDeliveryPolicyConfigurationKey.EnvelopeKeyAcquisitionUrl, HLSkeyAcquisitionUri.ToString()},
//				{AssetDeliveryPolicyConfigurationKey.EnvelopeEncryptionIVAsBase64, envelopeEncryptionIV},
//			};

			IAssetDeliveryPolicy assetDeliveryPolicy =
				context.AssetDeliveryPolicies.Create(
							"myAssetDeliveryPolicy",
							AssetDeliveryPolicyType.NoDynamicEncryption,
							AssetDeliveryProtocol.Dash | AssetDeliveryProtocol.HLS | AssetDeliveryProtocol.SmoothStreaming,
							null);

			// Add AssetDelivery Policy to the asset
			asset.DeliveryPolicies.Add(assetDeliveryPolicy);
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
