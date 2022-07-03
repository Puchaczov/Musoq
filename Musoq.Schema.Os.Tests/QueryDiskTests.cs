using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema.Os.Compare.Directories;
using Musoq.Schema.Os.Directories;
using Musoq.Schema.Os.Dlls;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Tests.Utils;
using Musoq.Tests.Common;
using Environment = Musoq.Plugins.Environment;

namespace Musoq.Schema.Os.Tests
{
    [TestClass]
    public class QueryDiskTests
    {
        [TestInitialize]
        public void Initialize()
        {
            if (!Directory.Exists("./Results"))
                Directory.CreateDirectory("./Results");
        }

        [TestMethod]
        public void CompressFilesTest()
        {
            var query =
                $"select Compress(AggregateFiles(), './Results/{nameof(CompressFilesTest)}.zip', 'fastest') from #disk.files('./Files', false)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual($"Compress(AggregateFiles(), './Results/{nameof(CompressFilesTest)}.zip', 'fastest')",
                table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual($"./Results/{nameof(CompressFilesTest)}.zip", table[0].Values[0]);

            File.Delete($"./Results/{nameof(CompressFilesTest)}.zip");
        }

        [TestMethod]
        public void CompressDirectoriesTest()
        {
            var resultName = $"./Results/{nameof(CompressDirectoriesTest)}.zip";

            var query =
                $"select Compress(AggregateDirectories(), '{resultName}', 'fastest') from #disk.directories('./Directories', false)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual($"Compress(AggregateDirectories(), '{resultName}', 'fastest')",
                table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(resultName, table[0][0]);

            using (var zipFile = File.OpenRead(resultName))
            {
                using (var zipArchive = new ZipArchive(zipFile))
                {
                    Assert.IsTrue(zipArchive.Entries.Any(f => f.FullName == "Directory1\\TextFile1.txt"));
                    Assert.IsTrue(zipArchive.Entries.Any(f => f.FullName == "Directory2\\TextFile2.txt"));
                    Assert.IsTrue(zipArchive.Entries.Any(f => f.FullName == "Directory2\\Directory3\\TextFile3.txt"));
                    Assert.AreEqual(3, zipArchive.Entries.Count);
                }
            }

            Assert.IsTrue(File.Exists(resultName));

            File.Delete(resultName);
        }

        [TestMethod]
        public void ComplexObjectPropertyTest()
        {
            var query = "select Parent.Name from #disk.directories('./Directories', false)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Parent.Name", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Directories", table[0].Values[0]);
            Assert.AreEqual("Directories", table[1].Values[0]);
        }

        [TestMethod]
        public void DescFilesTest()
        {
            var query = "desc #os.files('./','false')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(3, table.Columns.Count());

            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.Name) && (string)row[2] == typeof(string).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.CreationTime) && (string)row[2] == typeof(DateTime).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.CreationTimeUtc) && (string)row[2] == typeof(DateTime).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.DirectoryName) && (string)row[2] == typeof(string).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.Extension) && (string)row[2] == typeof(string).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.FullName) && (string)row[2] == typeof(string).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.Exists) && (string)row[2] == typeof(bool).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.IsReadOnly) && (string)row[2] == typeof(bool).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(FileInfo.Length) && (string)row[2] == typeof(long).FullName));
        }

        [TestMethod]
        public void DescDllsTest()
        {
            var query = "desc #os.dlls('./','false')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(3, table.Columns.Count());

            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(DllInfo.FileInfo) && (string)row[2] == typeof(FileInfo).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(DllInfo.Assembly) && (string)row[2] == typeof(Assembly).FullName));
            Assert.IsTrue(table.Any(row => (string)row[0] == nameof(DllInfo.Version) && (string)row[2] == typeof(FileVersionInfo).FullName));
        }

        [TestMethod]
        public void FilesSourceIterateDirectoriesTest()
        {
            var source = new TestFilesSource("./Directories", false, RuntimeContext.Empty);

            var folders = source.GetFiles();

            Assert.AreEqual(1, folders.Count);

            Assert.AreEqual("TestFile1.txt", ((ExtendedFileInfo)folders[0].Contexts[0]).Name);
        }

        [TestMethod]
        public void FilesSourceIterateWithNestedDirectoriesTest()
        {
            var source = new TestFilesSource("./Directories", true, RuntimeContext.Empty);

            var folders = source.GetFiles();

            Assert.AreEqual(4, folders.Count);

            Assert.AreEqual("TestFile1.txt", ((ExtendedFileInfo)folders[0].Contexts[0]).Name);
            Assert.AreEqual("TextFile2.txt", ((ExtendedFileInfo)folders[1].Contexts[0]).Name);
            Assert.AreEqual("TextFile3.txt", ((ExtendedFileInfo)folders[2].Contexts[0]).Name);
            Assert.AreEqual("TextFile1.txt", ((ExtendedFileInfo)folders[3].Contexts[0]).Name);
        }

        [TestMethod]
        public void DirectoriesSourceIterateDirectoriesTest()
        {
            var source = new TestDirectoriesSource("./Directories", false, RuntimeContext.Empty);

            var directories = source.GetDirectories();

            Assert.AreEqual(2, directories.Count);

            Assert.AreEqual("Directory1", ((DirectoryInfo)directories[0].Contexts[0]).Name);
            Assert.AreEqual("Directory2", ((DirectoryInfo)directories[1].Contexts[0]).Name);
        }

        [TestMethod]
        public void TestDirectoriesSourceIterateWithNestedDirectories()
        {
            var source = new TestDirectoriesSource("./Directories", true, RuntimeContext.Empty);

            var directories = source.GetDirectories();

            Assert.AreEqual(3, directories.Count);

            Assert.AreEqual("Directory1", ((DirectoryInfo) directories[0].Contexts[0]).Name);
            Assert.AreEqual("Directory2", ((DirectoryInfo) directories[1].Contexts[0]).Name);
            Assert.AreEqual("Directory3", ((DirectoryInfo) directories[2].Contexts[0]).Name);
        }

        [TestMethod]
        public void NonExistingDirectoryTest()
        {
            var source = new TestDirectoriesSource("./Some/Non/Existing/Path", true, RuntimeContext.Empty);

            var directories = source.GetDirectories();

            Assert.AreEqual(0, directories.Count);
        }

        [TestMethod]
        public void NonExisitngFileTest()
        {
            var source = new TestFilesSource("./Some/Non/Existing/Path.pdf", true, RuntimeContext.Empty);

            var directories = source.GetFiles();

            Assert.AreEqual(0, directories.Count);
        }

        [TestMethod]
        public void DirectoriesSource_CancelledLoadTest()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                tokenSource.Cancel();
                var source = new DirectoriesSource("./Directories", true, new RuntimeContext(tokenSource.Token, new ISchemaColumn[0]));

                var fired = source.Rows.Count();

                Assert.AreEqual(0, fired);
            }
        }

        [TestMethod]
        public void DirectoriesSource_FullLoadTest()
        {
            var source = new DirectoriesSource("./Directories", true, RuntimeContext.Empty);

            var fired = source.Rows.Count();

            Assert.AreEqual(3, fired);
        }

        [TestMethod]
        public void File_GetFirstByte_Test()
        {
            var query = "select ToHex(GetFileBytes(2), '|') from #disk.files('./Files', false) where Name = 'File1.txt'";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);

            Assert.AreEqual("EF|BB", table[0][0]);
        }

        [TestMethod]
        public void File_SkipTwoBytesAndTakeFiveBytes_Test()
        {
            var query = "select ToHex(ToArray(Take(Skip(GetFileBytes(), 2), 5)), '|') from #disk.files('./Files', false) where Name = 'File1.txt'";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);

            Assert.AreEqual("BF|45|78|61|6D", table[0][0]);
        }

        [TestMethod]
        public void File_SkipTwoBytesAndTakeFiveBytes2_Test()
        {
            var query = "select ToHex(ToArray(SkipAndTake(GetFileBytes(), 2, 5)), '|') from #disk.files('./Files', false) where Name = 'File1.txt'";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);

            Assert.AreEqual("BF|45|78|61|6D", table[0][0]);
        }

        [TestMethod]
        public void File_GetHead_Test()
        {
            var query = "select ToHex(Head(2), '|') from #disk.files('./Files', false) where Name = 'File1.txt'";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);

            Assert.AreEqual("EF|BB", table[0][0]);
        }

        [TestMethod]
        public void File_GetTail_Test()
        {
            var query = "select ToHex(Tail(2), '|') from #disk.files('./Files', false) where Name = 'File1.txt'";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);

            Assert.AreEqual("31|2E", table[0][0]);
        }

        [TestMethod]
        public void FilesSource_CancelledLoadTest()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                tokenSource.Cancel();
                var source = new FilesSource("./Directories", true, new RuntimeContext(tokenSource.Token, new ISchemaColumn[0]));

                var fired = source.Rows.Count();

                Assert.AreEqual(0, fired);
            }
        }

        [TestMethod]
        public void FilesSource_FullLoadTest()
        {
            var source = new FilesSource("./Directories", true, RuntimeContext.Empty);

            var fired = source.Rows.Count();

            Assert.AreEqual(4, fired);
        }

        [TestMethod]
        public void DirectoriesCompare_CompareTwoDirectories()
        {
            var source = new CompareDirectoriesSource("./Directories/Directory1", "./Directories/Directory2", RuntimeContext.Empty);

            var rows = source.Rows.ToArray();

            var firstRow = rows[0].Contexts[0] as CompareDirectoriesResult;
            var secondRow = rows[1].Contexts[0] as CompareDirectoriesResult;
            var thirdRow = rows[2].Contexts[0] as CompareDirectoriesResult;

            Assert.AreEqual(new FileInfo("./Directories/Directory1/TextFile1.txt").FullName, firstRow.SourceFile.FullName);
            Assert.AreEqual(null, firstRow.DestinationFile);
            Assert.AreEqual(State.Removed, firstRow.State);


            Assert.AreEqual(null, secondRow.SourceFile);
            Assert.AreEqual(new FileInfo("./Directories/Directory2/TextFile2.txt").FullName, secondRow.DestinationFile.FullName);
            Assert.AreEqual(State.Added, secondRow.State);


            Assert.AreEqual(null, thirdRow.SourceFile);
            Assert.AreEqual(new FileInfo("./Directories/Directory2/Directory3/TextFile3.txt").FullName, thirdRow.DestinationFile.FullName);
            Assert.AreEqual(State.Added, thirdRow.State);
        }

        [TestMethod]
        public void DirectoriesCompare_CompareWithItself()
        {
            var source = new CompareDirectoriesSource("./Directories/Directory1", "./Directories/Directory1", RuntimeContext.Empty);

            var rows = source.Rows.ToArray();

            var firstRow = rows[0].Contexts[0] as CompareDirectoriesResult;

            Assert.AreEqual(new FileInfo("./Directories/Directory1/TextFile1.txt").FullName, firstRow.SourceFile.FullName);
            Assert.AreEqual(new FileInfo("./Directories/Directory1/TextFile1.txt").FullName, firstRow.DestinationFile.FullName);
            Assert.AreEqual(State.TheSame, firstRow.State);
        }

        [TestMethod]
        public void Query_CompareTwoDirectiories()
        {
            var query = "select * from #disk.DirsCompare('./Directories/Directory1', './Directories/Directory2')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();
        }

        [TestMethod]
        public void Query_CompareTwoDirectiories_WithSha()
        {
            var query = "select Sha256File(SourceFile) from #disk.DirsCompare('./Directories/Directory1', './Directories/Directory2') where SourceFile is not null";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();
        }

        [TestMethod]
        public void Query_IntersectSameDirectoryTest()
        {
            var query = @"
with IntersectedFiles as (
	select a.Name as Name, a.Sha256File() as sha1, b.Sha256File() as sha2 from #os.files('.\Files', true) a inner join #os.files('.\Files', true) b on a.FullName = b.FullName
)
select * from IntersectedFiles";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);

            Assert.AreEqual("File1.txt", table[0][0]);
            Assert.AreEqual(table[0][1], table[0][2]);

            Assert.AreEqual("File2.txt", table[1][0]);
            Assert.AreEqual(table[1][1], table[1][2]);
        }

        [TestMethod]
        public void Query_DirectoryDiffTest()
        {
            var query = @"
with FirstDirectory as (
    select a.GetRelativePath('.\Files') as RelativeName, a.Sha256File() as sha from #os.files('.\Files', true) a
), SecondDirectory as (
    select a.GetRelativePath('.\Files2') as RelativeName, a.Sha256File() as sha from #os.files('.\Files2', true) a
), IntersectedFiles as (
	select a.RelativeName as RelativeName, a.sha as sha1, b.sha as sha2 from FirstDirectory a inner join SecondDirectory b on a.RelativeName = b.RelativeName
), ThoseInLeft as (
	select a.RelativeName as RelativeName, a.sha as sha1, '' as sha2 from FirstDirectory a left outer join SecondDirectory b on a.RelativeName = b.RelativeName where b.RelativeName is null
), ThoseInRight as (
	select b.RelativeName as RelativeName, '' as sha1, b.sha as sha2 from FirstDirectory a right outer join SecondDirectory b on a.RelativeName = b.RelativeName where a.RelativeName is null
)
select RelativeName, (case when sha1 <> sha2 then 'modified' else 'the same' end) as state from IntersectedFiles
union all (RelativeName)
select RelativeName, 'removed' as state from ThoseInLeft
union all (RelativeName)
select RelativeName, 'added' as state from ThoseInRight";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);

            Assert.AreEqual("\\File1.txt", table[0][0]);
            Assert.AreEqual("modified", table[0][1]);

            Assert.AreEqual("\\File2.txt", table[1][0]);
            Assert.AreEqual("removed", table[1][1]);

            Assert.AreEqual("\\File3.txt", table[2][0]);
            Assert.AreEqual("added", table[2][1]);
        }

        [TestMethod]
        public void Query_ShouldNotThrowException()
        {
            var query = "select (case when SourceFile is not null then ToHex(Head(SourceFile, 5), '|') else '' end) as t, DestinationFileRelative, State from #os.dirscompare('./Files', './Files')";
            
            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();
        }

        [TestMethod]
        public void Query_UseFilesFromFilteredDirectories()
        {
            var query = "" +
                "table Files {" +
                "   FullName 'System.String'" +
                "};" +
                "couple #os.files with table Files as SourceOfFiles;" +
                "with dirs as (" +
                "   select FullName, true from #os.directories('./Directories', false)" +
                ")" +
                "select FullName from SourceOfFiles(dirs)";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.IsTrue(table.Any(row => ((string)row[0]).Contains("TextFile1")));
            Assert.IsTrue(table.Any(row => ((string)row[0]).Contains("TextFile3")));
            Assert.IsTrue(table.Any(row => ((string)row[0]).Contains("TextFile2")));
        }

        [TestMethod]
        public void TraverseToDirectoryFromRootTest() 
        {
            var library = new OsLibrary();
            var separator = Path.DirectorySeparatorChar;

            Assert.AreEqual("this", library.SubPath($"this{separator}is{separator}test", 0));
            Assert.AreEqual($"this{separator}is", library.SubPath($"this{separator}is{separator}test", 1));
            Assert.AreEqual($"this{separator}is{separator}test", library.SubPath($"this{separator}is{separator}test", 2));
            Assert.AreEqual($"this{separator}is{separator}test", library.SubPath($"this{separator}is{separator}test", 10));
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString(), new OsSchemaProvider());
        }

        static QueryDiskTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());

            Culture.ApplyWithDefaultCulture();
        }
    }
}