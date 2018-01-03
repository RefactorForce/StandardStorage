# StandardStorage
A direct reimplementation of PCLStorage in .NET Standard. 

# Note
This project keeps 100% of the public API of the portable PCLStorage assemblies, and all functionality is externally identical, and all current valid usage of PCLStorage should work perfectly with StandardStorage witout any sort of refactoring. 
The difference is that this entire project is implemented in pure .NET Standard with a "one size fits all" approach. 
All platforms that support .NET Standard 2.0 should be able to take advantage of this project without any hiccups.

The only difference is the namespace; change `PCLStorage` to `StandardStorage`, and everything should still be operational.

# Important Warning
If you have been using PCLStorage, and have hard-coded the storage locations for your app specifically, please note that depending on which path values you used from PCLStorage, the locations may have changed.
Storage paths are now created with the following format:

    <Company Name (inferred, or declared in AssemblyInfo)>/<Main Referencing Project Name>/<Version>

Please also note that the directory separators (slashes in the above formatting declaration) represent the platform-specific directory separator specified by `Path.VolumeSeparatorChar`.

# Contributing
To contribute to this project please submit a pull request, and it will be considered based on how many breaking changes there are, how many bugs were fixed, and how much the structure has changed. Please note that I would like to differ the existing API as little as possible from PCLStorage, in order to make it easier for projects previously referencing PCLStorage to migrate as hassle-free as possible. However, adding to the public API is completely fine.

# Extra Useful Tidbits
 - Check out this link to one of my PasteBin posts containing the code to get all of the supported `System.Environment.SpecialFolder` paths for the calling platform. It also contains the results for Windows 10, iOS, Android, and macOS. If you would like to contribute by adding more results please send me a message containing the results and the operating system that they were found on. Here is the link: https://pastebin.com/aqYDiayD
