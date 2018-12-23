using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using RestSharp;

namespace FMdotNet__DataAPI
{

    public partial class FMS18 : FMS
    {

        public List<FileMakerLayout> Layouts { get; private set; }
        public List<FileMakerFile> Files { get; private set; }

        public DataAPIinfo Productinfo { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes version 17 or higher
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="timeOut"></param>
        public FMS18(string dnsName, int httpsPort, string theAccount, string thePW, int timeOut) : base(dnsName, httpsPort, theAccount, thePW, timeOut, 18)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes the standard HTTPS port of 443 and version 18
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="timeOut"></param>
        public FMS18(string dnsName, string theAccount, string thePW, int timeOut) : base(dnsName, 443, theAccount, thePW, timeOut, 18)
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
        public FMS18(string dnsName, string theAccount, string thePW, int timeOut, int version) : base(dnsName, 443, theAccount, thePW, timeOut, version)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FMS"/> class.
        /// Assumes default port 443 and default timeout 100 secs or 100,000ms, and version 18
        /// </summary>
        /// <param name="dnsName"></param>
        /// <param name="theAccount"></param>
        /// <param name="thePW"></param>
        /// <param name="version"></param>
        public FMS18(string dnsName, string theAccount, string thePW) : base(dnsName, 443, theAccount, thePW, 100000, 18)
        {
        }

        public async Task<List<FileMakerFile>> GetFiles()
        {
            var url = BaseUrl + "databases";
            var files = new List<FileMakerFile>();

            try
            {
                HttpResponseMessage HttpResponse = null;

                
                // clear the default headers to get rid of the auth header
                webClient.DefaultRequestHeaders.Clear();

                // no auth needed
                /*
                // add the new header
                webClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                */
                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var r = JsonConvert.DeserializeObject<Received>(resultJson);
                    files = r.Response.FMfiles.ToList();
                    //
                    /*
                       {
                        "response": {
                            "databases": [
                      
                     * */

                }
            }
            catch (Exception ex)
            {
                // set last error
                this.lastErrorCode = 999;
                this.lastErrorMessage = ex.InnerException.ToString();
            }

            return files;
        }

        private async Task<List<FileMakerLayout>> GetLayoutDetails(string layoutName)
        {
            var url = BaseUrl + "databases/" + this.CurrentDatabase + "/layouts/" + layoutName;
            var details = new List<FileMakerLayout>();

            return details;
        }

        private async Task<List<FileMakerScript>> GetScript()
        {
            var url = BaseUrl + "databases/" + this.CurrentDatabase + "/scripts";
            var scripts = new List<FileMakerScript>();

            try
            {
                HttpResponseMessage HttpResponse = null;

                // empty json as the body as per the api documentation, probably works without a body too as per my Postman testing
                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var r = JsonConvert.DeserializeObject<Received>(resultJson);
                    scripts = r.Response.FMscripts.ToList();
                    //
                    /*
                        {
                            "response": {
                                "scripts": [
                                    {
                                        "name": "create_new_cake_and_fruit",
                                        "isFolder": false
                                    },
                     * */
                }
                else
                {
                    // register error
                }
            }
            catch (Exception ex)
            {

            }

            return scripts;
        }

        private async Task<List<FileMakerLayout>> GetLayoutsAsync()
        {
            var url = BaseUrl + "databases/" + this.CurrentDatabase + "/layouts";
            var layouts = new List<FileMakerLayout>();

            try
            {
                HttpResponseMessage HttpResponse = null;

                // empty json as the body as per the api documentation, probably works without a body too as per my Postman testing
                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var r = JsonConvert.DeserializeObject<Received>(resultJson);
                    layouts = r.Response.FMlayouts.ToList();
                    //
                    /*
                       {
                        "response": {
                            "databases": [
                      
                     * */
                }
                else
                {
                    // register error
                }
            }
            catch(Exception ex)
            {

            }

            return layouts;
        }

        public async Task<DataAPIinfo> GetProductInfo()
        {

            // this call will always work even if there is no valid token
            // clear the default headers to get rid of the auth header
            webClient.DefaultRequestHeaders.Clear();

            var url = BaseUrl + "productInfo";
            var info = new DataAPIinfo();

            try
            {
                HttpResponseMessage HttpResponse = null;


                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    // instead of doing this, it would be better to deserialize --> but not ready yet to deal with the json in response property
                    JObject o = JObject.Parse(resultJson);
                    string productJson = (string)o["response"];

                    // product info is a json object as the value of the response key
                    // body will also contain an error code :
                    /*
                    {
                        "response": {
                            "name": "FileMaker Data API Engine",
                            "buildDate": "11/14/2018",
                            "version": "18.0.1.69",
                            "dateFormat": "MM/dd/yyyy",
                            "timeFormat": "HH:mm:ss",
                            "timeStampFormat": "MM/dd/yyyy HH:mm:ss"
                        },
                        "messages": [
                    */
                    info = JsonConvert.DeserializeObject<DataAPIinfo>(productJson);
                    return info;

                }
            }
            catch(Exception ex)
            {
                this.lastErrorMessage = ex.InnerException.ToString();
                this.lastErrorCode = 999;
            }

            return info;
            
        }

        // not using the default FMS authenticate since FMS18 seems to work a bit differently
        public async Task<string> Authenticate()
        {
            token = "";
            string resultJson;
            string url = string.Empty;

            url = BaseUrl + "databases/" + CurrentDatabase + "/sessions";
            var byteArray = Encoding.ASCII.GetBytes(FMAccount + ":" + FMPassword);
            var sig = Convert.ToBase64String(byteArray);


            var client = new RestClient(url);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic " + sig);
            request.AddHeader("Content-Type", "application/json");
            

           

            try
            {
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // get the json from the response body
                    resultJson = response.Content;

                    Received received = JsonConvert.DeserializeObject<Received>(resultJson, new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });
                    if (received.messages[0].code == "0")
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
                    resultJson = response.Content;
                    Received received = JsonConvert.DeserializeObject<Received>(resultJson);
                    this.lastErrorCode = Convert.ToInt16(received.messages[0].code);
                }
            }
            catch (Exception ex)
            {
                //If it fails it could be because of the TLS issue
                token = "Error - " + ex.Message;
            }
            return token;
        }

    }
}
