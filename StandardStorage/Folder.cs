using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace StandardStorage
{
    /// <summary>
    /// Represents a folder in the <see cref="FileSystem"/>.
    /// </summary>
    public class Folder : IFolder
    {
        /// <summary>
        /// Creates a new <see cref="Folder"/> corresponding to a specified path.
        /// </summary>
        /// <param name="path">the folder path</param>
        /// <param name="canDelete">specifies whether or not the folder can be deleted</param>
        public Folder(string path, bool canDelete) : this(path) => CanDelete = canDelete;

        /// <summary>
        /// Creates a new <see cref="Folder" /> corresponding to a specified path.
        /// <see cref="CanDelete"/> will be set to false by default.
        /// </summary>
        /// <param name="path">The folder path</param>
        public Folder(string path) => Name = System.IO.Path.GetFileName(Path = Directory.CreateDirectory(System.IO.Path.GetFullPath(path)).FullName);

        /// <summary>
        /// Declares whether or not the folder can be deleted.
        /// </summary>
        public bool CanDelete { get; } = false;

        /// <summary>
        /// The name of the folder
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The "full path" of the folder, which should uniquely identify it within a given <see cref="IFileSystem"/>
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Checks whether a folder or file exists at the given location.
        /// </summary>
        /// <param name="name">The name of the file or folder to check for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task whose result is the result of the existence check.
        /// </returns>
        public async Task<ExistenceCheckResult> CheckExistsAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(name, nameof (name));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            string pathToCheck = System.IO.Path.Combine(Path, name);
            if (System.IO.File.Exists(pathToCheck)) return ExistenceCheckResult.FileExists;
            else if (Directory.Exists(pathToCheck)) return ExistenceCheckResult.FolderExists;
            return ExistenceCheckResult.NotFound;
        }

        /// <summary>
        /// Creates a file in this folder
        /// </summary>
        /// <param name="desiredName">The name of the file to create</param>
        /// <param name="option">Specifies how to behave if the specified file already exists</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="failIfFolderDoesNotExist">Whether or not an <see cref="Exceptions.DirectoryNotFoundException"/> should be thrown if the folder path specified by <see cref="Path"/> does not exist.</param>
        /// <returns>The newly created file</returns>
        public async Task<IFile> CreateFileAsync(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken = default(CancellationToken)/*, bool failIfFolderDoesNotExist = true*/)
        {
            Ensure.NotNullOrEmpty(desiredName, nameof(desiredName));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            await EnsureExistanceAsync();

            string newPath = System.IO.Path.Combine(Path, desiredName);
            if (System.IO.File.Exists(newPath))
            {
                switch (option)
                {
                    case CreationCollisionOption.GenerateUniqueName:
                        for (int num = 2; System.IO.File.Exists(newPath); newPath = System.IO.Path.Combine(Path, System.IO.Path.GetFileNameWithoutExtension(desiredName) + " (" + num++ + ")" + System.IO.Path.GetExtension(desiredName))) cancellationToken.ThrowIfCancellationRequested();
                        break;
                    case CreationCollisionOption.ReplaceExisting:
                        System.IO.File.Delete(newPath);
                        break;
                    case CreationCollisionOption.FailIfExists:
                        throw new IOException("File already exists: " + newPath);
                    case CreationCollisionOption.OpenIfExists:
                        goto skipFileCreation;
                }
            }
#pragma warning disable CS0642 // Possible mistaken empty statement
            using (System.IO.File.Create(newPath)) ;
#pragma warning restore CS0642 // Possible mistaken empty statement

            skipFileCreation:
            return new File (newPath);
        }

        /// <summary>
        /// Creates a subfolder in this folder
        /// </summary>
        /// <param name="desiredName">The name of the folder to create</param>
        /// <param name="option">Specifies how to behave if the specified folder already exists</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly created folder</returns>
        public async Task<IFolder> CreateFolderAsync(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(desiredName, nameof(desiredName));

            //await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            await EnsureExistanceAsync(cancellationToken);
            string newPath = System.IO.Path.Combine(Path, desiredName);
            if (Directory.Exists(newPath))
            {
                switch (option)
                {
                    case CreationCollisionOption.GenerateUniqueName:
                        for (int num = 2; Directory.Exists(newPath); newPath = System.IO.Path.Combine(Path, desiredName + " (" + num++ + ")")) cancellationToken.ThrowIfCancellationRequested();
                        break;
                    case CreationCollisionOption.ReplaceExisting:
                        Directory.Delete(newPath, true);
                        break;
                    case CreationCollisionOption.FailIfExists:
                        throw new IOException("Directory already exists: " + newPath);
                    case CreationCollisionOption.OpenIfExists:
                        goto skipDirectoryCreation;
                }
            }
            Directory.CreateDirectory(newPath);

            skipDirectoryCreation:
            return new Folder(newPath, true);
        }

        /// <summary>
        /// Deletes the folder found at the location specified by <see cref="Path"/>.
        /// </summary>
        /// <returns>A task which will complete after the folder is deleted.</returns>
        public async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!CanDelete) throw new IOException("Cannot delete root storage folder.");
            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            await EnsureExistanceAsync();
            Directory.Delete(Path, true);
        }

        /// <summary>
        /// Gets a file in this folder
        /// </summary>
        /// <param name="name">The name of the file to get</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The requested file, or null if it does not exist</returns>
        public async Task<IFile> GetFileAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(name, nameof (name));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);

            string path = System.IO.Path.Combine(Path, name);
            if (!System.IO.File.Exists(path)) throw new FileNotFoundException("File does not exist: " + path);
            return new File(path);
        }

        /// <summary>
        /// Gets a list of the files in this folder.
        /// </summary>
        /// <returns>A list of the files in the folder.</returns>
        public async Task<IList<IFile>> GetFilesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            //await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            await EnsureExistanceAsync();
            return Directory.GetFiles(Path).Select(file => new File(file)).ToList<IFile>().AsReadOnly();
        }

        /// <summary>
        /// Gets a subfolder of the folder at the location specified by <see cref="Path"/>.
        /// </summary>
        /// <param name="name">The name of the folder to find.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The requested folder, or null if it does not exist.</returns>
        public async Task<IFolder> GetFolderAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            string path = System.IO.Path.Combine(Path, name);
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException("Directory does not exist: " + path);
            return new Folder(path, true);
        }

        /// <summary>
        /// Gets a list of subfolders of the folder at the location specified by <see cref="Path"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of subfolders in the folder.</returns>
        public async Task<IList<IFolder>> GetFoldersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            //await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            await EnsureExistanceAsync();
            return Directory.GetDirectories(Path).Select(d => new Folder(d, true)).ToList<IFolder>().AsReadOnly();
        }

        /// <summary>
        /// Ensures that there is a folder at the location specified by <see cref="Path"/>. If one doesn't exist, it will be created.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to use.</param>
        /// <returns>A task which will complete once the folder has been created or found.</returns>
        public async Task EnsureExistanceAsync(CancellationToken cancellationToken = default(CancellationToken), bool failIfFolderDoesNotExist = true, bool runSynchronously = true)
        {
            if (!runSynchronously) await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);
            if (failIfFolderDoesNotExist && !Directory.Exists(Path)) throw new DirectoryNotFoundException($"A folder was not found at the path {Path}.");
            Directory.CreateDirectory(Path);
        }
    }
}
