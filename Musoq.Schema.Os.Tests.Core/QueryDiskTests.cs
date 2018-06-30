using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema.DataSources;
using Musoq.Schema.Os.Files;
using Musoq.Schema.Os.Tests.Core.Utils;

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
        public void TestDesc()
        {
            var query = "desc #os.files('C:/','false')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();
        }

        [TestMethod]
        public void TestFilesSourceIterateDirectories()
        {
            var source = new TestFilesSource("./Directories", false);

            var folders = source.GetFolders();

            Assert.AreEqual(1, folders.Count);

            Assert.AreEqual("TestFile1.txt", ((FileInfo)folders[0].Context).Name);
        }

        [TestMethod]
        public void TestFilesSourceIterateWithNestedDirectories()
        {
            var source = new TestFilesSource("./Directories", true);

            var folders = source.GetFolders();

            Assert.AreEqual(4, folders.Count);

            Assert.AreEqual("TestFile1.txt", ((FileInfo)folders[0].Context).Name);
            Assert.AreEqual("TextFile2.txt", ((FileInfo)folders[1].Context).Name);
            Assert.AreEqual("TextFile3.txt", ((FileInfo)folders[2].Context).Name);
            Assert.AreEqual("TextFile1.txt", ((FileInfo)folders[3].Context).Name);
        }

        [TestMethod]
        public void TestDirectoriesSourceIterateDirectories()
        {
            var source = new TestDirectoriesSource("./Directories", false);

            var directories = source.GetDirectories();

            Assert.AreEqual(2, directories.Count);

            Assert.AreEqual("Directory1", ((DirectoryInfo)directories[0].Context).Name);
            Assert.AreEqual("Directory2", ((DirectoryInfo)directories[1].Context).Name);
        }

        [TestMethod]
        public void TestDirectoriesSourceIterateWithNestedDirectories()
        {
            var source = new TestDirectoriesSource("./Directories", true);

            var directories = source.GetDirectories();

            Assert.AreEqual(3, directories.Count);

            Assert.AreEqual("Directory1", ((DirectoryInfo) directories[0].Context).Name);
            Assert.AreEqual("Directory2", ((DirectoryInfo) directories[1].Context).Name);
            Assert.AreEqual("Directory3", ((DirectoryInfo) directories[2].Context).Name);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new OsSchemaProvider());
        }
    }
}