using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{
    [TestMethod]
    public void WhenQualifyPartitionedWithManyPartitions_ShouldFilterEachIndependently()
    {
        var query = @"
            select Country, City, Name,
                   RowNumber() over (partition by Country, City order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (partition by Country, City order by Name) = 1";

        var sources = CreateSingleSource(
            new BasicEntity("Zach") { Country = "US", City = "NYC" },
            new BasicEntity("Alice") { Country = "US", City = "NYC" },
            new BasicEntity("Bob") { Country = "US", City = "LA" },
            new BasicEntity("Clara") { Country = "UK", City = "London" },
            new BasicEntity("Dan") { Country = "UK", City = "London" },
            new BasicEntity("Eve") { Country = "US", City = "LA" },
            new BasicEntity("Frank") { Country = "UK", City = "Manchester" });

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["US", "NYC", "Alice", 1L],
            ["US", "LA", "Bob", 1L],
            ["UK", "London", "Clara", 1L],
            ["UK", "Manchester", "Frank", 1L]);
    }
}
