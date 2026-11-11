using Musoq.Tests.Common;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvFixtureLifecycleTests : CsvExampleTestBase
{
    [TestMethod]
    public void WriteTempCsv_ShouldUseAnOwnedDirectoryUnderRepositoryArtifacts()
    {
        var path = WriteTempCsv("Name\nAda\n");
        var ownerDirectory = Path.GetDirectoryName(path)!;

        Assert.AreEqual(
            Path.Combine(RepositoryPaths.FindRoot(), ".artifacts", "csv-tests"),
            Directory.GetParent(ownerDirectory)!.FullName
        );
    }

    [TestMethod]
    public void CleanupCsvFixtures_ShouldPreserveAnotherFixturesFiles()
    {
        var first = new FixtureProbe();
        var second = new FixtureProbe();
        try
        {
            var firstPath = first.Write();
            var secondPath = second.Write();
            first.CleanupCsvFixtures();

            Assert.IsFalse(File.Exists(firstPath));
            Assert.IsTrue(File.Exists(secondPath));
        }
        finally
        {
            first.CleanupCsvFixtures();
            second.CleanupCsvFixtures();
        }
    }

    private sealed class FixtureProbe : CsvExampleTestBase
    {
        public string Write() => WriteTempCsv("Name\nAda\n");
    }
}
