# FMdotNet__DataAPI


.NET wrapper (.NET Standard 2.0) around the FileMaker Server REST Data API.  

Updated for FileMaker Server 18's official release of the Data API.

FileMaker Server 16 introduced a new RESTful Data API.  This .NET wrapper makes it easy for any .NET developer to interact with that Data API without having to know the endpoints or understand the structure of the returned JSON.

Note that the FileMaker Server 17/18 version of the Data API is very different than the one with FileMaker Server 16.
The FileMaker Server 16 Data API has stopped working in September 2018, so all FMS16-specific code has been removed.

The original fmDotNet was a wrapper around the FileMaker Server XML API and is still valid.  It is available here on GitHub and as a NuGet package. (https://github.com/fuzzzerd/fmDotNet)

See this repo for a demo application: https://github.com/wimdecorte/fmDotNet__DataAPI_Demo_17 for FileMaker Server 17

To Do:
- This project started off as a PCL Library to maximize cross-platform compatibility.  Before releasing it here I've upgraded it to .NET Standard 2.0.  One of the things missing from PCL was System.Data.  Since those are available in .NET Standard 2.0 I am considering changing the return type of the custom FMData object to a native DataSet.
