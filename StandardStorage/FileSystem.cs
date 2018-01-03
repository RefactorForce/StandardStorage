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
    /// <summary>
    /// Represents a file system.
    /// </summary>
    public class FileSystem : IFileSystem
    {
        /// <summary>
        /// The implementation of <see cref="IFileSystem"/> for the current platform
        /// </summary>
        public static IFileSystem Current { get; } = new FileSystem { };

        /// <summary>
        /// A folder representing storage which is local to the current device
        /// </summary>
        public IFolder LocalStorage => new Folder(Directory.CreateDirectory(Path.GetFullPath(StorageUtilities.GetAppSpecificStoragePathFromBasePath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))).FullName);

        /// <summary>
        /// A folder representing storage which may be synced with other devices for the same user
        /// </summary>
        public IFolder RoamingStorage => new Folder(Directory.CreateDirectory(Path.GetFullPath(StorageUtilities.GetAppSpecificStoragePathFromBasePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))).FullName);

        /// <summary>
        /// Gets a file, given its path.  Returns null if the file does not exist.
        /// </summary>
        /// <param name="path">The path to a file, as returned from the <see cref="IFile.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A file for the given path, or null if it does not exist.</returns>
        public async Task<IFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(path, nameof (path));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            return System.IO.File.Exists(path) ? new File(path) : null;
        }

        /// <summary>
        /// Gets a folder, given its path.  Returns null if the folder does not exist.
        /// </summary>
        /// <param name="path">The path to a folder, as returned from the <see cref="IFolder.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A folder for the specified path, or null if it does not exist.</returns>
        public async Task<IFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(path, nameof (path));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            return Directory.Exists(path) ? new Folder(path, true) : null;
        }
    }
}
