using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FMdotNet__DataAPI
{
    /// <summary>
    /// All of fmDotNet.
    /// This version makes use of the official Data API released in FileMaker Server 17.
    /// As such it only supports HTTPS.
    /// Remember to force TLS 1.2 in your client if necessary. See the example console application for a way to do that.
    /// </summary>
    public partial class FMS
    {
        /// <summary>
        /// DNS name of the FileMaker Server. Remember that only HTTPS is supported so using the IP address
        /// will generate a security warning.
        /// </summary>
        public string ServerAddress { get; private set; }

        /// <summary>
        /// The version of FMS - Data API is different in 17 than in 18 for instance.
        /// </summary>
        /// <value>The version.</value>
        public int Version { get; private set; }

        /// <summary>
        /// Use this if you are using a non-standard HTTPS port in your FMS config.
        /// </summary>
		public int Port { get; private set; }                 // 443 for HTTPS or custom HTTPS port

        /// <summary>
        /// Milliseconds used in HttpWebRequest to set the timeout
        /// </summary>
        public int ResponseTimeout { get; private set; }

        // using one HttpClient saves the actual client from having to spawn one for each call
        internal static HttpClient webClient { get; private set; }

        internal static RestSharp.RestClient restClient { get; private set; }

        /// <summary>
        /// The name of the FileMaker file to target.  Set by the SetFile method.
        /// </summary>
		public string CurrentDatabase { get; private set; }
        
        /// <summary>
        /// Represents the currently selected layout, set by SetLayout method.
        /// </summary>
		public string CurrentLayout { get; protected set; }

        /// <summary>
        /// The shared URL for all the of the requests.
        /// </summary>
		internal string BaseUrl { get; private set; }

        /// <summary>
        /// The FileMaker account name to use in the authentication request
        /// </summary>
		public string FMAccount { get; protected set; }

        /// <summary>
        /// The FileMaker password to use in the authentication request
        /// </summary>
		internal string FMPassword { get; private set; }

        /// <summary>
        /// The folder where fmdotNet will save container data to.
        /// </summary>
        public string DownLoadFolder { get; protected set; }

        //public int TotalRecords { get; private set; } // not sure we can get this except by all records

        /// <summary>
        /// The token that FileMaker Server generates after a successful authentication call.
        /// It is valid for 15 minutes after the last call to the API.
        /// </summary>
        protected string token { get; set; }

        /// <summary>
        /// Class for all requests that create new records
        /// </summary>
        internal RecordCreateRequest createRequest { get; private set; }

        /// <summary>
        /// Class for all requests that modify existing records
        /// </summary>
        internal RecordEditRequest editRequest { get; private set; }

        /// <summary>
        /// Class for all requests that finds records
        /// </summary>
        internal RecordFindRequest findRequest { get; private set; }

        /// <summary>
        /// The FileMaker error code from the last call made.
        /// </summary>
        public int lastErrorCode { get; protected set; }

        /// <summary>
        /// The FileMaker error code from the last script executed after the Data API call.
        /// </summary>
        public int lastErrorCodeScript { get; protected set; }

        /// <summary>
        /// The FileMaker error code from the last script executed after the Data API call but before the sort.
        /// </summary>
        public int lastErrorCodeScriptPreSort { get; protected set; }

        /// <summary>
        /// The FileMaker error code from the last script executed before the Data API call.
        /// </summary>
        public int lastErrorCodeScriptPreRequest { get; protected set; }

        /// <summary>
        /// The FileMaker error message associated with the last error.
        /// </summary>
        public string lastErrorMessage { get; protected set; }




        #region "Constructor Methods"
        /*
         * To Do:
         * - add oAuth as an option, see other params for that --> requires a login page and redirect to FMS
         * - replace var Client with webclient, most of them done but check
         * - use serialization on the FMdata classes DataContract/DataMember vs Serializable
         * - beef up error handling, for instance when passing empty field values   
         * - refactor for camelcase on variables?
         * 
         * - add modId as a parameter where possible
         * - add the 18-specific things to the FMS18 class - or use a vX naming instead of FMS version numbering
         * - user deserialziation using the classes in Response.cs instead of reading through the json like in RecordGetResponse
         * */

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Default constructor, requires all the info.
        /// </summary>
        /// <param name="dnsName">The DNS name of the web server.</param>
        /// <param name="httpsPort">The port.</param>
        /// <param name="theAccount">The account.</param>
        /// <param name="thePW">The password.</param>
        /// <param name="timeOut">Milliseconds to wait for FMSA's response. (Default is 100,000 or 100 seconds).</param>
        /// <param name="version">The version number of your FileMaker Server: 18 or 17</param>
        /// <remarks>The FMS Data API only works over HTTPS. Make sure the client supports TLS 1.2.  If it does not add something like this line to your code: System.Net.ServicePointManager.SecurityProtcol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;</remarks>
        public FMS(string dnsName, int httpsPort, string theAccount, string thePW, int timeOut, int version)
        {
            ServerAddress = dnsName;
            Port = httpsPort;
            ResponseTimeout = timeOut;
            FMAccount = theAccount;
            FMPassword = thePW;
            Version = version;

            webClient = new HttpClient();
            restClient = new RestSharp.RestClient();

            if( Version == 17)
            {
                BaseUrl = "https://" + ServerAddress + ":" + Port + "/fmi/data/v1/";
            }
            else if(version > 17)
            {
                BaseUrl = "https://" + ServerAddress + ":" + Port + "/fmi/data/vLatest/";
            }
            restClient.BaseUrl = new System.Uri(BaseUrl);

            // ideally I'd do this below but seems to be not supported in Portable, only in platform specific code
            // System.Net.ServicePointManager.SecurityProtcol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            // so this needs to be done in whatever uses the DLL
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes the standard HTTPS port of 443
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="timeOut"></param>
        /// <param name="version"></param>
        public FMS(string dnsName, string theAccount, string thePW, int timeOut, int version) : this(dnsName, 443, theAccount, thePW, timeOut, version)
        { }



        #endregion

        #region "public methods"

        // because HttpClient does not have a PATCH async method
        /// <summary>
        /// patch as an asynchronous operation.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="iContent">The HttpContent to send.</param>
        /// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
        internal async Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUrl, HttpContent iContent)
        {
            var method = new HttpMethod("PATCH");

            Uri requestUri = new Uri(requestUrl);
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            HttpResponseMessage response = new HttpResponseMessage();
            // In case you want to set a timeout
            //CancellationToken cancellationToken = new CancellationTokenSource(60).Token;


            response = await client.SendAsync(request);
            // If you want to use the timeout you set
            //response = await client.SendRequestAsync(request).AsTask(cancellationToken);


            return response;
        }

        /// <summary>
        /// Sets the target FileMaker file.
        /// </summary>
        /// <param name="fileName">The FM file name.</param>
        /// <remarks>FMS Data API does not have a way to check if the file is available</remarks>
        public void SetFile(string fileName)
        {
            CurrentDatabase = fileName;
        }

        /// <summary>
        /// Sets the target layout, the layout determines the database/table context
        /// </summary>
        /// <param name="theLayout">The layout name.</param>
        /// <remarks>FMS Data API does not have a way to check if the layout is available</remarks>
        /// <remarks>This may be case sensitive!</remarks>
        public void SetLayout(String theLayout)
        {
            CurrentLayout = Uri.EscapeUriString(theLayout);
        }

        /// <summary>
        /// Sets the folder where container data will be downloaded to.
        /// </summary>
        /// <param name="folder">The folder.</param>
        public void SetDownloadFolder(string folder)
        {
            DownLoadFolder = folder;
        }

        /// <summary>
        /// Gets a token based on the provided info.  Make sure to set the target file first.
        /// </summary>
        public async Task<string> Authenticate()
        {
            token = "";
            string resultJson;
            string url = string.Empty;
            
            url = BaseUrl + "databases/" + CurrentDatabase + "/sessions";
            var byteArray = Encoding.ASCII.GetBytes(FMAccount + ":" + FMPassword);
            var sig = Convert.ToBase64String(byteArray);
            webClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", sig);

            var content = new StringContent("{}", Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = null;

                // empty json as the body as per the api documentation, probably works without a body too as per my Postman testing

                // works in 17 but not 18
                response = await webClient.PostAsync(url, content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // get the json from the response body
                    resultJson = await response.Content.ReadAsStringAsync();

                    // token is in the response body
                    // body will also contain an error code :
                    /*
                        {
                            "response": {
                                "token": "3eaed56b71f0ba0c6ca8d2984e7a681e3bbada56d48b1b21ab4"
                            },
                            "messages": [
                                {
                                    "code": "0",
                                    "message": "OK"
                                }
                            ]
                        }
                    */
                    Received received = JsonConvert.DeserializeObject<Received>(resultJson);
                    if(received.messages[0].code == "0")
                    {
                        token = received.Response.Token;

                        // clear the default headers to get rid of the auth header since we'll be re-using webclient
                        webClient.DefaultRequestHeaders.Clear();

                        // add the new header
                        webClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                }
                else
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                    Received received = JsonConvert.DeserializeObject<Received>(resultJson);
                    this.lastErrorCode = Convert.ToInt16(received.messages[0].code);
                }
            }
            catch(Exception ex)
            {
                //If it fails it could be because of the TLS issue
                token = "Error - " + ex.Message;
            }
            return token;
        }

        /// <summary>
        /// Logs out of the session and invalidates the token.
        /// </summary>
        /// <returns>error code, 0 if no error.</returns>
        public async Task<int> Logout()
        {
            int errorCode = 0;
            string url = string.Empty;

            url = BaseUrl + "databases/" + CurrentDatabase + "/sessions/" + token;

            string resultJson;

            //var client = new HttpClient();
            var client = webClient;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync(url);
            resultJson = await response.Content.ReadAsStringAsync();

            Received received = JsonConvert.DeserializeObject<Received>(resultJson);
            errorCode = Convert.ToInt32(received.messages[0].code);

            // clear the token regardless of the logout outcome, that will force a new login
            token = "";

            return errorCode;
        }


        /// <summary>
        /// Method to create a new record request and specify options for it later.
        /// </summary>
        /// <returns> <see cref="RecordCreateRequest"/></returns>
        public RecordCreateRequest NewRecordRequest()
        {
            createRequest = new RecordCreateRequest(this);
            return createRequest;
        }


        /// <summary>
        /// Method to call the record modification request and specify options for it later.
        /// </summary>
        /// <param name="recId">The internal FileMaker record id for the record to edit.</param>
        /// <returns><see cref="RecordEditRequest"/></returns>
        public RecordEditRequest EditRequest(int recId)
        {
            editRequest = new RecordEditRequest(this,recId);
            return editRequest;
        }

        /// <summary>
        /// Method to call a record modification request and specify options for it later.
        /// </summary>
        /// <param name="recId">The internal FileMaker record id for the record to edit.</param>
        /// <param name="modId">The internal FileMaker modification id for the record to edit. If the record has a different modification Id than specified, an error will be returned.</param>
        /// <returns><see cref="RecordEditRequest"/></returns>
        public RecordEditRequest EditRequest(int recId, int modId)
        {
            editRequest = new RecordEditRequest(this, recId, modId);
            return editRequest;
        }


        /// <summary>
        /// Method to call a find request and specify options for it later.
        /// </summary>
        /// <returns><see cref="RecordFindRequest"/></returns>
        public RecordFindRequest FindRequest()
        {
            findRequest = new RecordFindRequest(this);
            return findRequest;
        }

        /// <summary>
        /// Method to call a find request and specify options for it later.
        /// </summary>
        /// <param name="recId">The internal FileMaker record id for the record to find.</param>
        /// <returns></returns>
        public RecordFindRequest FindRequest(int recId)
        {
            findRequest = new RecordFindRequest(this, recId);
            return findRequest;
        }

        /// <summary>
        /// Method to call a find request and specify options for it later.
        /// </summary>
        /// <param name="recId">The internal FileMaker record id for the record to find.</param>
        /// <param name="responseLayout">The FileMaker layout to use to return the data (only fields on this layout will be returned).</param>
        /// <returns></returns>
        public RecordFindRequest FindRequest(int recId, string responseLayout)
        {
            findRequest = new RecordFindRequest(this, recId, responseLayout);
            return findRequest;
        }

        /// <summary>
        /// Method to create a new records and specify options for it later.
        /// </summary>
        /// <returns></returns>
        public RecordCreateRequest CreateRequest()
        {
            createRequest = new RecordCreateRequest(this);
            return createRequest;
        }

        // add the rest here


        #endregion

        /// <summary>
        /// Sets the last error properties.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message.</param>
        /// <param name="response">The response that contains the json sent by FMS.</param>
        private void SetLastError(int code, string message, Response response)
        {
            lastErrorCode = code;
            lastErrorMessage = message;
            lastErrorCodeScript = Convert.ToInt16(response.ScriptError);
            lastErrorCodeScriptPreRequest = Convert.ToInt16(response.ScriptErrorPreRequest);
            lastErrorCodeScriptPreSort = Convert.ToInt16(response.ScriptErrorPreSort);
        }

        /// <summary>
        /// Sets the last error properties.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message.</param>
        private void SetLastError(int code, string message)
        {
            lastErrorCode = code;
            lastErrorMessage = message;
            lastErrorCodeScript = 0;
            lastErrorCodeScriptPreRequest = 0;
            lastErrorCodeScriptPreSort = 0;
        }

        /// <summary>
        /// Modify the request to add teh calls to the FM scripts.
        /// </summary>
        /// <param name="requestToModify">The request to modify.</param>
        /// <param name="scripts">The List of FMscript objects to add.</param>
        /// <returns>RestSharp.RestRequest.</returns>
        private RestSharp.RestRequest BuildScriptsURLpart(RestSharp.RestRequest requestToModify, List<FMSscript> scripts)
        {
            if (scripts != null && scripts.Count >= 1)
            {
                foreach (FMSscript s in scripts)
                {
                    if (s.type == ScriptTypes.after)
                    {
                        requestToModify.AddParameter("script", s.name, ParameterType.QueryString);
                        if (s.parameter != null)
                        {
                            requestToModify.AddParameter("script.param", s.parameter, ParameterType.QueryString);
                        }
                    }
                    else if (s.type == ScriptTypes.before)
                    {
                        requestToModify.AddParameter("script,prerequest", s.name, ParameterType.QueryString);
                        if (s.parameter != null)
                        {
                            requestToModify.AddParameter("script.prerequest.param", s.parameter, ParameterType.QueryString);
                        }
                    }
                    else if (s.type == ScriptTypes.beforeSort)
                    {
                        requestToModify.AddParameter("script,presort", s.name, ParameterType.QueryString);
                        if (s.parameter != null)
                        {
                            requestToModify.AddParameter("script.presort.param", s.parameter, ParameterType.QueryString);
                        }
                    }
                }
            }
            return requestToModify;
        }

        /// <summary>
        /// Builds out the URL with the FM scripts to call.
        /// </summary>
        /// <param name="scripts">The list of FMscript objects to add.</param>
        /// <returns>modified URL</returns>
        private string buildScriptsURLpart(List<FMSscript> scripts)
        {
            // need to have:
            // ?script=log&script.param=runs after delete&script.prerequest=log&script.prerequest.param=runs before the delete&script.presort=log&script.presort.param=runs after delete but before sort
            string urlPart = string.Empty;

            if (scripts != null && scripts.Count >= 1)
            {
                List<string> parts = new List<string>();

                foreach (FMSscript s in scripts)
                {
                    if (s.type == ScriptTypes.after)
                    {
                        parts.Add("script=" + s.name);
                        if (s.parameter != null)
                        {
                            parts.Add("script.param=" + s.parameter);
                        }
                    }
                    else if (s.type == ScriptTypes.before)
                    {
                        parts.Add("script.prerequest=" + s.name);
                        if (s.parameter != null)
                        {
                            parts.Add("script.prerequest.param=" + s.parameter);
                        }
                    }
                    else if (s.type == ScriptTypes.beforeSort)
                    {
                        parts.Add("script.presort=" + s.name);
                        if (s.parameter != null)
                        {
                            parts.Add("script.presort.param=" + s.parameter);
                        }
                    }
                }

                // combine the parts into one string with the & delimiter
                urlPart = "?" + String.Join("&", parts);
            }

            return urlPart;

        }

    }

    /// <summary>
    /// Represents a FileMaker Server 17 deployment.
    /// </summary>
    /// <seealso cref="FMdotNet__DataAPI.FMS" />
    public partial class FMS17 : FMS
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes version 17 or higher
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="timeOut"></param>
        public FMS17(string dnsName, int httpsPort, string theAccount, string thePW, int timeOut) : base(dnsName, httpsPort, theAccount, thePW, timeOut, 17)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes the standard HTTPS port of 443 and version 17
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="timeOut"></param>
        public FMS17(string dnsName, string theAccount, string thePW, int timeOut) : base(dnsName, 443, theAccount, thePW, timeOut, 17)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes the standard HTTPS port of 443
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="timeOut"></param>
        /// <param name="version"></param>
        public FMS17(string dnsName, string theAccount, string thePW, int timeOut, int version) : base(dnsName, 443, theAccount, thePW, timeOut, version)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes default port 443 and default timeout 100 secs or 100,000ms, and version 17
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="version"></param>
        public FMS17(string dnsName, string theAccount, string thePW) : base(dnsName, 443, theAccount, thePW, 100000, 17)
        {
        }

    }


    /// <summary>
    /// Represents a FileMaker script to execute.
    /// </summary>
    public partial class FMSscript
    {
        /// <summary>
        /// The FileMaker script name.
        /// </summary>
        /// <value>The name.</value>
        public string name { get; private set; }
        /// <summary>
        /// The parameter to pass to the script.
        /// </summary>
        /// <value>The parameter.</value>
        public string parameter { get; private set; }
        /// <summary>
        /// The script type, determines when the script will be executed.
        /// </summary>
        /// <value>The type.</value>
        public ScriptTypes type { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSscript"/> class.
        /// </summary>
        /// <param name="scriptType">Type of the script.</param>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="scriptParameter">The script parameter.</param>
        public FMSscript( ScriptTypes scriptType, string scriptName, string scriptParameter) : this(scriptType, scriptName)
        {
            parameter = scriptParameter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMSscript"/> class, passes no parameter to the script.
        /// </summary>
        /// <param name="scriptType">Type of the script.</param>
        /// <param name="scriptName">Name of the script.</param>
        public FMSscript(ScriptTypes scriptType, string scriptName)
        {
            type = scriptType;
            name = scriptName;
        }
    }

    /// <summary>
    /// The three script types, differentiated by when they are executed
    /// </summary>
    public enum ScriptTypes
    {
        after,
        before,
        beforeSort
    }



}
