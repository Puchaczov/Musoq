using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Schema.Os.Zip;
using Environment = Musoq.Plugins.Environment;

namespace Musoq.Schema.Os.Tests.Core
{
    [TestClass]
    public class ZipTests
    {
        [TestInitialize]
        public void Initialize()
        {
            if (!Directory.Exists("./Results"))
                Directory.CreateDirectory("./Results");
        }

        [TestMethod]
        public void SimpleZipSelectTest()
        {
            var query = @"select FullName from #disk.zip('./Files.zip')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("FullName", table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(3, table.Count);
            Assert.AreEqual("Files/File1.txt", table[0].Values[0]);
            Assert.AreEqual("Files/File2.txt", table[1].Values[0]);
            Assert.AreEqual("Files/SubFolder/File3.txt", table[2].Values[0]);
        }

        [TestMethod]
        public void DecompressTest()
        {
            var query =
                "select Decompress(AggregateFiles(GetZipEntryFileInfo()), './Results/DecompressTest') from #disk.zip('./Files.zip')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Decompress(AggregateFiles(GetZipEntryFileInfo()), './Results/DecompressTest')",
                table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("./Results/DecompressTest", table[0].Values[0]);

            Assert.IsTrue(File.Exists("./Results/DecompressTest/File1.txt"));
            Assert.IsTrue(File.Exists("./Results/DecompressTest/File2.txt"));
            Assert.IsTrue(File.Exists("./Results/DecompressTest/SubFolder/File3.txt"));

            Directory.Delete("./Results/DecompressTest", true);
        }

        [TestMethod]
        public void DecompressWithFilterTest()
        {
            var query =
                "select Decompress(AggregateFiles(GetZipEntryFileInfo()), './Results/DecompressWithFilterTest') from #disk.zip('./Files.zip') where Level = 1";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(1, table.Columns.Count());
            Assert.AreEqual("Decompress(AggregateFiles(GetZipEntryFileInfo()), './Results/DecompressWithFilterTest')",
                table.Columns.ElementAt(0).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("./Results/DecompressWithFilterTest", table[0].Values[0]);

            Assert.IsTrue(File.Exists("./Results/DecompressWithFilterTest/File1.txt"));
            Assert.IsTrue(File.Exists("./Results/DecompressWithFilterTest/File2.txt"));

            Directory.Delete("./Results/DecompressWithFilterTest", true);
        }

        [TestMethod]
        public void DecompressZipWithHelperTest()
        {
            var tempDir = "./Temp";
            using (var file = File.OpenRead("./Files.zip"))
            {
                using (var zip = new ZipArchive(file))
                {
                    var unpackedFile = SchemaZipHelper.UnpackZipEntry(zip.Entries[0], "test.abc", tempDir);
                    unpackedFile.Delete();
                }
            }

            Directory.Delete(tempDir);
        }

        [TestMethod]
        public void DecompressZipSlipVulnerabilityTest()
        {
            var tempDir = "./Temp";
            using (var file = File.OpenRead("./Files.zip"))
            {
                using (var zip = new ZipArchive(file))
                {
                    Assert.ThrowsException<InvalidOperationException>(() => SchemaZipHelper.UnpackZipEntry(zip.Entries[0], "../test.abc", tempDir));
                }
            }
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString(), new OsSchemaProvider());
        }

        static ZipTests()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}