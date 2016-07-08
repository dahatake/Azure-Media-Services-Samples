using System;
using System.Configuration;
using System.Linq;
using System.IO;

using Microsoft.WindowsAzure.MediaServices.Client;

using VideoEditor.Models;
using System.Xml.Linq;

namespace VideoEditor.Processor
{
    class AzureMediaServicesEncoderStandard
    {

        public string EncodeAsset( EncodeConfig config )
        {
            var context = new CloudMediaContext(
                            ConfigurationManager.AppSettings["accountName"],
                            ConfigurationManager.AppSettings["accountKey"]
                        );

            // Search asset from Streaming URL
            var startP = config.Source.IndexOf("/", 8);
            var endP = config.Source.IndexOf("/", startP + 1);
            var queryID = "nb:lid:UUID:" + config.Source.Substring(startP + 1, endP - startP - 1);

            var locator = (from l in context.Locators
                           where l.Id == queryID
                           select l).FirstOrDefault();
            var asset = locator.Asset;

            var configStartP = config.Source.IndexOf("(");
            var smoothURL = config.Source;
            if (configStartP > 0)
            {
                smoothURL = smoothURL.Substring(0, configStartP);
            }

            // Calculate start/duration for encoder
            var offset = GetManifestTimingData(smoothURL);

            var startTime = "";
            if (offset.IsLive)
            {
                startTime = TimeSpan.FromMilliseconds(offset.TimestampOffset).ToString();
            } else
            {
                startTime = TimeSpan.FromSeconds(double.Parse(config.StartTime)).ToString();
            }
            var duration = TimeSpan.FromSeconds(double.Parse(config.EndTime) - double.Parse(config.StartTime)).ToString();

            var encodingConfigParameter = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + (@"/Config/encodingConfigTemplate.txt")).
                Replace("###starttime###", startTime).
                Replace("###duration###", duration);

            //// MSE job
            var job = context.Jobs.CreateWithSingleTask(
                "Media Encoder Standard",
                encodingConfigParameter,
                asset,
                config.Title,
                AssetCreationOptions.None);

            job.Submit();
            job = job.StartExecutionProgressTask(
                j =>
                {
                        //TODO: show encode progress
                },
                System.Threading.CancellationToken.None).Result;

            if (job.State != JobState.Finished)
            {
                return "";
            }
            var outputAsset = job.OutputMediaAssets[0];

            // Prepare streaming
            context.Locators.CreateLocator(
                LocatorType.OnDemandOrigin,
                outputAsset,
                context.AccessPolicies.Create(
                            "Streaming Access Policy",
                            TimeSpan.FromDays(7),
                            AccessPermissions.Read)
            );

            return outputAsset.GetSmoothStreamingUri().AbsoluteUri;

        }

        public ManifestTimingData GetManifestTimingData(string playerURI)
        {
            ManifestTimingData response = new ManifestTimingData() { IsLive = false, Error = false, TimestampOffset = 0 };

            try
            {
                XDocument manifest = XDocument.Load(playerURI);
                var smoothmedia = manifest.Element("SmoothStreamingMedia");
                var videotrack = smoothmedia.Elements("StreamIndex").Where(a => a.Attribute("Type").Value == "video");

                // TIMESCALE
                string timescalefrommanifest = smoothmedia.Attribute("TimeScale").Value;
                if (videotrack.FirstOrDefault().Attribute("TimeScale") != null) // there is timescale value in the video track. Let's take this one.
                {
                    timescalefrommanifest = videotrack.FirstOrDefault().Attribute("TimeScale").Value;
                }
                ulong timescale = ulong.Parse(timescalefrommanifest);
                response.TimeScale = (timescale == TimeSpan.TicksPerSecond) ? null : (ulong?)timescale; // if 10000000 then null (default)

                // Timestamp offset
                if (videotrack.FirstOrDefault().Element("c").Attribute("t") != null)
                {
                    response.TimestampOffset = ulong.Parse(videotrack.FirstOrDefault().Element("c").Attribute("t").Value);
                }
                else
                {
                    response.TimestampOffset = 0; // no timestamp, so it should be 0
                }

                if (smoothmedia.Attribute("IsLive") != null && smoothmedia.Attribute("IsLive").Value == "TRUE")
                { // Live asset.... No duration to read (but we can read scaling and compute duration if no gap)
                    response.IsLive = true;

                    long duration = 0;
                    long r, d;
                    foreach (var chunk in videotrack.Elements("c"))
                    {
                        if (chunk.Attribute("t") != null)
                        {
                            duration = long.Parse(chunk.Attribute("t").Value) - (long)response.TimestampOffset; // new timestamp, perhaps gap in live stream....
                        }
                        d = chunk.Attribute("d") != null ? long.Parse(chunk.Attribute("d").Value) : 0;
                        r = chunk.Attribute("r") != null ? long.Parse(chunk.Attribute("r").Value) : 1;
                        duration += d * r;
                    }
                    response.AssetDuration = TimeSpan.FromSeconds((double)duration / ((double)timescale));
                }
                else
                {
                    ulong duration = ulong.Parse(smoothmedia.Attribute("Duration").Value);
                    response.AssetDuration = TimeSpan.FromSeconds((double)duration / ((double)timescale));
                }
            }
            catch
            {
                response.Error = true;
            }
            return response;
        }


        public class ManifestTimingData
        {
            public TimeSpan AssetDuration { get; set; }
            public ulong TimestampOffset { get; set; }
            public ulong? TimeScale { get; set; }
            public bool IsLive { get; set; }
            public bool Error { get; set; }
        }

    }
}
