using System.Threading;
using System.Threading.Tasks;

namespace StandardStorage
{
    /// <summary>
    /// Specifies what should happen when trying to create a file or folder that already exists.
    /// </summary>
    public enum CreationCollisionOption
    {
        /// <summary>
        /// Creates a new file with a unique name of the form "name (2).txt"
        /// </summary>
        ///
        GenerateUniqueName = 0,

        /// <summary>
        /// Replaces any existing file with a new (empty) one
        /// </summary>
        ReplaceExisting = 1,

        /// <summary>
        /// Throws an exception if the file exists
        /// </summary>
        FailIfExists = 2,

        /// <summary>
        /// Opens the existing file, if any
        /// </summary>
        OpenIfExists
    }

    /// <summary>
    /// Describes the result of a file or folder existence check.
    /// </summary>
    public enum ExistenceCheckResult
    {
        /// <summary>
        /// A file was found at the given path.
        /// </summary>
        FileExists,

        /// <summary>
        /// A folder was found at the given path.
        /// </summary>
        FolderExists,

        /// <summary>
        /// No file system entity was found at the given path.
        /// </summary>
        NotFound
    }

    /// <summary>
    /// Specifies what should happen when trying to create/rename a file or folder to a name that already exists.
    /// </summary>
    public enum NameCollisionOption
    {
        /// <summary>
        /// Automatically generate a unique name by appending a number to the name of
        /// the file or folder.
        /// </summary>
        GenerateUniqueName = 0,

        /// <summary>
        /// Replace the existing file or folder. Your app must have permission to access
        /// the location that contains the existing file or folder.
        /// </summary>
        ReplaceExisting = 1,

        /// <summary>
        /// Return an error if another file or folder exists with the same name and abort
        /// the operation.
        /// </summary>
        FailIfExists = 2
    }

    /// <summary>
    /// Represents a file system.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// A folder representing storage which is local to the current device
        /// </summary>
        IFolder LocalStorage { get; }

        /// <summary>
        /// A folder representing storage which may be synced with other devices for the same user
        /// </summary>
        IFolder RoamingStorage { get; }

        /// <summary>
        /// Gets a file, given its path.  Returns null if the file does not exist.
        /// </summary>
        /// <param name="path">The path to a file, as returned from the <see cref="IFile.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A file for the given path, or null if it does not exist.</returns>
        Task<IFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets a folder, given its path.  Returns null if the folder does not exist.
        /// </summary>
        /// <param name="path">The path to a folder, as returned from the <see cref="IFolder.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A folder for the specified path, or null if it does not exist.</returns>
        Task<IFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken));
    }
}