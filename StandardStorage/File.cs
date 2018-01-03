using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StandardStorage
{
    /// <summary>
    /// Represents a file in the <see cref="FileSystem"/>
    /// </summary>
    public class File : IFile
    {
        /// <summary>
        /// Creates a new <see cref="File"/> corresponding to the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        public File(string path) => Name = System.IO.Path.GetFileName(Path = System.IO.Path.GetFullPath(path));

        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The "full path" of the file, which should uniquely identify it within a given <see cref="IFileSystem"/>.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <returns>A task which will complete after the file is deleted.</returns>
        public async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);

            if (!System.IO.File.Exists(Path)) throw new FileNotFoundException("File does not exist: " + Path);

            System.IO.File.Delete(Path);
        }

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="newPath">The new full path of the file.</param>
        /// <param name="collisionOption">How to deal with collisions with existing files.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task which will complete after the file is moved.</returns>
        public async Task MoveAsync(string newPath, NameCollisionOption collisionOption = NameCollisionOption.ReplaceExisting, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(newPath, nameof(newPath));

            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);

            string newDirectory = System.IO.Path.GetDirectoryName(newPath);
            string newName = System.IO.Path.GetFileName(newPath);

            for (int counter = 1; ; counter++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string candidateName = counter > 1 ? $"{System.IO.Path.GetFileNameWithoutExtension(newName)} ({counter}){System.IO.Path.GetExtension(newName)}" : newName;

                string candidatePath = System.IO.Path.Combine(newDirectory, candidateName);

                if (System.IO.File.Exists(candidatePath))
                {
                    switch (collisionOption)
                    {
                        case NameCollisionOption.FailIfExists:
                            throw new IOException("File already exists.");
                        case NameCollisionOption.GenerateUniqueName:
                            continue; // try again with a new name.
                        case NameCollisionOption.ReplaceExisting:
                            System.IO.File.Delete(candidatePath);
                            break;
                    }
                }

                System.IO.File.Move(Path, Path = candidatePath);
                Name = candidateName;
                return;
            }
        }

        /// <summary>
        /// Opens the file.
        /// </summary>
        /// <param name="fileAccess">Specifies whether the file should be opened in read-only or read/write mode.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Stream"/> which can be used to read from or write to the file.</returns>
        public async Task<Stream> OpenAsync(FileAccess fileAccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            await AsynchronityUtilities.SwitchOffMainThreadAsync(cancellationToken);

            switch (fileAccess)
            {
                case FileAccess.Read:
                    return System.IO.File.OpenRead(Path);
                case FileAccess.ReadAndWrite:
                    return System.IO.File.Open(Path, FileMode.Open, System.IO.FileAccess.ReadWrite);
            }
            throw new ArgumentException("Unrecognized FileAccess value: " + fileAccess);
        }

        /// <summary>
        /// Renames a file without changing its location.
        /// </summary>
        /// <param name="newName">The new leaf name of the file.</param>
        /// <param name="collisionOption">How to deal with collisions with existing files.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task which will complete after the file is renamed.</returns>
        public async Task RenameAsync(string newName, NameCollisionOption collisionOption = NameCollisionOption.FailIfExists, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNullOrEmpty(newName, nameof(newName));
            await MoveAsync(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), newName), collisionOption, cancellationToken);
        }
    }
}
