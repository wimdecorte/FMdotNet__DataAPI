using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace FMdotNet__DataAPI
{
    /// <summary>
    /// as of yet unused - 20181221
    /// </summary>
    internal class oAuthPayload
    {
        public string layout { get; private set; }
        public string oAuthRequestId { get; private set; }
        public string oAuthIdentifier { get; private set; }

        public oAuthPayload(string requestId, string identifier, string layoutName)
        {
            oAuthIdentifier = identifier;
            oAuthRequestId = requestId;
            layout = layoutName;
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ReceivedMessages
    {
        public Message[] messages { get; set; }
    }

    public class ReceivedToken : ReceivedMessages
    {
        [JsonProperty("response")]
        public Response ResponseToken { get; set; }

    }

    /// <summary>
    /// Represents the json received from FMS
    /// </summary>
    public class Received : ReceivedMessages
    {
        /*
        // needs a constructor to deal with the unknown json object that may be teh value of the response property/key
        public Received(JObject response)
        {

        }
        */

            /// <summary>
            /// Represents the response node in the json received from FMS
            /// </summary>
        [JsonProperty("response")]
        public Response Response { get; set; }
       
    }

    /// <summary>
    /// Represents the response key of the json received from FMS,  holds a great many things depending on the type of the call made
    /// </summary>
    public class Response
    {
 
        public DataAPIinfo Productinfo { get; set; }

        [JsonProperty("databases")]
        public FileMakerFile[] FMfiles { get; set; }

        [JsonProperty("layouts")]
        public FileMakerLayout[] FMlayouts { get; set; }

        [JsonProperty("scripts")]
        public FileMakerScript[] FMscripts { get; set; }

        #region properties used in the layout details
        [JsonProperty("fieldMetaData")]
        public FieldMetaData[] FMfields { get; set; }

        [JsonProperty("portalMetaData")]
        public Dictionary<string, FieldMetaData[]> Portals { get; set; }

        
        [JsonProperty("valueLists")]
        public FileMakerValueList[] ValueLists { get; set; }

        #endregion

        [JsonProperty("dataSource")]
        public DataSource DataSource { get; set; }

        [JsonProperty("resultSet")]
        public ResultSet ResultSet { get; set; }

        [JsonProperty("recordId")]
        public string RecordId { get; set; }

        [JsonProperty("modId")]
        public string ModId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("scriptError")]
        public string ScriptError { get; set; }

        [JsonProperty("scriptError.presort")]
        public string ScriptErrorPreSort { get; set; }

        [JsonProperty("scriptError.prerequest")]
        public string ScriptErrorPreRequest { get; set; }

        [JsonProperty("data")]
        public ResponseData Data { get; set; }
    }

     /// <summary>
    /// The FMS json holds an array of messages like this - usually only one message through
    /// </summary>
    public class Message
    {
        public string code { get; set; }
        public string message { get; set; }
    }

 
    public class ResponseData
    {
        public Record[] records { get; set; }
    }

    public class Record
    {
        public Record(JObject fieldData, JObject portalData)
        {
            // constructor to parse the field and portal data
            // the rest of the json data will get de-serialized by the matching json key (property) names

        }

        public List<Field> fields { get; private set; }
        public string recordId { get; set; }
        public string modId { get; set; }

        /// <summary>
        /// for each portal and related record, the info on the TO and table of the related data, found count and returned count
        /// </summary>
        public RelatedSet[] relatedSet { get; set; }
    }

    /// <summary>
    /// Returned by the ProductInfo call - metadata
    /// </summary>
    public class DataAPIinfo
    {
        public string name { get; set; }
        public string buildDate { get; set; }
        public string version { get; set; }
        public string dateFormat { get; set; }
        public string timeFormat { get; set; }
        public string timeStampFormat { get; set; }
    }

    public class FileMakerFile
    {
        public string name { get; set; }
    }

    public class FileMakerLayout
    {
        public string name { get; set; }
    }

    public class FileMakerLayoutDetails
    {
        public FieldMetaData[] Fields { get; set; }

        public Dictionary<string, FieldMetaData[]> Portals { get; set; }

        public FileMakerValueList[] ValueLists { get; set; }
    }

    public class FileMakerScript
    {
        public string name { get; set; }
        public bool isFolder { get; set; }
    }


    public class FieldMetaData
    {
        public string name { get; set; }
        public string type { get; set; }
        public string displayType { get; set; }
        public string result { get; set; }
        public bool global { get; set; }
        public bool autoEnter { get; set; }
        public bool fourDigitYear { get; set; }
        public int maxRepeat { get; set; }
        public int maxCharacters { get; set; }
        public bool notEmpty { get; set; }
        public bool numeric { get; set; }
        public bool timeOfDay { get; set; }
        public int repetitionStart { get; set; }
        public int repetitionEnd { get; set; }
    }


    public class FileMakerValueList
    {
        public string name { get; set; }
        public string type { get; set; }
        public VLValue[] values { get; set; }
    }

    public class VLValue
    {
        public string displayValue { get; set; }
        public string value { get; set; }
    }


    public class DataSource
    {
        public string database { get; set; }
        public string layout { get; set; }
        public string table { get; set; }
        public int totalRecordCount { get; set; }
    }


    public class ResultSet
    {
        public int foundCount { get; set; }
        public int returnedCount { get; set; }
    }


    public class RelatedSet
    {
        public string database { get; set; }
        public string table { get; set; }
        public int foundCount { get; set; }
        public int returnedCount { get; set; }
    }


}
