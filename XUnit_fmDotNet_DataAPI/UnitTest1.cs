using System;
using Xunit;
using FMdotNet__DataAPI;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                getFindResponse.NumberOfRecords.returnedCount == 1 );
        }
    }
}
