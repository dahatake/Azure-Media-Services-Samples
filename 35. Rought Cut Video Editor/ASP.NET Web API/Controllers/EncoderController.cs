using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using VideoEditor.Models;
using VideoEditor.Processor;

namespace VideoEditor.Controllers
{
    public class EncoderController : ApiController
    {
        // GET: api/Encoder
        public string Get()
        {
            AzureMediaServicesEncoderStandard encoder = new AzureMediaServicesEncoderStandard();
            var queryStrings = Request.Properties["MS_QueryNameValuePairs"] as IEnumerable<KeyValuePair<string, string>>;



            var result = encoder.EncodeAsset(new EncodeConfig()
            {
                StartTime = queryStrings.Single(q => q.Key == "StartTime").Value,
                EndTime = queryStrings.Single(q => q.Key == "EndTime").Value,
                Source = queryStrings.Single(q => q.Key == "Source").Value,
                Title = queryStrings.Single(q => q.Key == "Title").Value
            });

            return result;

        }


        // POST: api/Encoder
        public void Post([FromBody]string value)
        {
        }


    }
}
