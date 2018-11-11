using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema.DataSources;
using Musoq.Schema.Os.Directories;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Tests.Core.Utils;
using Environment = Musoq.Plugins.Environment;

namespace Musoq.Schema.Os.Tests.Core
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
                table.Columns.ElementAt(0).Name);
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
                table.Columns.ElementAt(0).Name);
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
            Assert.AreEqual("Parent.Name", table.Columns.ElementAt(0).Name);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Directories", table[0].Values[0]);
            Assert.AreEqual("Directories", table[1].Values[0]);
        }

        [TestMethod]
        public void TestFilesTest()
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
        public void FilesSourceIterateDirectoriesTest()
        {
            var source = new TestFilesSource("./Directories", false, InterCommunicator.Empty);

            var folders = source.GetFiles();

            Assert.AreEqual(1, folders.Count);

            Assert.AreEqual("TestFile1.txt", ((FileInfo)folders[0].Context).Name);
        }

        [TestMethod]
        public void FilesSourceIterateWithNestedDirectoriesTest()
        {
            var source = new TestFilesSource("./Directories", true, InterCommunicator.Empty);

            var folders = source.GetFiles();

            Assert.AreEqual(4, folders.Count);

            Assert.AreEqual("TestFile1.txt", ((FileInfo)folders[0].Context).Name);
            Assert.AreEqual("TextFile2.txt", ((FileInfo)folders[1].Context).Name);
            Assert.AreEqual("TextFile3.txt", ((FileInfo)folders[2].Context).Name);
            Assert.AreEqual("TextFile1.txt", ((FileInfo)folders[3].Context).Name);
        }

        [TestMethod]
        public void DirectoriesSourceIterateDirectoriesTest()
        {
            var source = new TestDirectoriesSource("./Directories", false, InterCommunicator.Empty);

            var directories = source.GetDirectories();

            Assert.AreEqual(2, directories.Count);

            Assert.AreEqual("Directory1", ((DirectoryInfo)directories[0].Context).Name);
            Assert.AreEqual("Directory2", ((DirectoryInfo)directories[1].Context).Name);
        }

        [TestMethod]
        public void TestDirectoriesSourceIterateWithNestedDirectories()
        {
            var source = new TestDirectoriesSource("./Directories", true, InterCommunicator.Empty);

            var directories = source.GetDirectories();

            Assert.AreEqual(3, directories.Count);

            Assert.AreEqual("Directory1", ((DirectoryInfo) directories[0].Context).Name);
            Assert.AreEqual("Directory2", ((DirectoryInfo) directories[1].Context).Name);
            Assert.AreEqual("Directory3", ((DirectoryInfo) directories[2].Context).Name);
        }

        [TestMethod]
        public void NonExistingDirectoryTest()
        {
            var source = new TestDirectoriesSource("./Some/Non/Existing/Path", true, InterCommunicator.Empty);

            var directories = source.GetDirectories();

            Assert.AreEqual(0, directories.Count);
        }

        [TestMethod]
        public void NonExisitngFileTest()
        {
            var source = new TestFilesSource("./Some/Non/Existing/Path.pdf", true, InterCommunicator.Empty);

            var directories = source.GetFiles();

            Assert.AreEqual(0, directories.Count);
        }

        [TestMethod]
        public void DirectoriesSource_CancelledLoadTest()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            var source = new DirectoriesSource("./Directories", true, new InterCommunicator(tokenSource.Token));

            var fired = source.Rows.Count();

            Assert.AreEqual(0, fired);
        }

        [TestMethod]
        public void DirectoriesSource_FullLoadTest()
        {
            var source = new DirectoriesSource("./Directories", true, InterCommunicator.Empty);

            var fired = source.Rows.Count();

            Assert.AreEqual(3, fired);
        }

        [TestMethod]
        public void FilesSource_CancelledLoadTest()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            var source = new FilesSource("./Directories", true, new InterCommunicator(tokenSource.Token));

            var fired = source.Rows.Count();

            Assert.AreEqual(0, fired);
        }

        [TestMethod]
        public void FilesSource_FullLoadTest()
        {
            var source = new FilesSource("./Directories", true, InterCommunicator.Empty);

            var fired = source.Rows.Count();

            Assert.AreEqual(4, fired);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new OsSchemaProvider());
        }

        static QueryDiskTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}