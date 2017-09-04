using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FMdotNet__DataAPI
{

    /// <exclude />
    internal class myJsonConverterStringString : JsonConverter
    {
        /// <exclude />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            List<KeyValuePair<string, string>> list = value as List<KeyValuePair<string, string>>;
            writer.WriteStartObject();
            foreach( var item in list)
            {
                writer.WritePropertyName(item.Key);
                writer.WriteValue(item.Value);
            }
            writer.WriteEndObject();
        }
        /// <exclude />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        /// <exclude />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<KeyValuePair<string, string>>);
        }
    }

    /// <exclude />
    public class myJsonConverterStringObject : JsonConverter
    {
        /// <exclude />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            List<KeyValuePair<string, object>> list = value as List<KeyValuePair<string, object>>;
            writer.WriteStartObject();
            foreach (var item in list)
            {
                writer.WritePropertyName(item.Key);
                var jsonValue = JsonConvert.SerializeObject(item.Value);
                writer.WriteValue(jsonValue);
                writer.WriteValue(item.Value);
            }
            writer.WriteEndObject();
        }
        /// <exclude />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        /// <exclude />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<KeyValuePair<string, object>>);
        }
    }
}
