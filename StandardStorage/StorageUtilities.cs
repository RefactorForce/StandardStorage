using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace StandardStorage
{
    /// <summary>
    /// A collection of useful storage utilities for finding storage paths.
    /// </summary>
    public static class StorageUtilities
    {
        /// <summary>
        /// Creates and returns the full path to an app-specific local user storage folder.
        /// </summary>
        public static string LocalUserAppDataPath
        {
            get
            {
                try
                {
                    if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                    {
                        if (AppDomain.CurrentDomain.GetData("DataDirectory") is string data)
                        {
                            return data;
                        }
                    }
                }
                catch
                {
                }
                return GetAppSpecificStoragePathFromBasePath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            }
        }

        /// <summary>
        /// Creates and returns the full path to an app-specific storage folder based on <paramref name="basePath"/>.
        /// </summary>
        /// <param name="basePath"></param>
        /// <returns>returns a full path to the created storage folder.</returns>
        public static string GetAppSpecificStoragePathFromBasePath(string basePath) => Directory.CreateDirectory(Path.Combine(basePath, CompanyName, ProductName, ProductVersion)).FullName;

        static string productVersion;

        /// <summary>
        /// Gets the product version of the current top-level assembly using this library.
        /// </summary>
        public static string ProductVersion
        {
            get
            {
                if (productVersion == null)
                {

                    // custom attribute
                    //
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null)
                    {
                        object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                        if (attrs != null && attrs.Length > 0)
                        {
                            productVersion = ((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion;
                        }
                    }

                    // win32 version info
                    //
                    if (productVersion == null || productVersion.Length == 0)
                    {
                        productVersion = GetAppFileVersionInfo().ProductVersion;
                        if (productVersion != null)
                        {
                            productVersion = productVersion.Trim();
                        }
                    }

                    // fake it
                    //
                    if (productVersion == null || productVersion.Length == 0)
                    {
                        productVersion = "1.0.0.0";
                    }
                }
                return productVersion;
            }
        }

        static string productName;

        /// <summary>
        /// Gets the prodcut name of the current top-level assembly using this library.
        /// </summary>
        public static string ProductName
        {
            get
            {
                if (productName == null)
                {

                    // custom attribute
                    //
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null)
                    {
                        object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                        if (attrs != null && attrs.Length > 0)
                        {
                            productName = ((AssemblyProductAttribute)attrs[0]).Product;
                        }
                    }

                    // win32 version info
                    //
                    if (productName == null || productName.Length == 0)
                    {
                        productName = GetAppFileVersionInfo().ProductName;
                        if (productName != null)
                        {
                            productName = productName.Trim();
                        }
                    }

                    // fake it with namespace
                    // won't work with MC++ see GetAppMainType.
                    if (productName == null || productName.Length == 0)
                    {
                        Type t = GetAppMainType();

                        if (t != null)
                        {
                            string ns = t.Namespace;

                            if (!string.IsNullOrEmpty(ns))
                            {
                                int lastDot = ns.LastIndexOf(".");
                                if (lastDot != -1 && lastDot < ns.Length - 1)
                                {
                                    productName = ns.Substring(lastDot + 1);
                                }
                                else
                                {
                                    productName = ns;
                                }
                            }
                            else
                            {
                                // last ditch... use the main type
                                //
                                productName = t.Name;
                            }
                        }
                    }
                }

                return productName;
            }
        }

        static string companyName;

        /// <summary>
        /// Gets the company name of the current top-level assembly using this library.
        /// </summary>
        public static string CompanyName
        {
            get
            {
                if (companyName == null)
                {

                    // custom attribute
                    //
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null)
                    {
                        object[] attrs = entryAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                        if (attrs != null && attrs.Length > 0)
                        {
                            companyName = ((AssemblyCompanyAttribute)attrs[0]).Company;
                        }
                    }

                    // win32 version
                    //
                    if (companyName == null || companyName.Length == 0)
                    {
                        companyName = GetAppFileVersionInfo().CompanyName;
                        if (companyName != null)
                        {
                            companyName = companyName.Trim();
                        }
                    }

                    // fake it with a namespace
                    // won't work with MC++ see GetAppMainType.
                    if (companyName == null || companyName.Length == 0)
                    {
                        Type t = GetAppMainType();

                        if (t != null)
                        {
                            string ns = t.Namespace;

                            if (!string.IsNullOrEmpty(ns))
                            {
                                int firstDot = ns.IndexOf(".");
                                if (firstDot != -1)
                                {
                                    companyName = ns.Substring(0, firstDot);
                                }
                                else
                                {
                                    companyName = ns;
                                }
                            }
                            else
                            {
                                // last ditch... no namespace, use product name...
                                //
                                companyName = ProductName;
                            }
                        }
                    }
                }
                return companyName;
            }
        }

        static FileVersionInfo appFileVersion;

        static FileVersionInfo GetAppFileVersionInfo()
        {
            if (appFileVersion == null)
            {
                Type t = GetAppMainType();
                if (t != null)
                {
                    // SECREVIEW : This Assert is ok, getting the module's version is a safe operation, 
                    //             the result is provided by the system.
                    //
                    new FileIOPermission(PermissionState.None) { AllFiles = FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read }.Assert();

                    try
                    {
                        appFileVersion = FileVersionInfo.GetVersionInfo(t.Module.FullyQualifiedName);
                    }
                    finally
                    {
                        CodeAccessPermission.RevertAssert();
                    }
                }
                else
                {
                    appFileVersion = FileVersionInfo.GetVersionInfo(ExecutablePath);
                }
            }

            return appFileVersion;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetModuleFileName(HandleRef hModule, StringBuilder buffer, int length);

        /// <summary>
        /// Gets the long path to the module pointed to by <paramref name="hModule"/>.
        /// </summary>
        /// <param name="hModule">A handle to the module that the long path is needed for.</param>
        /// <returns>The long path to the module pointed to by <paramref name="hModule"/></returns>
        public static StringBuilder GetModuleFileNameLongPath(HandleRef hModule)
        {
            StringBuilder buffer = new StringBuilder(260);
            int noOfTimes = 1;
            int length = 0;
            // Iterating by allocating chunk of memory each time we find the length is not sufficient.
            // Performance should not be an issue for current MAX_PATH length due to this change.
            while (((length = GetModuleFileName(hModule, buffer, buffer.Capacity)) == buffer.Capacity)
                && Marshal.GetLastWin32Error() == 122
                && buffer.Capacity < short.MaxValue)
            {
                noOfTimes += 2; // Increasing buffer size by 520 in each iteration.
                int capacity = noOfTimes * 260 < short.MaxValue ? noOfTimes * 260 : short.MaxValue;
                buffer.EnsureCapacity(capacity);
            }
            buffer.Length = length;
            return buffer;
        }

        internal static string UnsafeGetFullPath(string fileName)
        {
            string full = fileName;

            FileIOPermission fiop = new FileIOPermission(PermissionState.None) { AllFiles = FileIOPermissionAccess.PathDiscovery };
            fiop.Assert();
            try
            {
                full = Path.GetFullPath(fileName);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
            return full;
        }

        static string executablePath;

        /// <summary>
        /// A full path to the current top-level executable assembly using this library.
        /// </summary>
        public static string ExecutablePath
        {
            get
            {
                if (executablePath == null)
                {
                    Assembly asm = Assembly.GetEntryAssembly();
                    if (asm == null)
                    {
                        StringBuilder sb = GetModuleFileNameLongPath(new HandleRef(null, IntPtr.Zero));
                        executablePath = UnsafeGetFullPath(sb.ToString());
                    }
                    else
                    {
                        String cb = asm.CodeBase;
                        Uri codeBase = new Uri(cb);
                        if (codeBase.IsFile)
                        {
                            executablePath = codeBase.LocalPath + Uri.UnescapeDataString(codeBase.Fragment); ;
                        }
                        else
                        {
                            executablePath = codeBase.ToString();
                        }
                    }
                }
                Uri exeUri = new Uri(executablePath);
                if (exeUri.Scheme == "file")
                {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, executablePath).Demand();
                }
                return executablePath;
            }
        }

        static Type mainType;

        // Get Main type...This doesn't work in MC++ because Main is a global function and not
        // a class static method (it doesn't belong to a Type).
        private static Type GetAppMainType() => mainType ?? (mainType = Assembly.GetEntryAssembly()?.EntryPoint.ReflectedType);
    }
}
