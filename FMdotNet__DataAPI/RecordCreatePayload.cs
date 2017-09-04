using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FMdotNet__DataAPI
{
    // ==> may need to be renamed because it is also used for edit requests
	internal class RecordCreatePayload
	{

        private List<KeyValuePair<string, string>> fields { get; set; }

        // modification Id can be used if the user wants to edit a record only if it is still at the same mod id level
        private int modId { get; set; }
        private bool hasModId { get; set; }

		// constructor
		public RecordCreatePayload(List<KeyValuePair<string, string>> fieldKeyValuePairs)
		{
            fields = fieldKeyValuePairs;
            hasModId = false;
		}
        public RecordCreatePayload(List<KeyValuePair<string, string>> fieldKeyValuePairs, int modificationId)
        {
            fields = fieldKeyValuePairs;
            modId = modificationId;
            hasModId = true;
        }

		public string ToJSON()
		{
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var writer = new JsonTextWriter(sw);

            writer.WriteStartObject();
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            foreach (var item in fields)
            {
                writer.WritePropertyName(item.Key);
                writer.WriteValue(item.Value);
            }
            writer.WriteEndObject(); // for data
            if(hasModId == true)
            {
                writer.WritePropertyName("modId");
                writer.WriteValue(modId);
            }
            writer.WriteEndObject(); // for overall json
            writer.Close();

            return sw.ToString();
		}
	}
}
