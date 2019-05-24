using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using FMdotNet__DataAPI;

namespace XUnit_fmDotNet_DataAPI
{
    public class DeserializingTests
    {

        [Fact]
        public void DeserializeDataResponse()
        {
            JObject OJson = JObject.Parse(File.ReadAllText(@"../../../DataResponse_full.json"));
            string input = OJson.ToString(Formatting.None);

            var response = JsonConvert.DeserializeObject<Received>(input);

            Assert.True(response.Response.DataInfo.returnedCount == 100);
        }
    }
}
