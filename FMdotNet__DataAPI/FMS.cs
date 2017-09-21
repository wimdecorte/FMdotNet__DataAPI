using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace FMdotNet__DataAPI
{
    /// <summary>
    /// All of fmDotNet.
    /// This version makes use of the new Data API introduced in FileMaker Server 16.
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
        /// Use this if you are using a non-standard HTTPS port in your FMS config.
        /// </summary>
		public int Port { get; private set; }                 // 443 for HTTPS or custom HTTPS port

        /// <summary>
        /// Milliseconds used in HttpWebRequest to set the timeout
        /// </summary>
        public int ResponseTimeout { get; private set; }

        // using one HttpClient saves the actual client from having to spawn one for each call
        internal static HttpClient webClient { get; private set; }

        /// <summary>
        /// The name of the FileMaker file to target.  Set by the SetFile method.
        /// </summary>
		public string CurrentDatabase { get; private set; }
        
        /// <summary>
        /// Represents the currently selected layout, set by SetLayout method.
        /// </summary>
		public string CurrentLayout { get; private set; }

        /// <summary>
        /// The shared URL for all the of the requests.
        /// </summary>
		internal string BaseUrl { get; private set; }

        /// <summary>
        /// The FileMaker account name to use in the authentication request
        /// </summary>
		public string FMAccount { get; private set; }

        /// <summary>
        /// The FileMaker password to use in the authentication request
        /// </summary>
		internal string FMPassword { get; private set; }

        //public int TotalRecords { get; private set; } // not sure we can get this except by all records

        /// <summary>
        /// The token that FileMaker Server generates after a successful authentication call.
        /// It is valid for 15 minutes after the last call to the API.
        /// </summary>
        internal string token { get; private set; }

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

        #region "Constructor Methods"
        /*
         * To Do:
         * - add oAuth as an option, see other params for that --> requires a login page and redirect to FMS
         * - replace var Client with webclient, most of them done but check
         * - use serialization on the FMdata classes DataContract/DataMember vs Serializable
         * - beef up error handling, for instance when passing empty field values   
         * - refactor for camelcase on variables?
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
        /// <remarks>The FMS Data API only works over HTTPS. Make sure the client supports TLS 1.2.  If it does not add something like this line to your code: System.Net.ServicePointManager.SecurityProtcol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;</remarks>
        public FMS(string dnsName, int httpsPort, string theAccount, string thePW, int timeOut)
        {
            ServerAddress = dnsName;
            Port = httpsPort;
            ResponseTimeout = timeOut;
            FMAccount = theAccount;
            FMPassword = thePW;

            webClient = new HttpClient();

            BaseUrl = "https://" + ServerAddress + ":" + Port + "/fmi/rest/api/";

            // ideally I'd do this below but seems to be not supported in Portable, only in platform specific code
            // System.Net.ServicePointManager.SecurityProtcol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes the standard HTTPS port of 443
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="timeOut"></param>
        public FMS(string dnsName, string theAccount, string thePW, int timeOut) : this(dnsName, 443, theAccount, thePW, timeOut)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes default port 443 and default timeout 100 secs or 100,000ms
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        public FMS(string dnsName, string theAccount, string thePW) : this(dnsName, 443, theAccount, thePW, 100000)
        {
        }

        #endregion

        #region "public methods"

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
        /// Gets a token based on the provided info.  Make sure to set the target file and target layout first.
        /// </summary>
        public async System.Threading.Tasks.Task<string> Authenticate()
        {
            token = "";
            string resultJson;
            string url = BaseUrl + "auth/" + CurrentDatabase;

            // construct the json
            AuthPayload payload = new AuthPayload(FMAccount, FMPassword, CurrentLayout);
            string payloadJson = payload.ToJSON();

            // var client = new HttpClient();
            // var client = webClient;
            try
            {
                var response = await webClient.PostAsync(url, new StringContent(payloadJson, Encoding.UTF8, "application/json"));
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    resultJson = await response.Content.ReadAsStringAsync();
                    AuthResponse received = JsonConvert.DeserializeObject<AuthResponse>(resultJson);
                    token = received.token;
                    // add the token here once, don't have to do it again then
                    webClient.DefaultRequestHeaders.Add("FM-Data-token", token);
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
        public async System.Threading.Tasks.Task<int> Logout()
        {
            int errorCode;
            string url = BaseUrl + "auth/" + CurrentDatabase;
            string resultJson;

            //var client = new HttpClient();
            var client = webClient;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("FM-Data-token", token);

            var response = await client.DeleteAsync(url);
            resultJson = await response.Content.ReadAsStringAsync();
            ErrorCodeOnlyResponse received = JsonConvert.DeserializeObject<ErrorCodeOnlyResponse>(resultJson);

            errorCode = Convert.ToInt32(received.errorCode);

            // clear the token regardless of the logout outcome, that will force a new login
            token = "";

            return errorCode;
        }

        /// <summary>
        /// Creates a new empty record in the specified file, in the table associates with the specified layout (context).
        /// </summary>
        /// <returns>A <see cref="RecordManipulationResponse"></see> response</returns>
        public async System.Threading.Tasks.Task<RecordManipulationResponse> CreateEmptyRecord()
        {
            var request = new EmptyRecordCreateRequest(this);
            var response = await request.Execute();
            return response;
        }

        /// <summary>
        /// Method to call to create a new record request and specify options for it later.
        /// </summary>
        /// <returns> <see cref="RecordCreateRequest"/></returns>
        public RecordCreateRequest NewRecordRequest()
        {
            createRequest = new RecordCreateRequest(this);
            return createRequest;
        }


        /// <summary>
        /// Method to call to create a record modification request and specify options for it later.
        /// </summary>
        /// <param name="recId">The internal FileMaker record id for the record to edit.</param>
        /// <returns><see cref="RecordEditRequest"/></returns>
        public RecordEditRequest EditRequest(int recId)
        {
            editRequest = new RecordEditRequest(this,recId);
            return editRequest;
        }

        /// <summary>
        /// Method to call to create a record modification request and specify options for it later.
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
        /// Method to call to create a find request and specify options for it later.
        /// </summary>
        /// <returns><see cref="RecordFindRequest"/></returns>
        public RecordFindRequest FindRequest()
        {
            findRequest = new RecordFindRequest(this);
            return findRequest;
        }

        /// <summary>
        /// Method to call to create a find request and specify options for it later.
        /// </summary>
        /// <param name="recId">The internal FileMaker record id for the record to find.</param>
        /// <returns></returns>
        public RecordFindRequest FindRequest(int recId)
        {
            findRequest = new RecordFindRequest(this, recId);
            return findRequest;
        }

        /// <summary>
        /// Method to call to create a new records and specify options for it later.
        /// </summary>
        /// <returns></returns>
        public RecordCreateRequest CreateRequest()
        {
            createRequest = new RecordCreateRequest(this);
            return createRequest;
        }


        // add the rest here
        

        #endregion



    }
}
