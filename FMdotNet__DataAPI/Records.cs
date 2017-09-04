using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        /// Deletes a specified record.
        /// </summary>
        /// <param name="recordId">The internal FileMaker record Id.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object. </returns>
        public async Task<RecordManipulationResponse> DeleteRecord(int recordId)
        {
            string url = BaseUrl + "/record/" + CurrentDatabase + "/" + CurrentLayout + "/" + recordId;

            var apiResponse = await webClient.DeleteAsync(url);
            string resultJson = await apiResponse.Content.ReadAsStringAsync();
            var received = new RecordManipulationResponse();
            if (apiResponse.StatusCode == HttpStatusCode.OK)
                received = JsonConvert.DeserializeObject<RecordManipulationResponse>(resultJson);
            else
            {
                received.errorCode = (int)apiResponse.StatusCode;
                received.errorMessage = apiResponse.ReasonPhrase;
            }


            return received;
        }

        /// <summary>
        /// Deletes a related record for a specified parent record.
        /// </summary>
        /// <param name="parentRecordId">The parent record Id.</param>
        /// <param name="relatedTableOccurance">The related table occurance name (portal name if it exists).</param>
        /// <param name="relatedRecordId">The related record Id.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
        /// <remarks>You can only delete one child at a time currently.  The way FMS has implemented this with the 'deleteRelated' key.</remarks>
        public async Task<RecordManipulationResponse> DeleteRelatedRecord(int parentRecordId, string relatedTableOccurance, int relatedRecordId)
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
        public async Task<RecordManipulationResponse> SetMultipleGlobalField(List<Field> fieldsAndValues)
        {

            string url = BaseUrl + "/global/" + CurrentDatabase + "/" + CurrentLayout + "/";

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
      
            var apiResponse = await webClient.PutAsync(url, body);
            string resultJson = await apiResponse.Content.ReadAsStringAsync();
            var received = new RecordManipulationResponse();
            if (apiResponse.StatusCode == HttpStatusCode.OK)
                received = JsonConvert.DeserializeObject<RecordManipulationResponse>(resultJson);
            else
            {
                received.errorCode = (int)apiResponse.StatusCode;
                received.errorMessage = apiResponse.ReasonPhrase;
            }


            return received;
        }


        /// <summary>
        /// Sets a single global field.
        /// </summary>
        /// <param name="fieldName">Name of the global field.</param>
        /// <param name="value">The value.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
        public async Task<RecordManipulationResponse> SetSingleGlobalField(string fieldName, string value)
        {
            List<Field> fields = new List<Field>();
            fields.Add(new Field(fieldName, value));
            var received = await SetMultipleGlobalField(fields);
            return received;
        }


        /// <summary>
        /// Sets a single global repeating field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <param name="repetition">The repetition.</param>
        /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
        public async Task<RecordManipulationResponse> SetSingleGlobalField(string fieldName, string value, int repetition)
        {
            List<Field> fields = new List<Field>();
            fields.Add(new Field(fieldName, repetition, value ));
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
            internal int modificationId { get; set; }

            // for the result
            //public int recordId { get; internal set; }
            //public int errorCode { get; internal set; }
            //public string errorMessage { get; set; }
            /// <exclude />
            public RecordManipulationResponse response { get; internal set; }

            // constructor
            internal RecordRequest(FMS fileMakerServer)
            {
                // sessionToken = fileMakerServer.token;
                url = fileMakerServer.BaseUrl + "record/" + fileMakerServer.CurrentDatabase + "/" + fileMakerServer.CurrentLayout;
                fields = new List<Field>();
                fieldKeyValuePairs = new List<KeyValuePair<string, string>>();
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

            // 'AddField' overloads to cover all scenarios
            #region AddFieldOverloads


            /// <exclude />
            internal void AddField(string name, string value, string TO = null, int recId = 0, int repetition = 1)
            {
                var fmField = new Field(name, TO, repetition, value, recId);
                populateLists(fmField);

            }


            /// <exclude />
            internal void AddField(string name, string TO, int repetition, string value)
            {
                var fmField = new Field(name, TO, repetition, value);
                populateLists(fmField);
            }

            /// <exclude />
            internal void AddField(string name, string TO, string value, int recId)
            {
                var fmField = new Field(name, TO, value, recId);
                populateLists(fmField);
            }

            /// <exclude />
            public void AddField(string name, string value)
            {
                var fmField = new Field(name, value);
                populateLists(fmField);
            }

            /// <exclude />
            public void AddField(string name, string value, int repetition)
            {
                var fmField = new Field(name, repetition, value);
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
                var fmField = new Field(name, TO, repetition, value, relatedRecordId);
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
                var fmField = new Field(name, TO, value, relatedRecId);
                populateLists(fmField);
            }
            #endregion
        }


        /// <exclude />
        public class EmptyRecordCreateRequest : RecordRequest
        {

            /// <exclude />
            public EmptyRecordCreateRequest(FMS fileMakerServer) : base(fileMakerServer)
            { }

            /// <exclude />
            public async Task<RecordManipulationResponse> Execute()
            {
                // create the payload, an empty {}
                var payloadJson = "{\"data\":{}}";

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
            /// Executes this request.
            /// </summary>
            /// <returns><see cref="RecordManipulationResponse"/> object.</returns>
            public async Task<RecordManipulationResponse> Execute()
            {
                // get the fields list in json
                // create the payload
                var payloadObject = new RecordCreatePayload(fieldKeyValuePairs);
                var payloadJson = payloadObject.ToJSON();

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
        public class RecordEditRequest : RecordRequest
        {
            private int parentRecordId;
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
                this.modificationId = modId;
            }

            #endregion



            /// <summary>
            /// Executes this request.
            /// </summary>
            /// <returns><see cref="RecordManipulationResponse"/></returns>
            public async Task<RecordManipulationResponse> Execute()
            {
                // need to append the record id to the URL
                url = url + "/" + parentRecordId;

                // get the fields list in json

                // create the payload
                RecordCreatePayload payloadObject;
                if (modificationId > 0)
                    payloadObject = new RecordCreatePayload(fieldKeyValuePairs, modificationId);
                else
                    payloadObject = new RecordCreatePayload(fieldKeyValuePairs);

                var payloadJson = payloadObject.ToJSON();

                HttpContent body = new StringContent(payloadJson);
                body.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // send it through the httpclient
                // var client = new HttpClient();
                // client.DefaultRequestHeaders.Clear();
                // client.DefaultRequestHeaders.Add("FM-Data-token", sessionToken);

                // main difference with record creation is the PUT here instead of the POST
                var apiResponse = await webClient.PutAsync(url, body);
                var statusCode = apiResponse.StatusCode;
                // status code is HttpStatusCode
                // but will have number 477 if Data Api Engine has an issue

                var reasonPhrase = apiResponse.ReasonPhrase;
                string resultJson = await apiResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<RecordManipulationResponse>(resultJson);

                /*
                recordId = Convert.ToInt32(response.recordId);
                errorCode = Convert.ToInt32(response.errorCode);
                errorMessage = response.errorMessage;
                */

                return response;
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
                    offset = offset + "&offset." + thePortal.portalName + "=" + thePortal.startRecord;

                if (thePortal.howManyRecords > 0)
                    range = range + "&range." + thePortal.portalName + "=" + thePortal.howManyRecords;
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

            // need a concept here for groups of fields in one request, a find can be multipe requests

            internal RecordFindRequest(FMS fileMakerServer) : this(fileMakerServer, 0)
            {}

            internal RecordFindRequest(FMS fileMakerServer, int recId)
            {
                fms = fileMakerServer;
                recordId = recId;
                // sessionToken = fileMakerServer.token;
                if( recId == 0)
                    url = fileMakerServer.BaseUrl + "{{placeholder}}" + fileMakerServer.CurrentDatabase + "/" + fileMakerServer.CurrentLayout;
                else
                    url = fileMakerServer.BaseUrl + "record/" + fileMakerServer.CurrentDatabase + "/" + fileMakerServer.CurrentLayout + "/" + recId;

                // portals = portalsToInclude;
                // findWhat = new FindCriteria();
                howManyRecords = 0;
                startRecord = 1;
                requestString = makeString();
                requests = new List<Request>();
                
            }

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

            private string makeString()
            {
                string fullString = "";
                string range = "";
                string offset = "";
                string sort = "";
                List<string> pieces = new List<string>();

                if (sorts != null && sorts.Count > 0)
                {
                    sort = "sort=" + JsonConvert.SerializeObject(sorts);
                    pieces.Add(sort);
                }

                if (howManyRecords > 0)
                {
                    range = "range=" + howManyRecords;
                    pieces.Add(range);
                }
                if (startRecord > 1)
                {
                    offset = "offset=" + startRecord;
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

                    requestString = makeString();

                    List<string> pieces = new List<string>();
                    string portalString = "";
                    if (portals != null && portals.Count > 0)
                    {
                        portalString = fms.extraQuery(portals);
                        pieces.Add(portalString);

                    }
                    string findString = requestString;
                    if (findString != null && findString.Length > 0)
                        pieces.Add(findString);

                    if (pieces.Count > 0)
                        url = url + "?" + string.Join("&", pieces.ToArray());

                    url = url.Replace("{{placeholder}}", "record/");
                    // using GET
                    apiResponse = await webClient.GetAsync(url);
                }
                else
                {
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
                                payloadJson.Add("range." + pName, p.howManyRecords);
                            if (p.startRecord > 1)
                                payloadJson.Add("offset." + pName, p.startRecord);
                        }
                        // add the names of the portals
                        payloadJson.Add("portal" , portalNames);
                    }

                    string body = payloadJson.ToString();

                    // POST the payload
                    url = url.Replace("{{placeholder}}", "find/");
                    apiResponse = await webClient.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
                }

                // work with the response
                resultJsonString = await apiResponse.Content.ReadAsStringAsync();
                JObject resultJson = JObject.Parse(resultJsonString);

                RecordsGetResponse received;
                if (apiResponse.StatusCode == HttpStatusCode.OK)
                    received = new RecordsGetResponse(resultJson);
                else
                    received = new RecordsGetResponse(Convert.ToInt16(apiResponse.StatusCode), apiResponse.ReasonPhrase);

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
            public void setStartRecord(int firstRecord)
            {
                startRecord = firstRecord;
                //requestString = makeString();
            }

            /// <summary>
            /// Sets how many records should be in the returned result.
            /// </summary>
            /// <param name="howMany">How many records to include in the returned result..</param>
            public void setHowManyRecords(int howMany)
            {
                howManyRecords = howMany;
               //  requestString = makeString();
            }
        }



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
