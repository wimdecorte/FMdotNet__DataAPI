using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMdotNet__DataAPI
{
    /// <summary>
    /// Class AuthPayload. only applies to FMS16
    /// </summary>
    internal class AuthPayload
    {
        public string user { get; private set; }
        public string password { get; private set; }
        public string layout { get; private set; }

        public AuthPayload(string userAccountName, string userPassword, string layoutName)
        {
            user = userAccountName;
            password = userPassword;
            layout = layoutName;
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

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


    public class Response17
    {
        public Response response { get; set; }
        public Message[] messages { get; set; }
    }

    public class Response
    {
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


    public class ResponseData17
    {
        public ResponseData response { get; set; }
        public Message[] messages { get; set; }
    }

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



}
