using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Should rename to UniversalStorage
namespace StandardStorage
{
    // Should rename to AdaptiveFileSystem
    public class FileSystem : IFileSystem
    {
        public static IFileSystem Current { get; } = new FileSystem { };

        public IFolder LocalStorage => new Folder(Directory.CreateDirectory(Path.GetFullPath(StorageUtilities.GetAppSpecificStoragePathFromBasePath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))).FullName);

        public IFolder RoamingStorage => new Folder(Directory.CreateDirectory(Path.GetFullPath(StorageUtilities.GetAppSpecificStoragePathFromBasePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))).FullName);

        public async Task<IFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(path, nameof (path));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            return System.IO.File.Exists(path) ? new File(path) : null;
        }

        public async Task<IFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(path, nameof (path));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            return Directory.Exists(path) ? new Folder(path, true) : null;
        }
    }
}
