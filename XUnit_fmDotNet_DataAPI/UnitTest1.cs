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

        internal async void login()
        {
            fms.SetFile(file);
            token = await fms.Authenticate();
        }

        internal async void logout()
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
            Xunit.Assert.True(tokenLength > 10);
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
    }
}
