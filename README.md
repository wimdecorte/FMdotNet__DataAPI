# FMdotNet__DataAPI
.NET wrapper (.NET Standard 2.0) around the FileMaker Server REST Data API.  

FileMaker Server 16 introduced a new RESTful Data API.  This .NET wrapper makes it easy for any .NET developer to interact with that Data API without having to know the endpoints or understand the structure of the returned JSON.

The original fmDotNet was a wrapper around the FileMaker Server XML API and is still valid.  It is available here on GitHub and as a NuGet package. (https://github.com/fuzzzerd/fmDotNet)

See this repo for a demo application: https://github.com/wimdecorte/FMdotNet__DataAPI_demo

To Do:
- This project started off as a PCL Library to maximize cross-platform compatibility.  Before releasing it here I've upgraded it to .NET Standard 2.0.  One of the things missing from PCL was System.Data.  Since those are available in .NET Standard 2.0 I am considering changing the return type of the custom FMData object to a native DataSet.
