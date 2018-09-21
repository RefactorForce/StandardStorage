# StandardStorage Legacy
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
 - The storage path formatting methodology is a direct functionality rip from `System.Windows.Forms.Application.LocalUserAppDataPath`. The method used to do this replication required a variety of tools and resources, but the basic gist is this:
    - Use http://referencesource.microsoft.com and search for what you want to replicate.
    - Copy the main section of the code and paste it into either a new project on https://www.dotnetfiddle.net or a new file in a C# project and add the namespaces that are needed, through the use of either Google or Intellisense; or both. 
    - Go off on as many tangents as there are, implementing every privately-declared method or property needed to make the code compile via copy and paste. My suggestion is to use the microsoft reference source (mentioned above) with [CTRL] + [CLICK] in your browser on to open each one in a new tab. Afterwards, copy them all into source, and try to fix them. Then, where necessary, repeat the processs.
    - When you encounter a dead end (something you cannot easily find an assembly for), look up the name of the class and/or member that you could not find an assembly for on Google, then write down the namespace that it is probably declared in. The microsoft reference source mentioned above may also help you.
    - First off, check NuGet; if there is a possible implemenation there, check relevent metadata to see if it should do and/or offer what you think it does, then use it if it feels appropriate.
    - If you couldn't find anything, get a quick-searching program such as "Everything" by voidtools (https://www.voidtools.com/), and search for the namespace you think the functionality (class or member) is declared in, appended by ".dll". Generally, use the one with the biggest file size or declared in an important-looking folder path. If no results where found, cut off the last part of the namespace and try again until it is hopefully found.
    - In my search for the `System.Deployment.Application.ApplicationDeployment` class, I first checked for `System.Deployment.Application.dll`, then for `System.Deployment.dll` and chose the version in `C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.1\` because it looked important.
