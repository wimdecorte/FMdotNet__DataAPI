using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace FMdotNet__DataAPI
{

    partial class FMS
    {

        /// <summary>
        /// Downloads the file from a container field.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>FileInfo object for the saved file.</returns>
        public async Task<FileInfo> DownloadFileFromContainerField(string url)
        {
            string fullName = "downloaded_from_FMS_" + Guid.NewGuid().ToString() + "_" + DateTime.Now.ToString("yyyy-M-dd-HH-mm-ss");
            FileInfo file = await DownloadFileFromContainerField(url, fullName);
            return file;

        }
        /// <summary>
        /// Downloads the file from a container field.
        /// </summary>
        /// <param name="url">The URL as returned from the GetRecord call.</param>
        /// <param name="fileNamePlusEtension">The file name and extension to use for the saved file.</param>
        /// <returns>FileInfo object for the saved file.</returns>
        public async Task<FileInfo> DownloadFileFromContainerField(string url, string fileNamePlusEtension)
        {
            FileInfo file;
            CookieContainer cookieContainer = new CookieContainer();

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.Headers.Add("Accept-Language", "en-US");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");

                request.CookieContainer = cookieContainer;

                using (WebResponse httpResponse = await request.GetResponseAsync())
                {
                    var contentType = httpResponse.Headers["Content-Type"];
                    // get the extension from the content type --> does not work, cannot determine file name or type from the download context
                    //string ext = MimeTypeMap.GetExtension(contentType);

                    string basePath = DownLoadFolder;
                    string fileName = Path.Combine(basePath, fileNamePlusEtension);

                    var responseStream = httpResponse.GetResponseStream();

                    using (var fileStream = File.Create(fileName))
                    {
                        responseStream.CopyTo(fileStream);
                    }
                    file = new FileInfo(fileName);
                    return file;
                }
            }
            catch (Exception ex)
            {

                //return ex.Message;
                file = null;
                return file;

            }
        }

        /// <summary>
        /// Uploads the file into the container field.
        /// </summary>
        /// <param name="recordId">The record identifier of the FileMaker record.</param>
        /// <param name="fieldName">Name of the container field.</param>
        /// <param name="fieldRepetition">The field repetition.</param>
        /// <param name="file">The file.</param>
        /// <returns>error code or 0</returns>
        public async Task<int> UploadFileIntoContainerField(int recordId, string fieldName, int fieldRepetition, FileInfo file)
        {
            int code;
            string url = string.Empty;
            url = BaseUrl + "databases/" + CurrentDatabase + "/layouts/" + CurrentLayout + "/records/" + recordId + "/containers/" + fieldName + "/" + fieldRepetition;

            string fileExtension = file.Extension;
            string fileMimeType = GetMimeType(fileExtension); //=> image/jpeg
            string fileName = file.Name;

            MultipartFormDataContent content = new MultipartFormDataContent();
            byte[] filedata = File.ReadAllBytes(file.FullName);
            ByteArrayContent baContent = new ByteArrayContent(filedata);
            content.Add(baContent, "upload", fileName);

            var apiResponse = await webClient.PostAsync(url, content);
            string resultJson = await apiResponse.Content.ReadAsStringAsync();

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var received = JsonConvert.DeserializeObject<Response17>(resultJson);
                code = Convert.ToInt32(received.messages[0].code);
                SetLastError(code, received.messages[0].message, received.response);
            }
            else
            {
                code = (int)apiResponse.StatusCode;
                SetLastError(code, apiResponse.ReasonPhrase);
            }
            return code;
        }

        /// <summary>
        /// Uploads the file into the container field. Assumes first repetition.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="file">The file.</param>
        /// <returns>error code or 0.</returns>
        public async Task<int> UploadFileIntoContainerField(int recordId, string fieldName, FileInfo file)
        {
            int code = await UploadFileIntoContainerField(recordId, fieldName, 1, file);
            return code;
        }

        /// <summary>
        /// Deletes the specified record.
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <returns>error code or 0.</returns>
        public async Task<int> DeleteRecord(int recordId)
        {
            int code = await DeleteRecord(recordId, null);
            return code;
        }

        /// <summary>
        /// Deletes a specified record and runs the specified scripts.
        /// </summary>
        /// <param name="recordId">The internal FileMaker record Id.</param>
        /// <param name="scripts">The list of scripts to execute.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object. </returns
        public async Task<int> DeleteRecord(int recordId, List<FMSscript> scripts)
        {
            string url = string.Empty;
            string endpoint = string.Empty;
            if (Version == 16)
            {
                endpoint = "/record/" + CurrentDatabase + "/" + CurrentLayout + "/" + recordId;
                url = BaseUrl + endpoint;
            }
            else if (Version > 16)
            {
                endpoint = "databases/" + CurrentDatabase + "/layouts/" + CurrentLayout + "/records/" + recordId;
                url = BaseUrl + endpoint;
            }

            // create a RestSharp request:
            var request = new RestRequest
            {
                Resource = endpoint,
                RequestFormat = DataFormat.Json,
                Method = Method.DELETE
            };
            request.AddHeader("Authorization", "Bearer " + token);

            // add scripts
            if (Version > 16 && scripts != null)
            {
                // build url addendum here
                url = url + buildScriptsURLpart(scripts);
                request = buildScriptsURLpart(request, scripts);
            }

            IRestResponse apiResponse = await restClient.ExecuteTaskAsync(request);
            string resultJson = apiResponse.Content;

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                if (Version == 16)
                {
                    var received = JsonConvert.DeserializeObject<RecordManipulationResponse>(resultJson);
                    SetLastError(received.errorCode, received.errorMessage);
                }
                else if (Version > 16)
                {
                    var received = JsonConvert.DeserializeObject<Response17>(resultJson);
                    SetLastError( Convert.ToInt32(received.messages[0].code), received.messages[0].message, received.response);
                }
            }
            else
            {
                // regular webclient / httpclient:
                // SetLastError((int)apiResponse.StatusCode, apiResponse.ReasonPhrase);
                SetLastError((int)apiResponse.StatusCode, apiResponse.StatusDescription);
            }


            return lastErrorCode;
        }

        /// <summary>
        /// Deletes a related record for a specified parent record.
        /// </summary>
        /// <param name="parentRecordId">The parent record Id.</param>
        /// <param name="relatedTableOccurance">The related table occurance name (portal name if it exists).</param>
        /// <param name="relatedRecordId">The related record Id.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
        /// <remarks>You can only delete one child at a time currently.  The way FMS has implemented this with the 'deleteRelated' key.</remarks>
        public async Task<int> DeleteRelatedRecord(int parentRecordId, string relatedTableOccurance, int relatedRecordId)
        {

            // you can only delete children by starting at the parent
            // you can only delete one child at a time otherwise you'll have duplicate 'deleteRelated' keys in the JSON payload

            // deletes go through the edit request by specifying this in the data node
            // so basically "deleteRelated" as if it were a field name
            // "deleteRelated": "Orders.2" 		  //delete related record on "Orders" table with a Record ID of 2

            var request = new RecordEditRequest(this, parentRecordId);
            request.AddField("deleteRelated", relatedTableOccurance + "." + relatedRecordId.ToString());
            var response = await request.Execute();

            return response;
        }

        /// <summary>
        /// Sets multiple global fields.
        /// </summary>
        /// <param name="fieldsAndValues">List of <see cref="Field"/> objects.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
        /// <remarks>The <see cref="Field"/> object allows for setting the repetition number.</remarks>
        public async Task<int> SetMultipleGlobalField(List<Field> fieldsAndValues)
        {
            string url = string.Empty;
            if (Version == 16)
                url = BaseUrl + "/global/" + CurrentDatabase + "/" + CurrentLayout + "/";
            else if (Version > 16)
                url = BaseUrl + "databases/" + CurrentDatabase + "/globals";


            JObject payloadJson = new JObject();

            // add the fields
            JObject pairs = new JObject();
            foreach (Field f in fieldsAndValues)
            {
                    JProperty pair = new JProperty(f.fullName, f.fieldValue);
                    pairs.Add(pair);
            }
            // add the main "" key and the JSON for the pairs
            payloadJson.Add("globalFields", pairs);


            StringContent body = new StringContent(payloadJson.ToString(), Encoding.UTF8, "application/json");

            HttpResponseMessage apiResponse = null;
            if (Version == 16)
                apiResponse = await webClient.PutAsync(url, body);
            else if(Version > 16 )
                apiResponse = await PatchAsync(webClient, url, body);

            string resultJson = await apiResponse.Content.ReadAsStringAsync();


            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                if (Version == 16)
                {
                    var received = JsonConvert.DeserializeObject<RecordManipulationResponse>(resultJson);
                    SetLastError(received.errorCode, received.errorMessage);
                }
                else if (Version > 16)
                {
                    var received = JsonConvert.DeserializeObject<Response17>(resultJson);
                    SetLastError(Convert.ToInt32(received.messages[0].code), received.messages[0].message, received.response);
                }
            }
            else
            {
                // regular webclient / httpclient:
                // SetLastError((int)apiResponse.StatusCode, apiResponse.ReasonPhrase);
                SetLastError((int)apiResponse.StatusCode, apiResponse.ReasonPhrase);
            }

            return lastErrorCode;
        }


        /// <summary>
        /// Sets a single global field.
        /// </summary>
        /// <param name="fieldName">Name of the global field.</param>
        /// <param name="TO">The Table Occurence for the global field</param>
        /// <param name="value">The value.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
        public async Task<int> SetSingleGlobalField(string fieldName, string TO, string value)
        {
            List<Field> fields = new List<Field>();
            fields.Add(new Field(fieldName, TO, 1, value, Version));
            var received = await SetMultipleGlobalField(fields);
            return received;
        }


        /// <summary>
        /// Sets a single global repeating field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="TO">The Table Occurence for the global field<</param>
        /// <param name="value">The value.</param>
        /// <param name="repetition">The repetition.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
        public async Task<int> SetSingleGlobalField(string fieldName, string TO, string value, int repetition)
        {
            List<Field> fields = new List<Field>();
            fields.Add(new Field(fieldName, TO, repetition, value, Version));
            var received = await SetMultipleGlobalField(fields);
            return received;
        }

        // this class and the edit request class could probably benefit from a base class to inherit from

        /// <exclude />
        public class RecordRequest
        {

            internal List<Field> fields { get; set; }
            internal List<KeyValuePair<string, string>> fieldKeyValuePairs { get; set; }
            internal string sessionToken { get; set; }
            internal string url { get; set; }
            internal string endpoint { get; set; }
            internal int modificationId { get; set; }

            internal FMS fms { get; private set; }


            internal List<FMSscript> fmScripts { get; set; }

            // for the result
            //public int recordId { get; internal set; }
            //public int errorCode { get; internal set; }
            //public string errorMessage { get; set; }
            /// <exclude />
            public RecordManipulationResponse response { get; internal set; }
            public Response17 NonRecordResponse { get; internal set; }

            // constructor
            internal RecordRequest(FMS fileMakerServer)
            {
                // sessionToken = fileMakerServer.token;
                fields = new List<Field>();
                fmScripts = new List<FMSscript>();
                fieldKeyValuePairs = new List<KeyValuePair<string, string>>();
                fms = fileMakerServer;
                if (fms.Version == 16)
                {
                    endpoint = "record/" + fileMakerServer.CurrentDatabase + "/" + fileMakerServer.CurrentLayout;
                    url = fileMakerServer.BaseUrl + endpoint;
                }
                else if (fms.Version > 16)
                {
                    endpoint = "databases/" + fileMakerServer.CurrentDatabase + "/layouts/" + fileMakerServer.CurrentLayout + "/records";
                    url = fileMakerServer.BaseUrl + endpoint;
                }
            }

            internal RecordRequest(FMS fileMakerServer, int modId) : this(fileMakerServer)
            {
                modificationId = modId;
            }

            /// <exclude />
            public int CreateRelatedRecord()
            {
                // same as normal record really but need to get field name in different format
                // perhaps don't do this here but create a new class and inherit from the normal recordcreate class?

                // there can be only one per request...

                // need to find a way to let users specify a TO once then provide field name value pairs
                return 0;
            }

            internal void populateLists(Field fmField)
            {
                // grab the name value pair to add to the list
                // list will be serialized as JSON for the payload
                fields.Add(fmField);
                fieldKeyValuePairs.Add(fmField.namveValuePair);
            }


            #region AddFieldOverloads


            /// <exclude />
            internal void AddField(string name, string value, string TO = null, int recId = 0, int repetition = 1, string portalName = null)
            {
                var fmField = new Field(name, TO, repetition, value, recId, fms.Version, portalName);
                populateLists(fmField);

            }

            internal void AddField(string name, string TO, int repetition, string value)
            {
                var fmField = new Field(name, TO, repetition, value, 0, fms.Version, string.Empty);
                populateLists(fmField);
            }

            /// <exclude />
            internal void AddField(string name, string TO, int repetition, string value, string portalName)
            {
                var fmField = new Field(name, TO, repetition, value, 0, fms.Version, portalName );
                populateLists(fmField);
            }

            internal void AddField(string name, string TO, string value, int recId)
            {
                var fmField = new Field(name, TO, 1, value, recId, fms.Version, string.Empty);
                populateLists(fmField);
            }

            /// <exclude />
            internal void AddField(string name, string TO, string value, int recId, string portalName)
            {
                var fmField = new Field(name, TO,1, value, recId, fms.Version, portalName);
                populateLists(fmField);
            }

            /// <exclude />
            public void AddField(string name, string value)
            {
                var fmField = new Field(name, value, fms.Version);
                populateLists(fmField);
            }

            /// <exclude />
            public void AddField(string name, string value, int repetition)
            {
                var fmField = new Field(name, repetition, value, fms.Version);
                populateLists(fmField);
            }

            #endregion

            // 'AddRelatedField' overloads
            #region AddRelatedFieldOverloads

            /// <summary>
            /// Adds a related field to the request.
            /// </summary>
            /// <param name="name">The field name.</param>
            /// <param name="TO">The table occurrence name / relationship name</param>
            /// <param name="repetition">The repetition number.</param>
            /// <param name="value">The field value.</param>
            /// <param name="relatedRecordId">The record Id.</param>
            public void AddRelatedField(string name, string TO, int repetition, string value, int relatedRecordId)
            {
                var fmField = new Field(name, TO, repetition, value, relatedRecordId, fms.Version, string.Empty);
                populateLists(fmField);
            }

            /// <summary>
            /// Adds the related field.
            /// </summary>
            /// <param name="name">The field name.</param>
            /// <param name="TO">The Table occurence or portal name that holds the field.</param>
            /// <param name="repetition">The field repetition.</param>
            /// <param name="value">The field value.</param>
            /// <param name="relatedRecordId">The related record id.</param>
            /// <param name="portalName">Name of the portal.</param>
            public void AddRelatedField(string name, string TO, int repetition, string value, int relatedRecordId, string portalName)
            {
                var fmField = new Field(name, TO, repetition, value, relatedRecordId, fms.Version, portalName);
                populateLists(fmField);
            }

            /// <summary>
            /// Adds a related field to the request.
            /// </summary>
            /// <param name="name">The field name.</param>
            /// <param name="TO">The table occurrence name / relationship name.</param>
            /// <param name="value">The field value.</param>
            /// <param name="relatedRecId">The related record Id.</param>
            public void AddRelatedField(string name, string TO, string value, int relatedRecId)
            {
                var fmField = new Field(name, TO, 1, value, relatedRecId, fms.Version, string.Empty);
                populateLists(fmField);
            }

            /// <summary>
            /// Adds the related field.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="TO">To.</param>
            /// <param name="value">The value.</param>
            /// <param name="relatedRecId">The related record identifier.</param>
            /// <param name="portalName">Name of the portal.</param>
            public void AddRelatedField(string name, string TO, string value, int relatedRecId, string portalName)
            {
                var fmField = new Field(name, TO, 1, value, relatedRecId, fms.Version, portalName);
                populateLists(fmField);
            }
            #endregion

            #region AddScripts            
            /// <summary>
            /// Adds a script to the request.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <param name="scriptName">Name of the script.</param>
            /// <param name="scriptParameter">The script parameter.</param>
            public void AddScript(ScriptTypes type, string scriptName, string scriptParameter)
            {
                FMSscript script = new FMSscript(type, scriptName, scriptParameter);
                fmScripts.Add(script);
            }

            /// <summary>
            /// Adds a script to the request.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <param name="scriptName">Name of the script.</param>
            public void AddScript(ScriptTypes type, string scriptName)
            {
                FMSscript script = new FMSscript(type, scriptName);
                fmScripts.Add(script);
            }
            #endregion


            /// <exclude />
            internal RecordManipulationResponse Parse16Response(string json)
            {

                response = JsonConvert.DeserializeObject<RecordManipulationResponse>(json);
                int code = response.errorCode;
                string message = response.errorMessage;
                fms.SetLastError(code, message);
                return response;
            }

            /// <exclude />
            internal Response17 Parse17Response(string json)
            {
                NonRecordResponse = JsonConvert.DeserializeObject<Response17>(json);
                int code = 0;
                if (NonRecordResponse.messages[0].code == null)
                    code = 9999;
                else
                    code = Convert.ToInt32(NonRecordResponse.messages[0].code);

                string message = NonRecordResponse.messages[0].message;
                fms.SetLastError(code, message, NonRecordResponse.response);
                return NonRecordResponse;
            }
        }


        /// <exclude />
        public class DeleteRecordRequest : RecordRequest
        {
            private int recId { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="DeleteRecordRequest"/> class.
            /// </summary>
            /// <param name="fileMakerServer">The filemaker server.</param>
            /// <param name="recordId">The record id to delete</param>
            public DeleteRecordRequest(FMS fileMakerServer, int recordId) : base(fileMakerServer)
            {
                recId = recordId;
            }

            /// <summary>
            /// Executes the request.
            /// </summary>
            /// <returns>Response object.</returns>
            public async Task<Response17> Execute()
            {
                // create the payload, an empty {}
                string payloadJson = string.Empty;
                /* no payload needed
                if (fms.Version == 16)
                    payloadJson = "{\"data\":{}}";
                else if (fms.Version > 16)
                    payloadJson = "{\"fieldData\":{}}";
                    */

                if (fms.Version == 16)
                    url = fms.BaseUrl + "/record/" + fms.CurrentDatabase + "/" + fms.CurrentLayout + "/" + recId;
                else if (fms.Version > 16)
                    url = fms.BaseUrl + "databases/" + fms.CurrentDatabase + "/layouts/" + fms.CurrentLayout + "/records/" + recId;
                HttpContent body = new StringContent(payloadJson);
                body.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // if there are scripts, add them
                if( fms.Version > 16 && fmScripts != null && fmScripts.Count > 0)
                {
                    url = url + fms.buildScriptsURLpart(fmScripts);
                }

                // doing a POST
                var apiResponse = await webClient.PostAsync(url, body);
                string resultJson = await apiResponse.Content.ReadAsStringAsync();
                NonRecordResponse = JsonConvert.DeserializeObject<Response17>(resultJson);

                return NonRecordResponse;
            }
        }

        /// <exclude />
        public class EmptyRecordCreateRequest : RecordRequest
        {

            /// <exclude />
            public EmptyRecordCreateRequest(FMS fileMakerServer) : base(fileMakerServer)
            { }
            

            /// <exclude />
            private async Task<RecordManipulationResponse> GetResponseFor16()
            {
                // create the payload, an empty {}
                string payloadJson = string.Empty;
                payloadJson = "{\"data\":{}}";

                HttpContent body = new StringContent(payloadJson);
                body.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // doing a POST
                var apiResponse = await webClient.PostAsync(url, body);
                string resultJson = await apiResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<RecordManipulationResponse>(resultJson);

                return response;
            }
        }


        /// <exclude />
        public class RecordCreateRequest : RecordRequest
        {
            /// <exclude />
            public RecordCreateRequest(FMS fileMakerServer) : base(fileMakerServer)
            {
            }

            /// <summary>
            /// Executes the request.
            /// </summary>
            /// <returns>error code.</returns>
            public async Task<int> Execute()
            {
                int newRecordId = 0;
                // get the fields list in json
                // create the payload
                RecordPayload payloadObject = new RecordPayload(fields, fms, fmScripts);

                string payloadJson = payloadObject.ToJsonString();

                
                var request = new RestRequest
                {
                    Resource = endpoint,
                    RequestFormat = DataFormat.Json,
                    Method = Method.POST
                };
                request.AddParameter( "application/json", payloadJson, ParameterType.RequestBody);
                request.AddHeader("Authorization", "Bearer " + fms.token);
                IRestResponse apiResponse = await restClient.ExecutePostTaskAsync(request);
                
                string resultJson = apiResponse.Content;

                if (fms.Version == 16)
                {
                    Parse16Response(resultJson);
                    if (fms.lastErrorCode == 0)
                        newRecordId = Convert.ToInt32(response.recordId);
                }
                else if(fms.Version > 16)
                {
                    Parse17Response(resultJson);
                    if (fms.lastErrorCode == 0)
                        newRecordId = Convert.ToInt32(NonRecordResponse.response.recordId);
                }

                return newRecordId;
            }


        }

        /// <exclude />
        public class RecordEditRequest : RecordRequest
        {
            private int parentRecordId;
            private int newModificationId;

            #region RecordEditRequestOverloads

            internal RecordEditRequest(FMS fileMakerServer) : base(fileMakerServer)
            {
                // calls the base class constructure with the passed parameters
            }

            internal RecordEditRequest(FMS fileMakerServer, int parentRecordId) : this(fileMakerServer)
            {
                this.parentRecordId = parentRecordId;
            }

            internal RecordEditRequest(FMS filemakerServer, int parentRecordId, int modId) : this(filemakerServer)
            {
                this.parentRecordId = parentRecordId;
                modificationId = modId;
            }

            #endregion

            /// <summary>
            /// Executes the request.
            /// </summary>
            /// <returns>error code.</returns>
            public async Task<int> Execute()
            {
                newModificationId = 0;

                // need to append the record id to the endpoint and URL
                // end point was already constructed as part of the RecordRequest initialization
                endpoint = endpoint + "/" + parentRecordId;
                url = url + endpoint;

                // get the fields list in json

                // create the payload
                RecordPayload payloadObject;
                if (modificationId > 0)
                    payloadObject = new RecordPayload(fields, modificationId, fms);
                else
                    payloadObject = new RecordPayload(fields, fms);

                var payloadJson = payloadObject.ToJsonString();

                var request = new RestRequest
                {
                    Resource = endpoint,
                    RequestFormat = DataFormat.Json,
                };
                request.AddParameter("application/json", payloadJson, ParameterType.RequestBody);
                request.AddHeader("Authorization", "Bearer " + fms.token);

                if (fms.Version == 16)
                {
                    // main difference with record creation is the PUT here instead of the POST
                    request.Method = Method.PUT;
                }
                else if(fms.Version > 16)
                {
                    request.Method = Method.PATCH;
                }
                IRestResponse apiResponse = restClient.Execute(request);
                var statusCode = apiResponse.StatusCode;
                string resultJson = apiResponse.Content;
                // status code is HttpStatusCode
                // but will have number 477 if Data Api Engine has an issue

                var reasonPhrase = apiResponse.StatusDescription;

                if (fms.Version == 16)
                {
                    response = Parse16Response(resultJson);
                    if (response.errorCode == 0)
                    {
                        // 16 does not return the mod id
                        newModificationId = Convert.ToInt32(response.recordId);
                    }
                }
                else if (fms.Version > 16)
                {
                    NonRecordResponse=Parse17Response(resultJson);
                    if (NonRecordResponse.messages[0].code == "0")
                        newModificationId = Convert.ToInt32(NonRecordResponse.response.modId);
                    else if (NonRecordResponse.messages[0].code == string.Empty || NonRecordResponse.messages[0].code == null)
                        NonRecordResponse.messages[0].code = "99999999999";
                }

                return newModificationId;
            }

        }

        internal string AddQuotes(string str)
        {
            return string.Format("\"{0}\"", str);
        }

        internal string extraQuery(List<Portal> portals)
        {
            // ?portal=["Portal1", "Portal2"]&offset.Portal1=1&range.Portal1=10 
            string output = "portal=[";

            int i = 0;
            foreach (Portal thePortal in portals)
            {
                if (i > 0)
                    output = output + ", ";
                output = output + AddQuotes(thePortal.portalName);
                ++i;
            }
            output = output + "]";

            string offset = "";
            string range = "";
            foreach (Portal thePortal in portals)
            {
                if (thePortal.startRecord > 0)
                    if(Version == 16)
                        offset = offset + "&offset." + thePortal.portalName + "=" + thePortal.startRecord;
                    else if(Version > 16)
                        offset = offset + "&_offset." + thePortal.portalName + "=" + thePortal.startRecord;

                if (thePortal.howManyRecords > 0)
                    if (Version == 16)
                        range = range + "&range." + thePortal.portalName + "=" + thePortal.howManyRecords;
                    else if (Version > 16)
                        range = range + "&_limit." + thePortal.portalName + "=" + thePortal.howManyRecords;
            }

            if (offset.Length > 1)
                output = output + offset;
            if (range.Length > 1)
                output = output + range;

            return output;
        }

        /// <exclude />
        public class RecordFindRequest
        {
            //internal string sessionToken { get; set; }
            internal int recordId { get; private set; } // the record ID to search for
            internal string url { get; set; }
            internal List<Portal> portals { get; set; }
            // internal FindCriteria findWhat { get; private set; }
            internal FMS fms { get; private set; }

            internal int howManyRecords { get; private set; } // range, default is 100
            internal int startRecord { get; private set; } // offset
            internal List<SortInstruction> sorts { get; private set; }
            internal string requestString { get; private set; }

            // each request is a list of field/value combos
            internal List<Request> requests { get; private set; }
            // public List<FieldSearch> fields { get; private set; }

            internal string responseLayout { get; private set; }

            private List<FMSscript> scripts { get; set; }

            // need a concept here for groups of fields in one request, a find can be multipe requests

            internal RecordFindRequest(FMS fileMakerServer) : this(fileMakerServer, 0, string.Empty)
            {}

            internal RecordFindRequest(FMS fileMakerServer, int recId, string layoutForResponse )
            {
                responseLayout = layoutForResponse;
                fms = fileMakerServer;
                recordId = recId;
                if (fms.Version == 16)
                {
                    if (recId == 0)
                        url = fileMakerServer.BaseUrl + "{{placeholder}}" + fms.CurrentDatabase + "/" + fms.CurrentLayout;
                    else
                        url = fileMakerServer.BaseUrl + "record/" + fms.CurrentDatabase + "/" + fms.CurrentLayout + "/" + recId;
                }
                else if (fms.Version > 16)
                {
                    if (recId == 0)
                        url = fileMakerServer.BaseUrl + "databases/" + fms.CurrentDatabase + "/layouts/" + fms.CurrentLayout; // + "/_find"; --> need to decide on _find later
                    else
                        url = fileMakerServer.BaseUrl + "databases/" + fms.CurrentDatabase + "/layouts/" + fms.CurrentLayout + "/records/" + recId;
                }
                // portals = portalsToInclude;
                // findWhat = new FindCriteria();
                howManyRecords = 0;
                startRecord = 1;
                requestString = MakeString();
                requests = new List<Request>();
                scripts = new List<FMSscript>();
            }

            internal RecordFindRequest(FMS fileMakerServer, string layoutForResponse) : this(fileMakerServer, 0, layoutForResponse)
            { }

            internal RecordFindRequest(FMS fileMakerServer, int recId) : this(fileMakerServer, recId, string.Empty)
            {}

            /// <summary>
            /// Adds a Portal to the search criteria.
            /// Remember that you can use named parameters to specify howManyRecords and not StartAtRecordNumber for instance.
            /// </summary>
            /// <param name="portalName">Has to the layout object name of the portal on the target layout.</param>
            /// <param name="howManyRecords">Optional. Will default to 100 as per FMS</param>
            /// <param name="StartAtRecordNumber">Optional.</param>
            public void AddPortal(string portalName, int howManyRecords = 0, int StartAtRecordNumber = 1)
            {
                Portal portal = new Portal();
                portal.portalName = portalName;
                portal.startRecord = StartAtRecordNumber;
                portal.howManyRecords = howManyRecords;

                if (portals == null)
                {
                    portals = new List<Portal>();
                }
                portals.Add(portal);
            }


            /// <summary>
            /// Adds a portal to the search criteria.
            /// </summary>
            /// <param name="portalName">Has to the layout object name of the portal on the target layout.</param>
            public void AddPortal(string portalName )
            {
                AddPortal(portalName, 0, 1);
            }

            /// <summary>
            /// Adds a script to the request.
            /// </summary>
            /// <param name="scriptType">Type of the script.</param>
            /// <param name="scriptName">Name of the script.</param>
            public void AddScript(ScriptTypes scriptType, string scriptName)
            {
                FMSscript script = new FMSscript(scriptType, scriptName);
                scripts.Add(script);
            }

            /// <summary>
            /// Adds a script to the request.
            /// </summary>
            /// <param name="scriptType">Type of the script.</param>
            /// <param name="scriptName">Name of the script.</param>
            /// <param name="scriptParameter">The script parameter.</param>
            public void AddScript(ScriptTypes scriptType, string scriptName, string scriptParameter)
            {
                FMSscript script = new FMSscript(scriptType, scriptName, scriptParameter);
                scripts.Add(script);
            }

            private string MakeString()
            {
                string fullString = "";
                string range = "";
                string offset = "";
                string sort = "";
                List<string> pieces = new List<string>();

                if (sorts != null && sorts.Count > 0)
                {
                    if (fms.Version == 16)
                        sort = "sort=" + JsonConvert.SerializeObject(sorts);
                    else if (fms.Version > 16)
                        sort = "_sort=" + JsonConvert.SerializeObject(sorts);

                    pieces.Add(sort);
                }

                if (howManyRecords > 0)
                {
                    if (fms.Version == 16)
                        range = "range=" + howManyRecords;
                    else if (fms.Version > 16)
                        range = "_limit=" + howManyRecords;
                    pieces.Add(range);
                }
                if (startRecord > 1)
                {
                    if (fms.Version == 16)
                        offset = "offset=" + startRecord;
                    else if (fms.Version > 16)
                        offset = "_offset=" + startRecord;
                    pieces.Add(offset);
                }

                if (pieces.Count > 0)
                {
                    fullString = string.Join("&", pieces.ToArray());
                }

                return fullString;
            }

            /// <summary>
            /// Executes the request.
            /// </summary>
            /// <returns><see cref="RecordsGetResponse"/> object</returns>
            public async Task<RecordsGetResponse> Execute()
            {

                // if there is no query part then use Get
                // if there is a query part then use Post and the sort also has to go in the body json payload

                HttpResponseMessage apiResponse;
                string resultJsonString;

                if (requests.Count == 0 || recordId > 0)
                {
                    // not searching on specific fields so no 'query' POST to make, just a GET
                    // with everything on the URL

                    // in case did not yet choose the right endpoint (we don't always know when creating the find request
                    if (!url.EndsWith("/records") && !url.EndsWith(recordId.ToString()))
                        url = url + "/records";

                    requestString = MakeString();

                    List<string> pieces = new List<string>();

                    // add the portals
                    string portalString = "";
                    if (portals != null && portals.Count > 0)
                    {
                        portalString = fms.extraQuery(portals);
                        pieces.Add(portalString);

                    }

                    // add the scripts
                    // could use this one below but needs to not add ? etc
                    // fms.buildScriptsURLpart(scripts)
                    if(scripts !=null && scripts.Count > 0)
                    {
                        foreach (FMSscript s in scripts)
                        {
                            if (s.type == ScriptTypes.after)
                            {
                                pieces.Add("script=" + s.name);
                                if (s.parameter != null)
                                {
                                    pieces.Add("script.param=" + s.parameter);
                                }
                            }
                            else if (s.type == ScriptTypes.before)
                            {
                                pieces.Add("script.prerequest=" + s.name);
                                if (s.parameter != null)
                                {
                                    pieces.Add("script.prerequest.param=" + s.parameter);
                                }
                            }
                            else if (s.type == ScriptTypes.beforeSort)
                            {
                                pieces.Add("script.presort=" + s.name);
                                if (s.parameter != null)
                                {
                                    pieces.Add("script.presort.param=" + s.parameter);
                                }
                            }
                        }
                    }

                    // add the response layout
                    if (responseLayout != string.Empty)
                        pieces.Add("layout.response=" + responseLayout);

                    //
                    string findString = requestString;
                    if (findString != null && findString.Length > 0)
                        pieces.Add(findString);

                    if (pieces.Count > 0)
                        url = url + "?" + string.Join("&", pieces.ToArray());

                    if(fms.Version == 16)
                        url = url.Replace("{{placeholder}}", "record/");
                    // using GET
                    apiResponse = await webClient.GetAsync(url);
                }
                else
                {
                    // need to decide here if this is still a GET and we need the /records endpoint
                    // or an actual fnd and we need /_find
                    if (!url.EndsWith("/_find"))
                        url = url + "/_find";

                    // we are searching on fields so we need to POST the query
                    // construct the body
                    JObject payloadJson = new JObject();

                    // add the fields for each request
                    JArray queries = new JArray();
                    foreach (Request r in requests)
                    {
                        JObject group = new JObject();
                        foreach (FieldSearch f in r.fields)
                        {
                            JProperty pair = new JProperty(f.fieldName, f.fieldValue);
                            group.Add(pair);
                            if (f.omit == true)
                            {
                                pair = new JProperty("omit", "true");
                                group.Add(pair);
                            }
                        }
                        queries.Add(group);
                    }
                    // add the query key and it's array of requests to the json
                    payloadJson.Add("query" , queries);

                    // add the sorts
                    if(sorts != null && sorts.Count > 0)
                    {
                        JArray allSorts = new JArray();
                        foreach (SortInstruction s in sorts)
                        {
                            JObject pair = new JObject();
                            pair.Add("fieldName", s.fieldName);
                            pair.Add("sortOrder", s.sortOrder);
                            allSorts.Add(pair);
                        }
                        payloadJson.Add("sort", allSorts);
                    }

                    if(howManyRecords > 0)
                        payloadJson.Add("range", howManyRecords.ToString());

                    if(startRecord > 1)
                        payloadJson.Add("offset", startRecord.ToString());

                    if (responseLayout != string.Empty)
                        payloadJson.Add("layout.response", responseLayout);

                    if (fms.Version > 16 && scripts != null && scripts.Count >= 1)
                    {
                        foreach (FMSscript s in scripts)
                        {
                            if (s.type == ScriptTypes.after)
                            {
                                payloadJson.Add("script", s.name);
                                if (s.parameter != null)
                                {
                                    payloadJson.Add("script.param", s.parameter);
                                }
                            }
                            else if (s.type == ScriptTypes.before)
                            {
                                payloadJson.Add("script.prerequest", s.name);
                                if (s.parameter != null)
                                {
                                    payloadJson.Add("script.prerequest.param", s.parameter);
                                }
                            }
                            else if (s.type == ScriptTypes.beforeSort)
                            {
                                payloadJson.Add("script.presort", s.name);
                                if (s.parameter != null)
                                {
                                    payloadJson.Add("script.presort.param", s.parameter);
                                }
                            }
                        }
                        /*
                            "script": "log",
                            "script.param": "runs after record creation and after sorting",
                            "script.prerequest": "log",
                            "script.prerequest.param": "runs before the record is created",
                            "script.presort": "some_script",
                            "script.presort.param": "runs after record creation but before sorting"
                         * */
                    }


                    // need to escape the data \n \r and so on
                    // (NOTE: ", \, /,\n, \r and \t characters must be escaped using
                    // \", \\ , V , \\n, \\r and \\t in strings. See the JSON specification.). 

                    // handle the portals, the offset and range for each portal requires the key name to
                    // be the name of the portal

                    if (portals !=null && portals.Count > 0)
                    {
                        JArray portalNames = new JArray();
                        //string[] portalNames = new string[portals.Length];
                        foreach (Portal p in portals)
                        {
                            string pName = p.portalName;
                            portalNames.Add(pName);
                            if(p.howManyRecords > 0)
                                if(fms.Version == 16)
                                    payloadJson.Add("range." + pName, p.howManyRecords);
                                else if(fms.Version > 16)
                                    payloadJson.Add("limit." + pName, p.howManyRecords);
                            if (p.startRecord > 1)
                                payloadJson.Add("offset." + pName, p.startRecord);
                        }
                        // add the names of the portals
                        payloadJson.Add("portal" , portalNames);
                    }

                    string body = payloadJson.ToString();

                    // POST the payload
                    if (fms.Version == 16)
                        url = url.Replace("{{placeholder}}", "find/");
                    apiResponse = await webClient.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
                }

                // work with the response
                resultJsonString = await apiResponse.Content.ReadAsStringAsync();
                JObject resultJson = JObject.Parse(resultJsonString);


                RecordsGetResponse received = new RecordsGetResponse();
                if (fms.Version == 16)
                {
                    var response = JsonConvert.DeserializeObject<RecordManipulationResponse>(resultJsonString);
                    int code = response.errorCode;
                    string message = response.errorMessage;
                    if (code == 0)
                        received = new RecordsGetResponse(resultJson);
                    else
                        received = new RecordsGetResponse(Convert.ToInt16(apiResponse.StatusCode), apiResponse.ReasonPhrase);
                    fms.SetLastError(code, message);
                }
                else if (fms.Version > 16)
                {
                    var response = JsonConvert.DeserializeObject<Response17>(resultJsonString);

                    int code = Convert.ToInt32(response.messages[0].code);
                    string message = response.messages[0].message;
                    if (code == 0 && apiResponse.StatusCode == HttpStatusCode.OK)
                        received = new RecordsGetResponse(resultJson);
                    else if (code == 0 && apiResponse.StatusCode != HttpStatusCode.OK)
                    {
                        code = (int)apiResponse.StatusCode;
                        received = new RecordsGetResponse(resultJson);
                    }
                    else
                        received = new RecordsGetResponse(Convert.ToInt16(apiResponse.StatusCode), apiResponse.ReasonPhrase + " - FMS says: " + response.messages[0].message);
                    fms.SetLastError(code, message, response.response);
                }
                return received;
            }

            /// <summary>
            /// Adds a sort instruction to the request.
            /// </summary>
            /// <param name="name">The name of the field to sort on.</param>
            /// <param name="direction">The sort direction, enum <see cref="SortDirection"/></param>
            public void AddSortField(string name, SortDirection direction)
            {
                AddSortFieldInternal(name, direction.ToString());
            }

            /// <summary>
            /// Adds a sort instruction to the request.
            /// </summary>
            /// <param name="name">The name of the field to sort on.</param>
            /// <param name="valueListName">Name of the value list to use for sorting.</param>
            public void AddSortField(string name, string valueListName)
            {
                AddSortFieldInternal(name, valueListName);
            }

            internal void AddSortFieldInternal(string name, string order)
            {
                if (sorts == null)
                    sorts = new List<SortInstruction>();

                var theSort = new SortInstruction { fieldName = name, sortOrder = order };
                sorts.Add(theSort);
                // requestString = makeString();
                // may need to change this in case there is also a query, then it should not be in the string
                // so perhaps don't make the string here yet....
            }

            /// <summary>
            /// Add a find query/request to a set of query criteria
            /// </summary>
            /// <returns>Request.</returns>
            /// <remarks>==> Need to adjust accessibility to make sure a request can only be added through this mechanism</remarks>
            public Request SearchCriterium()
            {
                Request newRequest = new Request();
                requests.Add(newRequest);
                return newRequest;
            }

            /// <summary>
            /// Sets the start record for the record set to be returned.
            /// </summary>
            /// <param name="firstRecord">The number of the first record to be returned.</param>
            public void SetStartRecord(int firstRecord)
            {
                startRecord = firstRecord;
                //requestString = makeString();
            }

            /// <summary>
            /// Sets how many records should be in the returned result.
            /// </summary>
            /// <param name="howMany">How many records to include in the returned result..</param>
            public void SetHowManyRecords(int howMany)
            {
                howManyRecords = howMany;
               //  requestString = makeString();
            }
        }



        /// <exclude />
        internal class Portal
        {
            public string portalName { get; set; }  // could be the name of the related TO or the object name of the portal
            public int startRecord { get; set; } // offset
            public int howManyRecords { get; set; } // range
        }


        /// <summary>
        /// Represents a find instructions for the query
        /// </summary>
        /// <remarks> ==> same as the <see cref="FieldSearch"/> class??  Check usage</remarks>
        public class Request
        {
            // is a set of fields to search on, each FM find request can have multiple fields
            internal List<FieldSearch> fields { get; private set; }

            /// <summary>
            /// Adds a field to the search.
            /// </summary>
            /// <param name="field">The field name.</param>
            /// <param name="searchFor">The field value, all FileMaker wild cards are supported.</param>
            /// <param name="omit">if set to <c>true</c> the field and value are omitted from the search results</param>
            public void AddFieldSearch(string field, string searchFor, bool omit)
            {
                FieldSearch searchField = new FieldSearch(field, searchFor, omit);

                if (fields == null)
                    fields = new List<FieldSearch>();

                fields.Add(searchField);
            }

            /// <summary>
            /// Adds a field to the search.
            /// </summary>
            /// <param name="field">The field name.</param>
            /// <param name="searchFor">The field value, all FileMaker wild cards are supported.</param>
            public void AddFieldSearch(string field, string searchFor)
            {
                AddFieldSearch(field, searchFor, false);
            }

        }

        /// <summary>
        /// Represents a find instructions for the query
        /// </summary>
        /// <remarks> ==> could be duplicate with <see cref="Request"/>, may need to consolidate</remarks>
        public class FieldSearch
        {
            // could this be just the Field Class and we add the 'omit' attribute there?
            // but if we do we also expose a lot of the other attributes that don't apply?

            internal string fieldName { get; private set; }
            internal string fieldValue { get; private set; }
            internal bool omit { get; private set; }

            /// <summary>
            /// Creates a new field search
            /// </summary>
            /// <param name="name">The field name to search on</param>
            /// <param name="value">The value to search for</param>
            /// <param name="omitFromResult">Set to 'true' to omit the result from the found set</param>
            /// <remarks>need the constructor to escape field values (tab, return,...)?</remarks>
            public FieldSearch(string name, string value, bool omitFromResult)
            {
                fieldName = name;
                fieldValue = value;
                omit = omitFromResult;
            }

            /// <summary>
            /// Creates a new field search
            /// </summary>
            /// <param name="name">The field name to search on</param>
            /// <param name="value">The value to search for</param>
            /// <remarks>need the constructor to escape field values (tab, return,...)?</remarks>
            public FieldSearch(string name, string value) : this( name, value, false)
            { }

        }

       

        internal class SortInstruction
        {
            public string fieldName { get; set; }
            public string sortOrder { get; set; }
        }

        /// <summary>
        /// Enum SortDirection, to use in Sort Order instructions
        /// </summary>
        public enum SortDirection
        {
            /// <summary>
            /// Sort ascending
            /// </summary>
            ascend,
            /// <summary>
            /// Sort descending
            /// </summary>
            descend
        }
    }


}
