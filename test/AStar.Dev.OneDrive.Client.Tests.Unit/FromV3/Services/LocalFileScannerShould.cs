//using AStarOneDriveClient.Models.Enums;
//using AStarOneDriveClient.Services;
//using Testably.Abstractions.Testing;

//namespace AStarOneDriveClient.Tests.Unit.Services;

//public class LocalFileScannerShould
//{
//    [Fact]
//    public async Task ScanFolderAndReturnFileMetadata()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder");
//        fileSystem.AddFile(@"C:\SyncFolder\file1.txt", new MockFileData("content1"));
//        fileSystem.AddFile(@"C:\SyncFolder\file2.txt", new MockFileData("content2"));
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive");

//        result.Count.ShouldBe(2);
//        result.ShouldAllBe(f => f.AccountId == "acc1");
//        result.ShouldAllBe(f => f.SyncStatus == FileSyncStatus.PendingUpload);
//        result.ShouldContain(f => f.Name == "file1.txt");
//        result.ShouldContain(f => f.Name == "file2.txt");
//    }

//    [Fact]
//    public async Task ScanNestedFoldersRecursively()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder\SubFolder1");
//        fileSystem.AddDirectory(@"C:\SyncFolder\SubFolder2");
//        fileSystem.AddFile(@"C:\SyncFolder\root.txt", new MockFileData("root"));
//        fileSystem.AddFile(@"C:\SyncFolder\SubFolder1\file1.txt", new MockFileData("file1"));
//        fileSystem.AddFile(@"C:\SyncFolder\SubFolder2\file2.txt", new MockFileData("file2"));
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive");

//        result.Count.ShouldBe(3);
//        result.ShouldContain(f => f.Path == "/OneDrive/root.txt");
//        result.ShouldContain(f => f.Path == "/OneDrive/SubFolder1/file1.txt");
//        result.ShouldContain(f => f.Path == "/OneDrive/SubFolder2/file2.txt");
//    }

//    [Fact]
//    public async Task ComputeHashForEachFile()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder");
//        fileSystem.AddFile(@"C:\SyncFolder\file.txt", new MockFileData("test content"));
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive");

//        result.Count.ShouldBe(1);
//        result[0].LocalHash.ShouldNotBeNullOrEmpty();
//        result[0].LocalHash!.Length.ShouldBe(64); // SHA256 hex string length
//    }

//    [Fact]
//    public async Task SetCorrectFileMetadata()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder");
//        var fileData = new MockFileData("content") { LastWriteTime = new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc) };
//        fileSystem.AddFile(@"C:\SyncFolder\test.txt", fileData);
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive");

//        result.Count.ShouldBe(1);
//        var file = result[0];
//        file.Name.ShouldBe("test.txt");
//        file.Size.ShouldBe(7); // "content" length
//        file.LocalPath.ShouldBe(@"C:\SyncFolder\test.txt");
//        file.Path.ShouldBe("/OneDrive/test.txt");
//        file.LastModifiedUtc.ShouldBe(new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc));
//    }

//    [Fact]
//    public async Task ReturnEmptyListForNonexistentFolder()
//    {
//        var fileSystem = new MockFileSystem();
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\DoesNotExist", "/OneDrive");

//        result.ShouldBeEmpty();
//    }

//    [Fact]
//    public async Task ReturnEmptyListForEmptyFolder()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\EmptyFolder");
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\EmptyFolder", "/OneDrive");

//        result.ShouldBeEmpty();
//    }

//    [Fact]
//    public async Task HandleCancellation()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder");
//        fileSystem.AddFile(@"C:\SyncFolder\file.txt", new MockFileData("content"));
//        var scanner = new LocalFileScanner(fileSystem);
//        using var cts = new CancellationTokenSource();
//        await cts.CancelAsync();

//        await Should.ThrowAsync<OperationCanceledException>(async () =>
//            await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive", cts.Token));
//    }

//    [Fact]
//    public async Task SetInitialSyncStatusAsPendingUpload()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder");
//        fileSystem.AddFile(@"C:\SyncFolder\file.txt", new MockFileData("content"));
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive");

//        result.Count.ShouldBe(1);
//        result[0].SyncStatus.ShouldBe(FileSyncStatus.PendingUpload);
//        result[0].Id.ShouldBe(string.Empty); // Will be populated after upload
//        result[0].CTag.ShouldBeNull();
//        result[0].ETag.ShouldBeNull();
//        result[0].LastSyncDirection.ShouldBeNull();
//    }

//    [Fact]
//    public async Task GenerateConsistentHashesForSameContent()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder");
//        fileSystem.AddFile(@"C:\SyncFolder\file1.txt", new MockFileData("same content"));
//        fileSystem.AddFile(@"C:\SyncFolder\file2.txt", new MockFileData("same content"));
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive");

//        result.Count.ShouldBe(2);
//        result[0].LocalHash.ShouldBe(result[1].LocalHash);
//    }

//    [Fact]
//    public async Task GenerateDifferentHashesForDifferentContent()
//    {
//        var fileSystem = new MockFileSystem();
//        fileSystem.AddDirectory(@"C:\SyncFolder");
//        fileSystem.AddFile(@"C:\SyncFolder\file1.txt", new MockFileData("content1"));
//        fileSystem.AddFile(@"C:\SyncFolder\file2.txt", new MockFileData("content2"));
//        var scanner = new LocalFileScanner(fileSystem);

//        var result = await scanner.ScanFolderAsync("acc1", @"C:\SyncFolder", "/OneDrive");

//        result.Count.ShouldBe(2);
//        result[0].LocalHash.ShouldNotBe(result[1].LocalHash);
//    }
//}
