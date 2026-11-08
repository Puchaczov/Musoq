using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{
    // P1-037: string Max uses ordinal comparison and ignores null input values.
    [TestMethod]
    [TestProperty("CandidateId", "P1-037")]
    public void StringMaximum_ShouldProduceOneLargestNamePerBucket()
    {
        const string query = "select Bucket, Max(Name) as Largest from values { (Bucket: 'A', Name: 'a'), (Bucket: 'A', Name: 'z'), (Bucket: 'B', Name: 'b') } v group by Bucket order by Bucket";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Bucket", typeof(string)), ("Largest", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "z"], ["B", "b"]);
    }
}
