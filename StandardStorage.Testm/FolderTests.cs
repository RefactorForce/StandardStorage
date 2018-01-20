﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StandardStorage.Test
{
    [TestClass]
    public class FolderTests
    {
        IFileSystem TestFileSystem { get { return FileSystem.Current; } }

        [TestMethod]
        public async Task GetFile()
        {
            //  Arrange
            IFolder folder = TestFileSystem.LocalStorage;
            string fileName = "fileToGet.txt";
            IFile createdFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);

            //  Act
            IFile gottenFile = await folder.GetFileAsync(fileName);

            //  Assert
            Assert.AreEqual(fileName, gottenFile.Name);
            Assert.AreEqual(Path.Combine(folder.Path, fileName), gottenFile.Path);

            //  Cleanup
            await createdFile.DeleteAsync();
        }

        [TestMethod]
        public void GetFile_Null()
        {
            Task result = TestFileSystem.LocalStorage.GetFileAsync(null);
            Assert.IsTrue(result.IsFaulted);
            Assert.AreEqual(typeof(ArgumentNullException), result.Exception.InnerException.GetType());
        }

        [TestMethod]
        public async Task GetFileThatDoesNotExist()
        {
            //  Arrange
            IFolder folder = TestFileSystem.LocalStorage;
            string fileName = "fileThatDoesNotExist.txt";

            //  Act & Assert
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await folder.GetFileAsync(fileName));
        }

        [TestMethod]
        public async Task GetFilesEmpty()
        {
            //  Arrange
            IFolder folder = await TestFileSystem.LocalStorage.CreateFolderAsync("GetFilesEmpty_Folder", CreationCollisionOption.FailIfExists);

            //  Act
            IList<IFile> files = await folder.GetFilesAsync();

            //  Assert
            Assert.AreEqual(0, files.Count, "File count");

            //  Cleanup
            await folder.DeleteAsync();
        }

        [TestMethod]
        public async Task GetFilesSingle()
        {
            //  Arrange
            IFolder folder = await TestFileSystem.LocalStorage.CreateFolderAsync("GetFilesSingle_Folder", CreationCollisionOption.FailIfExists);
            await folder.CreateFileAsync("file.txt", CreationCollisionOption.FailIfExists);

            //  Act
            IList<IFile> files = await folder.GetFilesAsync();

            //  Assert
            Assert.AreEqual(1, files.Count, "File count");
            Assert.AreEqual("file.txt", files[0].Name);

            //  Cleanup
            await folder.DeleteAsync();
        }

        [TestMethod]
        public async Task GetFilesMultiple()
        {
            //  Arrange
            IFolder folder = await TestFileSystem.LocalStorage.CreateFolderAsync("GetFilesMultiple_Folder", CreationCollisionOption.FailIfExists);
            var fileNames = new[] { "hello.txt", "file.zzz", "anotherone", "42" };
            foreach (var fn in fileNames)
            {
                await folder.CreateFileAsync(fn, CreationCollisionOption.FailIfExists);
            }

            //  Act
            IList<IFile> files = await folder.GetFilesAsync();

            //  Assert
            Assert.AreEqual(fileNames.Length, files.Count, "File count");
            foreach (var fn in fileNames)
            {
                Assert.IsTrue(files.Count(f => f.Name == fn) == 1, "File " + fn + " in results");
            }

            //  Cleanup
            await folder.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFolder()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string folderName = "FolderToCreate";

            //  Act
            IFolder folder = await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.FailIfExists);

            //  Assert
            Assert.AreEqual(folderName, folder.Name);
            Assert.AreEqual(Path.Combine(rootFolder.Path, folderName), folder.Path, "Folder path");

            //  Cleanup
            await folder.DeleteAsync();
        }

        [TestMethod]
        public void CreateFolder_Null()
        {
            Task result = TestFileSystem.LocalStorage.CreateFolderAsync(null, CreationCollisionOption.FailIfExists);
            Assert.IsTrue(result.IsFaulted);
            Assert.AreEqual(typeof(ArgumentNullException), result.Exception.InnerException.GetType());
        }

        [TestMethod]
        public async Task CreateNestedFolder()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string subFolderName = "NestedSubFolder";
            IFolder subFolder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);
            string leafFolderName = "NestedLeafFolder";

            //  Act
            IFolder testFolder = await subFolder.CreateFolderAsync(leafFolderName, CreationCollisionOption.FailIfExists);

            //  Assert
            Assert.AreEqual(leafFolderName, testFolder.Name);
            Assert.AreEqual(Path.Combine(rootFolder.Path, subFolderName, leafFolderName), testFolder.Path, "Leaf folder path");

            //  Cleanup
            await subFolder.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFolderCollision_GenerateUniqueName()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string subFolderName = "Collision_Unique";
            IFolder existingFolder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);

            //  Act
            IFolder folder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.GenerateUniqueName);

            //  Assert
            Assert.AreEqual(subFolderName + " (2)", folder.Name);

            //  Cleanup
            await existingFolder.DeleteAsync();
            await folder.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFolderCollision_ReplaceExisting()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string subFolderName = "Collision_Replace";
            IFolder existingFolder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);
            await existingFolder.CreateFileAsync("FileInFolder.txt", CreationCollisionOption.FailIfExists);

            //  Act
            IFolder newFolder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.ReplaceExisting);

            //  Assert
            Assert.AreEqual(subFolderName, newFolder.Name);
            var files = await newFolder.GetFilesAsync();
            Assert.AreEqual(0, files.Count, "New folder file count");

            //  Cleanup
            await newFolder.DeleteAsync();
        }

#if NETFX_CORE
        [TestMethod]
        public async Task ConcurrentGetFolderFromPath()
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var task = Task.Run(async () =>
                    {
                        string localFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                        //await Task.Yield();
                        var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(localFolderPath);
                    });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
#endif

        [TestMethod]
        public async Task ConcurrentCreateFolder()
        {
            //  Arrange
            var folderName = "json";
            var items = new List<Task<IFolder>>();
            int iterations = 10;

            for (var i = 0; i < iterations; i++)
            {
                var task = Task.Run(async () =>
                    {
                        var folder = await FileSystem.Current.LocalStorage.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
                        return folder;
                    });
                items.Add(task);
            }

            //  Act
            List<IFolder> folders = (await Task.WhenAll(items)).ToList();

            //  Assert
            Assert.AreEqual(iterations, folders.Count, "Folder count");
            for (int i = 0; i < iterations; i++)
            {
                Assert.AreEqual(folderName, folders[i].Name, "Folder " + i + " name");
            }

        }

        [TestMethod]
        public async Task CreateFolderCollision_FailIfExists()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string subFolderName = "Collision_Fail";
            IFolder existingFolder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);

            //  Act & Assert
            await Assert.ThrowsExceptionAsync<IOException>(async () =>
                {
                    await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);
                });

            //  Cleanup
            await existingFolder.DeleteAsync();
        }

        [TestMethod]
        public async Task CreateFolderCollision_OpenIfExists()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string subFolderName = "Collision_Open";
            IFolder existingFolder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);
            await existingFolder.CreateFileAsync("FileInFolder.txt", CreationCollisionOption.FailIfExists);

            //  Act
            IFolder newFolder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.OpenIfExists);

            //  Assert
            Assert.AreEqual(subFolderName, newFolder.Name);
            var files = await newFolder.GetFilesAsync();
            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("FileInFolder.txt", files[0].Name);

            //  Cleanup
            await newFolder.DeleteAsync();
        }

        [TestMethod]
        public async Task GetFolder()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string folderName = "FolderToGet";
            IFolder createdFolder = await rootFolder.CreateFolderAsync(folderName, CreationCollisionOption.FailIfExists);

            //  Act
            IFolder gottenFolder = await rootFolder.GetFolderAsync(folderName);

            //  Assert
            Assert.AreEqual(folderName, gottenFolder.Name);
            Assert.AreEqual(Path.Combine(rootFolder.Path, folderName), gottenFolder.Path);

            //  Cleanup
            await gottenFolder.DeleteAsync();
        }

        [TestMethod]
        public void GetFolder_Null()
        {
            Task result = TestFileSystem.LocalStorage.GetFolderAsync(null);
            Assert.IsTrue(result.IsFaulted);
            Assert.AreEqual(typeof(ArgumentNullException), result.Exception.InnerException.GetType());
        }

        [TestMethod]
        public async Task GetFolderThatDoesNotExist()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string folderName = "FolderThatDoesNotExist";

            //  Act & Assert
            await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(
                async () => await rootFolder.GetFolderAsync(folderName));
        }

        [TestMethod]
        public async Task GetFoldersEmpty()
        {
            //  Arrange
            IFolder folder = await TestFileSystem.LocalStorage.CreateFolderAsync("GetFoldersEmpty_Folder", CreationCollisionOption.FailIfExists);

            //  Act
            IList<IFolder> folders = await folder.GetFoldersAsync();

            //  Assert
            Assert.AreEqual(0, folders.Count, "Folder count");

            //  Cleanup
            await folder.DeleteAsync();
        }

        [TestMethod]
        public async Task GetFoldersSingle()
        {
            //  Arrange
            IFolder folder = await TestFileSystem.LocalStorage.CreateFolderAsync("GetFoldersSingle_Folder", CreationCollisionOption.FailIfExists);
            string expectedFolderName = "Subfolder";
            await folder.CreateFolderAsync(expectedFolderName, CreationCollisionOption.FailIfExists);

            //  Act
            IList<IFolder> folders = await folder.GetFoldersAsync();

            //  Assert
            Assert.AreEqual(1, folders.Count, "Folder count");
            Assert.AreEqual(expectedFolderName, folders[0].Name);

            //  Cleanup
            await folder.DeleteAsync();
        }

        [TestMethod]
        public async Task GetFoldersMultiple()
        {
            //  Arrange
            IFolder folder = await TestFileSystem.LocalStorage.CreateFolderAsync("GetFoldersMultiple_Folder", CreationCollisionOption.FailIfExists);
            var folderNames = new[] { "One", "2", "Hello" };
            foreach (var fn in folderNames)
            {
                await folder.CreateFolderAsync(fn, CreationCollisionOption.FailIfExists);
            }

            //  Act
            IList<IFolder> folders = await folder.GetFoldersAsync();

            //  Assert
            Assert.AreEqual(folderNames.Length, folders.Count, "Folder count");
            foreach (var fn in folderNames)
            {
                Assert.IsTrue(folders.Count(f => f.Name == fn) == 1, "Folder " + fn + " in results");
            }

            //  Cleanup
            await folder.DeleteAsync();
        }

        [TestMethod]
        public async Task DeleteFolder()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string subFolderName = "FolderToDelete";
            IFolder folder = await rootFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.FailIfExists);

            //  Act
            await folder.DeleteAsync();

            //  Assert
            var folders = await rootFolder.GetFoldersAsync();
            Assert.IsFalse(folders.Any(f => f.Name == subFolderName));
        }

        [TestMethod]
        public async Task DeleteNonEmptyFolder()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            string folderToDeleteName = "FolderToDelete";
            IFolder folder = await rootFolder.CreateFolderAsync(folderToDeleteName, CreationCollisionOption.FailIfExists);
            IFile fileInFolder = await folder.CreateFileAsync("file.txt", CreationCollisionOption.FailIfExists);
            await fileInFolder.WriteAllTextAsync("hello, world");
            IFolder subfolder = await folder.CreateFolderAsync("subfolder", CreationCollisionOption.FailIfExists);

            //  Act
            await folder.DeleteAsync();

            //  Assert
            var folders = await rootFolder.GetFoldersAsync();
            Assert.IsFalse(folders.Any(f => f.Name == folderToDeleteName));
            Assert.IsNull(await TestFileSystem.GetFileFromPathAsync(fileInFolder.Path));
            Assert.IsNull(await TestFileSystem.GetFolderFromPathAsync(subfolder.Path));
        }

        [TestMethod]
        public async Task CreateFileInDeletedFolder()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            IFolder folder = await rootFolder.CreateFolderAsync("FolderToDeleteAndThenCreateFileIn", CreationCollisionOption.FailIfExists);
            await folder.DeleteAsync();

            //  Act & Assert
            await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(async () => await folder.CreateFileAsync("Foo.txt", CreationCollisionOption.GenerateUniqueName));
        }

        [TestMethod]
        public void CreateFile_Null()
        {
            Task result = TestFileSystem.LocalStorage.CreateFileAsync(null, CreationCollisionOption.FailIfExists);
            Assert.IsTrue(result.IsFaulted);
            Assert.AreEqual(typeof(ArgumentNullException), result.Exception.InnerException.GetType());
        }

        [TestMethod]
        public async Task CheckExists()
        {
            // setup
            var file = await TestFileSystem.LocalStorage.CreateFileAsync("somefile", CreationCollisionOption.OpenIfExists);
            var folder = await TestFileSystem.LocalStorage.CreateFolderAsync("somefolder", CreationCollisionOption.OpenIfExists);

            // assertions
            Assert.AreEqual(ExistenceCheckResult.NotFound, await TestFileSystem.LocalStorage.CheckExistsAsync("no-file-here"));
            Assert.AreEqual(ExistenceCheckResult.FolderExists, await TestFileSystem.LocalStorage.CheckExistsAsync("somefolder"));
            Assert.AreEqual(ExistenceCheckResult.FileExists, await TestFileSystem.LocalStorage.CheckExistsAsync("somefile"));

            // clean up
            await file.DeleteAsync();
            await folder.DeleteAsync();
        }

        [TestMethod]
        public void CheckExists_Null()
        {
            Task result = TestFileSystem.LocalStorage.CheckExistsAsync(null);
            Assert.IsTrue(result.IsFaulted);
            Assert.AreEqual(typeof(ArgumentNullException), result.Exception.InnerException.GetType());
        }

        [TestMethod]
        public async Task DeleteFolderTwice()
        {
            //  Arrange
            IFolder rootFolder = TestFileSystem.LocalStorage;
            IFolder folder = await rootFolder.CreateFolderAsync("FolderToDeleteTwice", CreationCollisionOption.FailIfExists);
            await folder.DeleteAsync();

            //  Act & Asserth
            await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(async () => await folder.DeleteAsync());
        }

        [TestMethod]
        public async Task DeleteAppLocalStorageThrows()
        {
            //  Arrange
            IFolder folder = TestFileSystem.LocalStorage;

            //  Act & Assert
            await Assert.ThrowsExceptionAsync<IOException>(async () => await folder.DeleteAsync());
        }
    }
}
