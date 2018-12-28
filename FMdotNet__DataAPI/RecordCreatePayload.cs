using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FMdotNet__DataAPI
{
	internal class RecordPayload
	{

        private List<Field> fields { get; set; }

        // private List<Field> fieldObjects { get; set; }

        // modification Id can be used if the user wants to edit a record only if it is still at the same mod id level
        private int modId { get; set; }
        private bool hasModId { get; set; }

        private int version { get; set; }

        private List<FMSscript> scripts { get; set; }

		// constructor
		public RecordPayload(List<Field> fieldsToWorkWith, FMS fileMakerServer )
		{
            fields = fieldsToWorkWith;
            hasModId = false;
            version = fileMakerServer.Version;
		}

        public RecordPayload(List<Field> fieldsToWorkWith, FMS fileMakerServer, List<FMSscript> fmScripts) : this(fieldsToWorkWith, fileMakerServer)
        {
            scripts = fmScripts;
        }
        public RecordPayload(List<Field> fieldsToWorkWith, int modificationId, FMS fileMakerServer)
        {
            fields = fieldsToWorkWith;
            modId = modificationId;
            hasModId = true;
            version = fileMakerServer.Version;
        }

        public RecordPayload(List<Field> fieldsToWorkWith, int modificationId, FMS fileMakerServer, List<FMSscript> fmScripts) : this(fieldsToWorkWith, modificationId, fileMakerServer)
        {
            scripts = fmScripts;
        }

        public RecordPayload( FMS fileMakerServer, List<FMSscript> fmScripts)
        {
            scripts = fmScripts;
            version = fileMakerServer.Version;
        }

        public RecordPayload(FMS fileMakerServer)
        {
            version = fileMakerServer.Version;
        }

        public string ToJsonString()
		{
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var writer = new JsonTextWriter(sw);
            List<Field> relatedFields = new List<Field>();
            List<string> portalNames = new List<string>();

            writer.WriteStartObject();
           
            if (fields != null && fields.Count > 0)
            {
                writer.WritePropertyName("fieldData");
                writer.WriteStartObject();

                foreach (var item in fields)
                {
                    // need to figure out if it is related or not
                    // and build a new list of just the related
                    if (item.tableOccurance == string.Empty)
                    {
                        writer.WritePropertyName(item.fullName);
                        writer.WriteValue(item.fieldValue);
                    }
                    else
                    {
                        if (item.portalObjectName != string.Empty && !portalNames.Contains(item.portalObjectName))
                            portalNames.Add(item.portalObjectName);
                        else if (!portalNames.Contains(item.tableOccurance))
                            portalNames.Add(item.tableOccurance);

                        relatedFields.Add(item);
                    }
                }

                writer.WriteEndObject(); // for data / fieldData
            }
            

            // for 17+, add the portal data
            if(relatedFields.Count > 0)
            {
                writer.WritePropertyName("portalData");
                writer.WriteStartObject();

                foreach (string pn in portalNames)
                {
                    writer.WritePropertyName(pn);
                    writer.WriteStartArray();
                    foreach (var item in relatedFields)
                    {
                        // need to figure out if it is related or not
                        // and build a new list of just the related
                        if (item.portalObjectName == pn || item.tableOccurance == pn)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName(item.fullName);
                            writer.WriteValue(item.fieldValue);
                            if(item.recordId > 0)
                            {
                                writer.WritePropertyName("recordId");
                                writer.WriteValue(item.recordId);
                            }
                            writer.WriteEndObject();
                        }
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndObject(); // for portalData
            }


            if(hasModId == true)
            {
                writer.WritePropertyName("modId");
                writer.WriteValue(modId);
            }
            if(scripts != null && scripts.Count >= 1 )
            {
                foreach(FMSscript s in scripts)
                {
                    if(s.type == ScriptTypes.after)
                    {
                        writer.WritePropertyName("script");
                        writer.WriteValue(s.name);
                        if(s.parameter != null)
                        {
                            writer.WritePropertyName("script.param");
                            writer.WriteValue(s.parameter);
                        }
                    }
                    else if(s.type == ScriptTypes.before)
                    {
                        writer.WritePropertyName("script.prerequest");
                        writer.WriteValue(s.name);
                        if (s.parameter != null)
                        {
                            writer.WritePropertyName("script.prerequest.param");
                            writer.WriteValue(s.parameter);
                        }
                    }
                    else if (s.type == ScriptTypes.beforeSort)
                    {
                        writer.WritePropertyName("script.presort");
                        writer.WriteValue(s.name);
                        if (s.parameter != null)
                        {
                            writer.WritePropertyName("script.presort.param");
                            writer.WriteValue(s.parameter);
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
            writer.WriteEndObject(); // for overall json
            writer.Close();

            return sw.ToString();
		}
	}
}
