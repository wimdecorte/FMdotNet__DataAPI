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

        public async Task<int> RunScript(string layout, string script, string param)
        {
            return await RunScript(this.CurrentDatabase, layout, script, param);
        }

        public async Task<int> RunScript(string script, string param)
        {
            return await RunScript(this.CurrentDatabase, this.CurrentLayout, script, param);
        }

        public async Task<int> RunScript(string script)
        {
            return await RunScript(this.CurrentDatabase, this.CurrentLayout, script, "");
        }



        public async Task<int> RunScript(string file, string layout, string script, string param)
        {
            ClearError();

            var url = BaseUrl + "databases/" + file + "/layouts/" + layout + "/script/" + script + "?script.param=" + param;
            SetAuthHeader();

            try
            {
                HttpResponseMessage HttpResponse = null;

                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();
                var r = JsonConvert.DeserializeObject<Received>(resultJson);

                // we don't really care what the response was, just capture the error
                SetFMSerror(r);
            }
            catch (Exception ex)
            {
                // set last error
                SetUnexpectedError999(ex);
            }

            return this.lastErrorCode;
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
                if (ex.InnerException != null) this.lastErrorMessage = ex.InnerException.ToString();
            }

            return files;
        }

        public async Task<FileMakerLayoutDetails> GetLayoutDetails(string layoutName)
        {
            ClearError();

            var url = BaseUrl + "databases/" + this.CurrentDatabase + "/layouts/" + layoutName;
            SetAuthHeader();

            var details = new FileMakerLayoutDetails();

            try
            {
                HttpResponseMessage HttpResponse = null;

                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();
                var r = JsonConvert.DeserializeObject<Received>(resultJson);

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    details.Fields = r.Response.FMfields;
                    details.Portals = r.Response.Portals;
                    details.ValueLists = r.Response.ValueLists;
                }
                else
                {
                    SetFMSerror(r);
                }
            }
            catch (Exception ex)
            {
                // set last error
                SetUnexpectedError999(ex);
            }

            return details;
        }

        public async Task<List<FileMakerScript>> GetScripts()
        {
            ClearError();

            var url = BaseUrl + "databases/" + this.CurrentDatabase + "/scripts";
            var scripts = new List<FileMakerScript>();

            try
            {
                HttpResponseMessage HttpResponse = null;
                SetAuthHeader();

                // empty json as the body as per the api documentation, probably works without a body too as per my Postman testing
                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();
                var r = JsonConvert.DeserializeObject<Received>(resultJson);

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
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
                    SetFMSerror(r);
                }
            }
            catch (Exception ex)
            {
                SetUnexpectedError999(ex);
            }

            return scripts;
        }

        public async Task<List<FileMakerLayout>> GetLayouts()
        {
            ClearError();

            var url = BaseUrl + "databases/" + this.CurrentDatabase + "/layouts";
            var layouts = new List<FileMakerLayout>();

            try
            {
                HttpResponseMessage HttpResponse = null;

                SetAuthHeader();

                HttpResponse = await webClient.GetAsync(url);
                var resultJson = await HttpResponse.Content.ReadAsStringAsync();
                var r = JsonConvert.DeserializeObject<Received>(resultJson);

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    layouts = r.Response.FMlayouts.ToList();
                    //
                    /*
                    {
                        "response": {
                            "layouts": [
                                {
                                    "name": "EMPTY"
                                },
                     * */
                }
                else
                {
                    // register error
                    SetFMSerror(r);
                }
            }
            catch (Exception ex)
            {
                SetUnexpectedError999(ex);
            }

            return layouts;
        }

        private void SetUnexpectedError999(Exception ex)
        {
            if (ex.InnerException != null) this.lastErrorMessage = ex.InnerException.ToString();
            this.lastErrorCode = 999;
        }

        private void SetFMSerror(Received r)
        {
            this.lastErrorMessage = r.messages[0].message;
            this.lastErrorCode = Convert.ToInt32(r.messages[0].code);
        }

        private void SetAuthHeader()
        {
            webClient.DefaultRequestHeaders.Clear();
            webClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        private void ClearError()
        {
            this.lastErrorMessage = string.Empty;
            this.lastErrorCode = 0;
        }

        public async Task<DataAPIinfo> GetProductInfo()
        {
            ClearError();

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
                JObject jsonObject = JObject.Parse(resultJson);

                if (HttpResponse.StatusCode == HttpStatusCode.OK)
                {
                    // instead of doing this, it would be better to deserialize --> but not ready yet to deal with the json in response property
                    var productJson = jsonObject["response"];

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
                    info = productJson.ToObject<DataAPIinfo>();
                    //info = JsonConvert.DeserializeObject<DataAPIinfo>(productJson);
                    return info;

                }
                else
                {
                    SetFMSerror(jsonObject.ToObject<Received>());
                }
            }
            catch(Exception ex)
            {
                SetUnexpectedError999(ex);
            }

            return info;
            
        }

        // not using the default FMS authenticate since FMS18 seems to work a bit differently
        public async Task<string> Authenticate()
        {
            ClearError();

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
                    SetFMSerror(received);
                }
            }
            catch (Exception ex)
            {
                //If it fails it could be because of the TLS issue
                token = "Error - " + ex.Message;
                SetUnexpectedError999(ex);
            }
            return token;
        }

    }
}
