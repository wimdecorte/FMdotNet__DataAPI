﻿using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System;
using Newtonsoft.Json;

namespace FMdotNet__DataAPI
{
    /// <summary>
    /// The response returned when finding a record or a set of records.
    /// </summary>
    public class RecordsGetResponse
    {
        /// <summary>
        /// Error number generated by the find request. 0 for no error
        /// </summary>
        public int ErrorCode { get; private set; }
        /// <summary>
        /// Error code returned by the script called before the request.
        /// </summary>
        public int ErrorCodeScriptBefore { get; private set;}

        /// <summary>
        /// Error code returned by the script called after the request.
        /// </summary>
        public int ErrorCodeScriptAfter { get; private set; }

        /// <summary>
        /// Error code returned by the script called before the sorting.
        /// </summary>
        public int ErrorCodeScriptBeforeSort { get; private set; }

        /// <summary>
        /// Result string generated by the find request (e.g. "OK" or "Bad Request").
        /// </summary>
        public string result { get;  private set; }

        /// <summary>
        /// Total number of records in the table.
        /// </summary>
        public int totalRecordCount { get; private set; }

        /// <summary>
        /// The total record count in the found set.
        /// </summary>
        public int FoundRecordCount { get; private set; }

        /// <summary>
        /// Total number of records returned in this result.
        /// </summary>
        public int returnedRecordCount { get; private set; }


        /// <summary>
        /// FMS 18+, information about the context for the found set and the # of records
        /// </summary>
        public DataInfo SourceInfo { get; private set; }

        

        /// <summary>
        /// The FileMaker Data, in an <see cref="FMData"/> object
        /// </summary>
        /// <remarks><see cref="FMData"/></remarks>
        public FMData data { get; private set; }

        /// <summary>
        /// The raw JSON returned by the FileMaker Data API.
        /// </summary>
        public string rawJson { get; private set; }

        // constructor
        internal RecordsGetResponse(JObject json)
        {
            rawJson = json.ToString();

            var response = JsonConvert.DeserializeObject<Received>(rawJson);
            ErrorCodeScriptBefore = Convert.ToInt32(response.Response.ScriptErrorPreRequest);
            ErrorCodeScriptBeforeSort = Convert.ToInt32(response.Response.ScriptErrorPreSort);
            ErrorCodeScriptAfter = Convert.ToInt32(response.Response.ScriptError);

            
            SourceInfo = response.Response.DataInfo;
            FoundRecordCount = SourceInfo.foundCount;
            totalRecordCount = SourceInfo.totalRecordCount;
            returnedRecordCount = SourceInfo.returnedCount;

            ErrorCode = (int)json["messages"][0]["code"];
            result = (string)json["messages"][0]["message"];
            JArray dataArray = (JArray)json["response"]["data"];

            data = new FMData("parent");

            int i = 0;
            // each element in the array here is a record in the found set, a "parent" record
            foreach (JObject record in dataArray)
            {
                // add a row to the DataTable
                
                string parentId = (string)record["recordId"];
                string modId = (string)record["modId"];
                FMRecord row = new FMRecord(record.ToString());
                row.setRecordId(parentId);
                row.setModificationId(modId);

                JObject fieldData = (JObject)record["fieldData"];

                // each element here is a field/value pair
                foreach (JProperty field in fieldData.Properties())
                {
                    string fieldName = field.Name;

                    // ==> need to handle here if field name has "::" in it, that it belongs to a related table
                    // that related table may also be set through portal data so check for the name later!

                    string fieldValue = (string)field.Value;
                    row.addFieldAndData(fieldName, fieldValue);
                }


                // check for portalData
                // ==> what if none?

                JObject portalData = (JObject)record["portalData"];
                // each child key here is a portal, not in an array
                // each portal key holds an array of portal records
                foreach (JProperty portal in portalData.Properties())
                {
                    // each property is a relationship/portal

                    // portal name can either be the related TO or the name of the portal object on the layout
                    string name = portal.Name;
                    bool nameHandled = false;
                    FMRecordSet child = new FMRecordSet(name, name);

                    // now loop through array of related records
                    JArray childRecords = (JArray)portal.Value;
                    int x = 0;
                    foreach (JObject childRecord in childRecords)
                    {
                        FMRecord portalRow = new FMRecord(childRecord.ToString());
                        portalRow.setRecordId((string)childRecord["recordId"]);
                        portalRow.setModificationId((string)childRecord["modId"]);
                        foreach (JProperty field in childRecord.Properties())
                        {
                            // get rid of the TO name in the field

                            string fieldName = field.Name;
                            string nameOnly;

                            if (fieldName.Contains("::"))
                            {
                                string[] split = Regex.Split(fieldName, "::");
                                string TOname = split[0];
                                nameOnly = split[1];
                                if (nameHandled == false)
                                {
                                    if (TOname == name)
                                        child.updateNames(name, null);
                                    else
                                        child.updateNames(TOname, name);

                                    // add the TO name to the list of related tables if we don't have it yet
                                    if (!data.relatedTableNames.Contains(TOname))
                                        data.relatedTableNames.Add(TOname);

                                    nameHandled = true;
                                }
                            }
                            else
                                nameOnly = fieldName;

                            string fieldValue = (string)field.Value;

                            portalRow.addFieldAndData(nameOnly, fieldValue);

                        }

                        // add the row to the record set
                        child.AddRecord(portalRow);
                        x++;
                    }

                    // add the child record set to the overall data
                    row.addRelatedRecords(child);
                }

                // add the row to the data
                data.foundSet.AddRecord(row);

                // increment the record counter for the parent records
                i++;
            }
        }

        internal RecordsGetResponse(int errCode, string errMessage)
        {
            ErrorCode = errCode;
            result = errMessage;
        }

        public RecordsGetResponse()
        {
        }
    }

    /// <summary>
    /// The response returned after editing a record.
    /// </summary>
    public class RecordManipulationResponse
    {
        /// <summary>
        /// The error number returned by FileMaker Server.
        /// </summary>
        public int errorCode { get; set; }

        /// <summary>
        /// The internal FileMaker record Id.
        /// </summary>
        public string recordId { get; set; }

        /// <summary>
        /// The error message returned by FileMaker Server.
        /// </summary>
        public string errorMessage { get; set; }
    }



    /// <summary>
    /// Represents a found set of records, could be the found set or its related records
    /// </summary>
    [Serializable]
    public class FMRecordSet
    {
        /// <summary>
        /// The name of the Table Occurrence for this found set. (Can be the relationship name if this is a related set of a given record in the found set).
        /// </summary>
        public string tableName { get; private set; }

        /// <summary>
        /// If this record set is a related record set, shown in a portal on the layout then this will be the name of the portal if a layout object name is assigned to it.
        /// </summary>
        public string tableLayoutObjectName { get; private set; }

        /// <summary>
        /// List of <see cref="FMRecord"/> records in the set.
        /// </summary>
        public List<FMRecord> records { get; private set; }


        internal FMRecordSet(string table, string objectName = null)
        {
            tableName = table;
            tableLayoutObjectName = objectName;
            records = new List<FMRecord>();
        }

        /// <summary>
        /// ?? is referenced but I'll have to figure out why
        /// </summary>
        /// <param name="record"></param>
        public void AddRecord(FMRecord record)
        {
            records.Add(record);
        }

        /// <summary>
        /// ?? is referenced but I can't remember what is is for
        /// </summary>
        /// <param name="table"></param>
        /// <param name="objectName"></param>
        public void updateNames(string table, string objectName)
        {
            tableName = table;
            tableLayoutObjectName = objectName;
        }
    }

    /// <summary>
    /// Represents one FileMaker Record, including its related records if the layout shows a portal of related records.
    /// </summary>
    [Serializable]
    public class FMRecord
    {
        /// <summary>
        /// The internal FileMaker Record Id of the record.  To be used in find and edit requests.
        /// </summary>
        public string recordId { get; private set; }

        /// <summary>
        /// The internal FileMaker Modification Id of the record.
        /// </summary>
        public string modId { get; private set; }

        /// <summary>
        /// Dictionary of field names and their data.
        /// </summary>
        /// <remarks>==> May be obsolete, redundant, check its only reference in the console app</remarks>
        public Dictionary<string, string> fieldsAndData { get; private set; }

        /// <summary>
        /// List of <see cref="FMRecordSet"/> related record sets.  Each element is a set of related records.  That would be if the layout has multiple portals on it.
        /// </summary>
        public List<FMRecordSet> relatedRecordSets { get; private set; }

        /// <summary>
        /// FMS 18+, info about the related TO and the number of records
        /// </summary>
        public RelatedSet[] RelatedSourceInfo { get; private set; }


        internal FMRecord(string recordJson)
        {
            fieldsAndData = new Dictionary<string, string>();
            var deserialized = JsonConvert.DeserializeObject<Record>(recordJson);
            RelatedSourceInfo = deserialized.RelatedFoundSet;

        }

        /// <summary>
        /// Method to add a field name and its data to the Dictionary <see cref="fieldsAndData"/>
        /// </summary>
        /// <param name="fieldName">The name of the field</param>
        /// <param name="data">The field data</param>
        internal void addFieldAndData(string fieldName, string data)
        {
            fieldsAndData.Add(fieldName, data);
        }


        internal void addRelatedRecords(FMRecordSet records)
        {
            if (relatedRecordSets == null)
                relatedRecordSets = new List<FMRecordSet>();
            relatedRecordSets.Add(records);
        }

        internal void setRecordId(string recId)
        {
            recordId = recId;
        }

        internal void setModificationId(string modificationId)
        {
            modId = modificationId;
        }
    }

    /// <summary>
    /// Represents all records in the found set.
    /// </summary>
    [Serializable]
    public class FMData
    {
        /// <summary>
        /// List of related table names found in the data.
        /// </summary>
        public List<string> relatedTableNames { get; private set; }



        /// <summary>
        /// Gets the records. <see cref="FMRecordSet"/>
        /// </summary>
        /// <value>
        /// The records.
        /// </value>
        public FMRecordSet foundSet { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="FMData"/> class.
        /// </summary>
        /// <param name="name">Name of the record set, usually for related records; the name of the relationship.</param>
        /// <param name="objectName">Optional, name of the object.</param>
        public FMData(string name, string objectName = null)
        {
            relatedTableNames = new List<string>();
            foundSet = new FMRecordSet(name, objectName);
        }

    }


}
