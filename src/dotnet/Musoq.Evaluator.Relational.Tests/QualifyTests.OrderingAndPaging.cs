using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class QualifyTests
{
    [TestMethod]
    public void WhenQualifyWithOrderBySkipTake_ShouldPageAfterFilteringAndOrdering()
    {
        var query = @"
            select Name, RowNumber() over (order by Name) as rn
            from #A.Entities()
            qualify RowNumber() over (order by Name) <= 4
            order by rn desc
            skip 1
            take 2";

        var sources = CreateSingleSource(
            new BasicEntity("Eve"),
            new BasicEntity("Charlie"),
            new BasicEntity("Alice"),
            new BasicEntity("Bob"),
            new BasicEntity("Diana"));

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Charlie", 3L],
            ["Bob", 2L]);
    }
}
