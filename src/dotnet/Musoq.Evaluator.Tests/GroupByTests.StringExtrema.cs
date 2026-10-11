using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{
    [TestMethod]
    public void StringMinimum_ShouldProduceOneSmallestNamePerBucket()
    {
        const string query = "select Bucket, Min(Name) as Smallest from values { (Bucket: 'A', Name: 'a'), (Bucket: 'A', Name: 'z'), (Bucket: 'B', Name: 'b') } v group by Bucket order by Bucket";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Bucket", typeof(string)), ("Smallest", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "a"], ["B", "b"]);
    }

    [TestMethod]
    [DataRow("select Min(Name) as Smallest, Max(Name) as Largest from #A.Entities()", "", "z")]
    [DataRow("select Min(Name) as Smallest, Max(Name) as Largest from #A.Entities() where Name is null", null, null)]
    [DataRow("select Min(Name) as Smallest, Max(Name) as Largest from #A.Entities() where 1 = 0", null, null)]
    [DataRow("select Min(Name) filter (where Name <> '') as Smallest, Max(Name) filter (where Name <> '') as Largest from #A.Entities()", "A", "z")]
    [DataRow("select Min(Name) filter (where 1 = 0) as Smallest, Max(Name) filter (where 1 = 0) as Largest from #A.Entities()", null, null)]
    public void StringExtrema_ShouldPreserveValuesAndStringMetadata(string query, string? minimum, string? maximum)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity(null), new BasicEntity(""), new BasicEntity("a"), new BasicEntity("A"), new BasicEntity("z")] }
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Smallest", typeof(string)), ("Largest", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [minimum, maximum]);
    }
}
