using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMdotNet__DataAPI
{
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

    internal class AuthResponse
    {
        public string errorCode { get; set; }
        public string layout { get; set; }
        public string token { get; set; }
    }

    internal class ErrorCodeOnlyResponse
    {
        public string errorCode { get; set; }
    }

    /// <summary>
    /// Represents the json received from FMS
    /// </summary>
    public class Received
    {
        public Response response { get; set; }
        public Message[] messages { get; set; }
    }

    public class Response
    {
        public DataAPIinfo productinfo { get; set; }

        [JsonProperty("databases")]
        public FileMakerFile[] FMfiles { get; set; }

        [JsonProperty("layouts")]
        public FileMakerLayout[] FMlayouts { get; set; }

        [JsonProperty("scripts")]
        public FileMakerScript[] FMscripts { get; set; }


        [JsonProperty("fieldMetaData")]
        public FieldMetaData[] FMfields { get; set; }

        public DataSource dataSource { get; set; }

        public ResultSet resultSet { get; set; }

        public string recordId { get; set; }
        public string modId { get; set; }

        public string token { get; set; }

        public string scriptError { get; set; }

        [JsonProperty("scriptError.presort")]
        public string scriptErrorPreSort { get; set; }

        [JsonProperty("scriptError.prerequest")]
        public string scriptErrorPreRequest { get; set; }
    }

    public class Message
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    // -------


    public class ResponseData
    {
        public RecordData[] data { get; set; }
    }

    public class RecordData
    {
        public Fielddata fieldData { get; set; }
        public Portaldata portalData { get; set; }
        public string recordId { get; set; }
        public string modId { get; set; }
    }

    public class Fielddata
    {
        public string cake { get; set; }
        public string wine_pairing { get; set; }
        public string country { get; set; }
        public int record_id { get; set; }
        public string cake_FRUIT__number__acfruit { get; set; }
        public string cake_FRUIT__number__acrecord_id { get; set; }
    }

    public class Portaldata
    {
        public Cake_FRUIT__Ac[] cake_FRUIT__ac { get; set; }
        public Same_Country[] same_country { get; set; }
    }

    public class Cake_FRUIT__Ac
    {
        public int recordId { get; set; }
        public string cake_FRUIT__acfruit { get; set; }
        public string cake_FRUIT__accountry { get; set; }
        public int cake_FRUIT__acrecord_id { get; set; }
        public string cake_FRUIT__acnumber_field { get; set; }
        public string modId { get; set; }
    }

    public class Same_Country
    {
        public int recordId { get; set; }
        public string cake_CAKE__sameCountrycake { get; set; }
        public int cake_CAKE__sameCountryrecord_id { get; set; }
        public string modId { get; set; }
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


}
