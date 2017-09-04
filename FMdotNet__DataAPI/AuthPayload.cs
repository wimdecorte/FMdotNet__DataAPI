using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMdotNet__DataAPI
{
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

}
