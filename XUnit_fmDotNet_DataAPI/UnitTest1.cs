using System;
using Xunit;
using FMdotNet__DataAPI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace XUnit_fmDotNet_DataAPI
{
    public class FMStest
    {
        public FMStest()
        {
            // could read from config file here with the FMS address and file data
        }
        string token;

        static string address = "achttien.soliant.cloud";
        static string user = "rest";
        static string pw = "rest";
        string file = "the_UI";

        FMS18 fms = new FMS18(address, user, pw);

        internal string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        internal async void Login()
        {
            fms.SetFile(file);
            token = await fms.Authenticate();
        }

        internal async void Logout()
        {
            int result = await fms.Logout();
            token = string.Empty;
        }

        [Fact]
        public async Task TestLogin()
        {
            fms.SetFile(file);
            token = await fms.Authenticate();
            Console.Write(token);
            var tokenLength = token.Length;
            Xunit.Assert.True(tokenLength > 10 && token.Contains("error") == false);
        }

        [Fact]
        public async Task TestGetProductInfo()
        {
            DataAPIinfo info = await fms.GetProductInfo();
            string expectedName = "FileMaker Data API Engine";

            Assert.Equal(expectedName, info.name);
        }


        // test the metadata - get databases
        [Fact]
        public async Task TestMetadataFiles()
        {
            // no auth required for this call
            //login();
            List<FileMakerFile> files = await fms.GetFiles();
            var count = files.Count;
            //logout();

            Assert.NotEqual(0, count);
        }

        [Fact]
        public async Task TestGetLayouts()
        {
            Login();
            List<FileMakerLayout> layouts = await fms.GetLayouts();
            var count = layouts.Count;
            Logout();

            Assert.NotEqual(0, count);
        }

        [Fact]
        public async Task TestGetLayoutDetails()
        {
            Login();
            var layout = "FRUIT_utility";
            var details = await fms.GetLayoutDetails(layout);
            var countVLs = details.ValueLists.Length; // array
            var countFields = details.Fields.Length; // array
            var countPortals = details.Portals.Count; // dictionary
            Logout();

            Assert.True(countFields > 0 && countPortals > 0 && countVLs > 0);
        }

        [Fact]
        public async Task TestGetScripts()
        {
            Login();
            List<FileMakerScript> scripts = await fms.GetScripts();
            var count = scripts.Count;
            Logout();

            Assert.NotEqual(0, count);
        }

        // starting its own FMS because we don't want it to interfere with the other tests
        [Fact]
        public async Task TestLogout()
        {
            FMS18 fms2 = new FMS18(address, user, pw);
            fms2.SetFile(file);
            var token2 = await fms2.Authenticate();
            int result = await fms2.Logout();

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task TestFindWildcard()
        {
            Login();
            fms.SetLayout("FRUIT_utility");
            var find = fms.FindRequest();
            var request = find.SearchCriterium();
            request.AddFieldSearch("country", "*");
            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 && getFindResponse.recordCount == 100);
        }

        [Fact]
        public async Task TestDataSourcePopulated()
        {
            var targetLayout = "FRUIT_utility";

            Login();
            fms.SetLayout(targetLayout);
            var find = fms.FindRequest();
            var request = find.SearchCriterium();
            request.AddFieldSearch("country", "==Belgium");
            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 && getFindResponse.SourceInfo.layout == targetLayout);
        }

        [Fact]
        public async Task TestResultSetPopulated()
        {
            var targetLayout = "FRUIT_utility";

            Login();
            fms.SetLayout(targetLayout);
            var find = fms.FindRequest();
            var request = find.SearchCriterium();
            request.AddFieldSearch("country", "==Belgium");
            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 && getFindResponse.NumberOfRecords.foundCount > 0);
        }

        [Fact]
        public async Task TestRelatedResultSetPopulated()
        {
            var targetLayout = "FRUIT_utility";

            Login();
            fms.SetLayout(targetLayout);
            var find = fms.FindRequest();
            var request = find.SearchCriterium();
            request.AddFieldSearch("country", "==Belgium");
            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 && getFindResponse.data.foundSet.records[0].RelatedSourceInfo[0].database == "the_Data");
        }

        [Fact]
        public async Task TestFindWithScripts()
        {
            var targetLayout = "FRUIT_utility";

            Login();
            fms.SetLayout(targetLayout);
            var find = fms.FindRequest();
            var request = find.SearchCriterium();
            request.AddFieldSearch("country", "==Belgium");
            find.AddScript(ScriptTypes.before, "log", "parameter added, before request");
            find.AddScript(ScriptTypes.beforeSort, "log", "parameter added, before sort");
            find.AddScript(ScriptTypes.after, "log", "parameter added, after request");
            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 &&
                getFindResponse.NumberOfRecords.foundCount > 0 &&
                getFindResponse.ErrorCodeScriptBefore == 0 &&
                getFindResponse.ErrorCodeScriptBeforeSort == 0 &&
                getFindResponse.ErrorCodeScriptAfter == 0
                );
        }

        [Fact]
        public async Task TestOffsetAndLimit()
        {
            var targetLayout = "FRUIT_utility";

            Login();
            fms.SetLayout(targetLayout);
            var find = fms.FindRequest();
            var request = find.SearchCriterium();
            request.AddFieldSearch("country", "==Belgium");
            find.SetStartRecord(10);
            find.SetHowManyRecords(1);
            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 &&
                getFindResponse.NumberOfRecords.returnedCount == 1);
        }

        [Fact]
        public async Task TestGetRecords()
        {
            var targetLayout = "FRUIT_utility";

            Login();
            fms.SetLayout(targetLayout);
            var find = fms.FindRequest();
            find.SetStartRecord(10);
            find.SetHowManyRecords(10);
            find.AddScript(ScriptTypes.before, "log", "parameter added, before request");
            find.AddScript(ScriptTypes.beforeSort, "log", "parameter added, before sort");
            find.AddScript(ScriptTypes.after, "log", "parameter added, after request");
            find.AddSortField("fruit", FMS.SortDirection.descend);
            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 &&
                getFindResponse.NumberOfRecords.returnedCount == 10 &&
                getFindResponse.NumberOfRecords.foundCount > 0 &&
                getFindResponse.ErrorCodeScriptBefore == 0 &&
                getFindResponse.ErrorCodeScriptBeforeSort == 0 );
        }

        [Fact]
        public async Task TestGetRecordById()
        {
            var targetLayout = "FRUIT_utility";

            // assuming that the first record in that table is still the one with record id = 1
            var targetId = 1;

            Login();
            fms.SetLayout(targetLayout);
            var find = fms.FindRequest(targetId);

            find.AddScript(ScriptTypes.before, "log", "parameter added, before request");
            find.AddScript(ScriptTypes.beforeSort, "log", "parameter added, before sort");
            find.AddScript(ScriptTypes.after, "log", "parameter added, after request");

            var getFindResponse = await find.Execute();
            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(getFindResponse.ErrorCode == 0 &&
                getFindResponse.NumberOfRecords.returnedCount == 1 &&
                getFindResponse.data.foundSet.records[0].recordId == "1" &&
                getFindResponse.NumberOfRecords.foundCount == 1 &&
                getFindResponse.ErrorCodeScriptBefore == 0 &&
                getFindResponse.ErrorCodeScriptBeforeSort == 0);
        }

        [Fact]
        public async Task TestCreateRecord()
        {
            var targetLayout = "cake_utility";
            Login();
            fms.SetLayout(targetLayout);
            var request = fms.NewRecordRequest();
            request.AddField("cake", RandomString(50, false));
            request.AddField("wine_pairing", RandomString(50, false));
            request.AddField("country", RandomString(50, false));

            // repeating field
            request.AddField("repeating_field", "9", 2);

            request.AddScript(ScriptTypes.before, "log", "parameter added, before request");
            request.AddScript(ScriptTypes.beforeSort, "log", "parameter added, before sort");
            request.AddScript(ScriptTypes.after, "log", "parameter added, after request");

            var id = await request.Execute();

            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(request.Reply.messages[0].code == "0" && id > 0 && request.Reply.Response.ScriptError == "0");
        }

        [Fact]
        public async Task TestCreateRecordWithRelatedData()
        {
            var targetLayout = "cake_utility";
            Login();
            fms.SetLayout(targetLayout);
            var request = fms.NewRecordRequest();
            request.AddField("cake", RandomString(50, false));
            request.AddField("wine_pairing", RandomString(50, false));
            request.AddField("country", RandomString(50, false));

            // related field (will create releated record if relationship allows"
            // request.AddField("cake_FRUIT__ac::fruit", "related - " + RandomString(50, true));

            // but this is the preferred way
            request.AddRelatedField("fruit", "cake_FRUIT__ac", "related - " + RandomString(15, true));

            request.AddScript(ScriptTypes.before, "log", "parameter added, before request");
            request.AddScript(ScriptTypes.beforeSort, "log", "parameter added, before sort");
            request.AddScript(ScriptTypes.after, "log", "parameter added, after request");

            var id = await request.Execute();

            Logout();

            // default search with no other criteria returns 100 records
            Assert.True(request.Reply.messages[0].code == "0" && id > 0 && request.Reply.Response.ScriptError == "0");
        }

        [Fact]
        public async Task TestModifyRecordWithRelatedData()
        {
            var targetLayout = "cake_utility";
            Login();
            fms.SetLayout(targetLayout);

            // first create a record and capture the ids
            var request = fms.NewRecordRequest();
            request.AddField("cake", RandomString(50, false));
            request.AddField("wine_pairing", "first value");
            request.AddField("country", RandomString(50, false));
            request.AddRelatedField("fruit", "cake_FRUIT__ac", "related - first value");
            var id = await request.Execute();

            // get the data for the new record so that we can get the related id
            var find = fms.FindRequest(id);
            var reply = await find.Execute();
            var relatedId = reply.data.foundSet.records[0].relatedRecordSets[0].records[0].recordId;
            var TOname = reply.data.relatedTableNames[0];
            
            // now  modify that record
            var editRequest = fms.EditRequest(id);
            editRequest.AddField("wine_pairing", "second value");
            editRequest.ModifyRelatedField("fruit", TOname, "second value", Convert.ToInt32(relatedId));
            var newModId = await editRequest.Execute();

            Logout();

            // the mod id returned from the edit should be 1 because it was a new record and this was the first edit
            Assert.True((newModId == 1 && id > 0 && Convert.ToInt32(relatedId) > 0));
        }

        [Fact]
        public async Task TestDuplicateRecord()
        {
            var targetLayout = "cake_utility";
            Login();
            fms.SetLayout(targetLayout);

            // first create a record and capture the ids
            var request = fms.NewRecordRequest();
            request.AddField("cake", RandomString(50, false));
            var id = await request.Execute();

            // now  duplicate that record
            var duplicateRequest = fms.DuplicateRquest(id);
            duplicateRequest.AddScript(ScriptTypes.after, "log", "some param");
            var reply = await duplicateRequest.Execute();

            var errorCode = Convert.ToInt32(reply.messages[0].code);
            var newId = Convert.ToInt32(reply.Response.RecordId);

            Logout();

            // the mod id returned from the edit should be 1 because it was a new record and this was the first edit
            Assert.True(newId == (id + 1) && errorCode == 0 && reply.Response.ScriptError == "0");
        }

        [Fact]
        public async Task TestDeleteRecord()
        {
            var targetLayout = "cake_utility";
            Login();
            fms.SetLayout(targetLayout);

            // first create a record and capture the id
            var request = fms.NewRecordRequest();
            request.AddField("cake", RandomString(50, false));
            var id = await request.Execute();

            // duplicate that record
            var dup = fms.DuplicateRquest(id);
            var reply = await dup.Execute();
            var dupId = Convert.ToInt32(reply.Response.RecordId);

            // now  delete that record
            // direct command, no need to first get the request and then execute it
            // doesn't allow to run scripts
            var deleteRecordResult = await fms.DeleteRecord(id);
            

            // delete the dup record but also run a script
            var deleteRecord = fms.DeleteRequest(dupId);
            deleteRecord.AddScript(ScriptTypes.after, "log", "some param after deleting record");
            reply = await deleteRecord.Execute();

            var errorCode = Convert.ToInt32(reply.messages[0].code);

            Logout();

            // the mod id returned from the edit should be 1 because it was a new record and this was the first edit
            Assert.True(dupId == (id + 1) && deleteRecordResult == 0 && errorCode == 0 && reply.Response.ScriptError == "0");
        }
    }
}
