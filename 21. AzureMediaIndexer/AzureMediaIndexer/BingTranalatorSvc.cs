using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Web;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Threading;

namespace AzureMediaIndexer
{
	// http://blogs.msdn.com/b/translation/p/gettingstarted1.aspx

	class BingTranalatorSvc
	{
		public string from	= "en";
		public string to	= "ja";

		// Azure MarketPlace と紐づけ
		//Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
		public string clientID		= "";
		public string clientSecret	= "";
		
		public string Translate(string text)
		{
			string result = "";

			// BingTranslator アクセス用のアクセストークンをAzure DataMarketから取得
			var accessToken = new AdmAuthentication(clientID, clientSecret);
			
			string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + System.Web.HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to;
			string authToken = "Bearer " + accessToken.GetAccessToken().access_token;

			// Microsoft Translator呼び出し
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
			httpWebRequest.Headers.Add("Authorization", authToken);

			// 結果取得
			WebResponse response = null;
			response = httpWebRequest.GetResponse();
			using (Stream stream = response.GetResponseStream())
			{
				System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
				result = (string)dcs.ReadObject(stream);
			}

			return result;
		}

		// Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 

		[DataContract]
		public class AdmAccessToken
		{
			[DataMember]
			public string access_token { get; set; }
			[DataMember]
			public string token_type { get; set; }
			[DataMember]
			public string expires_in { get; set; }
			[DataMember]
			public string scope { get; set; }
		}
		public class AdmAuthentication
		{
			public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
			private string clientId;
			private string clientSecret;
			private string request;
			private AdmAccessToken token;
			private Timer accessTokenRenewer;
			//Access token expires every 10 minutes. Renew it every 9 minutes only.
			private const int RefreshTokenDuration = 9;
			public AdmAuthentication(string clientId, string clientSecret)
			{
				this.clientId = clientId;
				this.clientSecret = clientSecret;
				//If clientid or client secret has special characters, encode before sending request
				this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));
				this.token = HttpPost(DatamarketAccessUri, this.request);
				//renew the token every specfied minutes
				accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback), this, TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
			}
			public AdmAccessToken GetAccessToken()
			{
				return this.token;
			}
			private void RenewAccessToken()
			{
				AdmAccessToken newAccessToken = HttpPost(DatamarketAccessUri, this.request);
				//swap the new token with old one
				//Note: the swap is thread unsafe
				this.token = newAccessToken;
			}
			private void OnTokenExpiredCallback(object stateInfo)
			{
				try
				{
					RenewAccessToken();
				}
				catch (Exception ex)
				{
					Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
				}
				finally
				{
					try
					{
						accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
					}
					catch (Exception ex)
					{
						Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
					}
				}
			}
			private AdmAccessToken HttpPost(string DatamarketAccessUri, string requestDetails)
			{
				//Prepare OAuth request 
				WebRequest webRequest = WebRequest.Create(DatamarketAccessUri);
				webRequest.ContentType = "application/x-www-form-urlencoded";
				webRequest.Method = "POST";
				byte[] bytes = Encoding.ASCII.GetBytes(requestDetails);
				webRequest.ContentLength = bytes.Length;
				using (Stream outputStream = webRequest.GetRequestStream())
				{
					outputStream.Write(bytes, 0, bytes.Length);
				}
				using (WebResponse webResponse = webRequest.GetResponse())
				{
					DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
					//Get deserialized object from JSON stream
					AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
					return token;
				}
			}
			
		}
	}
}
