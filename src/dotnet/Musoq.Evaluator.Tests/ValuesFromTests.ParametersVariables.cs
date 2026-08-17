using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    public void ValuesSource_WithScalarParameter_ShouldUseRuntimeValue()
    {
        const string query = @"
param(baseScore: int)
from values {
    { Name: 'first', Score: $baseScore },
    { Name: 'second', Score: $baseScore + 5 }
} scores
select scores.Name, scores.Score
order by scores.Score";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        vm.Parameters["baseScore"] = 10;
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Name", typeof(string)),
            ("scores.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["first", 10], ["second", 15]);
    }

    [TestMethod]
    public void ValuesSource_WithNullableScalarParameter_ShouldInferNullableColumn()
    {
        const string query = @"
param(optionalScore: int? = null)
from values {
    { Name: 'first', Score: $optionalScore },
    { Name: 'second', Score: 20 }
} scores
select scores.Name, scores.Score
order by scores.Name";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Name", typeof(string)),
            ("scores.Score", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            new object?[] { "first", null },
            ["second", 20]);
    }

    [TestMethod]
    public void ValuesSource_WithScalarLetReferences_ShouldUseResolvedValues()
    {
        const string query = @"
let prefix: string = 'pkg'
let baseScore: int = 40
from values {
    { Name: $prefix + '-a', Score: $baseScore + 1 },
    { Name: $prefix + '-b', Score: $baseScore + 2 }
} scores
select scores.Name, scores.Score
order by scores.Score";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Name", typeof(string)),
            ("scores.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["pkg-a", 41], ["pkg-b", 42]);
    }

    [TestMethod]
    public void ValuesSource_WithLiteralAndScalarParameter_ShouldInferSharedType()
    {
        const string query = @"
param(baseScore: int)
from values {
    { Name: 'literal', Score: 7 },
    { Name: 'parameter', Score: $baseScore }
} scores
select scores.Name, scores.Score
order by scores.Score";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        vm.Parameters["baseScore"] = 12;
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("scores.Name", typeof(string)),
            ("scores.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["literal", 7], ["parameter", 12]);
    }

    [TestMethod]
    public void ValuesSource_WithSourceColumnReference_ShouldThrow()
    {
        const string query = @"
select v.Score
from #A.entities() a
inner join values {
    { Score: a.Population }
} v on a.Id = v.Score";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Id = 1, Population = 100m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        MusoqExceptionAssertions.AssertSingleError(
            ex,
            DiagnosticCode.MQ3055_InvalidValuesSource,
            DiagnosticPhase.Bind,
            "scalar script parameter/let expression");
    }
}
