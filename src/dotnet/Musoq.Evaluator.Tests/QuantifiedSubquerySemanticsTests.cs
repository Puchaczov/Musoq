using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class QuantifiedSubquerySemanticsTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("ANY", "=")]
    [DataRow("ANY", "<>")]
    [DataRow("ANY", ">")]
    [DataRow("ANY", ">=")]
    [DataRow("ANY", "<")]
    [DataRow("ANY", "<=")]
    [DataRow("SOME", "=")]
    [DataRow("SOME", "<>")]
    [DataRow("SOME", ">")]
    [DataRow("SOME", ">=")]
    [DataRow("SOME", "<")]
    [DataRow("SOME", "<=")]
    [DataRow("ALL", "=")]
    [DataRow("ALL", "<>")]
    [DataRow("ALL", ">")]
    [DataRow("ALL", ">=")]
    [DataRow("ALL", "<")]
    [DataRow("ALL", "<=")]
    public void Comparisons_ShouldReduceTrueFalseUnknownAndEmptyInputs(string quantifier, string operation)
    {
        int?[][] groups = [[1, null], [3, null], [null], [], [1], [], [1, 1], [3, 3], [2, 2], [null, 1], [null, 3]];
        var outer = Enumerable.Range(1, groups.Length).Select(id => new BasicEntity
        {
            Id = id, NullableValue = id is 5 or 6 ? null : 2
        }).ToArray();
        var inner = groups.SelectMany((values, index) => values.Select(value => new BasicEntity
        {
            Id = index + 1, NullableValue = value
        })).ToArray();
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = outer, ["#B"] = inner };
        var query = $"select o.Id, o.NullableValue {operation} {quantifier} " +
                    "(select i.NullableValue from #B.entities() i where i.Id = o.Id) as Outcome " +
                    "from #A.entities() o order by o.Id";
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)), ("Outcome", typeof(bool?)));
        var expected = outer.Select((row, index) => new object?[]
        {
            row.Id, Reduce(groups[index].Select(value => Compare(row.NullableValue, value, operation)), quantifier)
        }).ToArray();
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
        var negatedQuery = $"select o.Id from #A.entities() o where not (o.NullableValue {operation} {quantifier} " +
                           "(select i.NullableValue from #B.entities() i where i.Id = o.Id)) order by o.Id";
        using var filtered = CreateAndRunVirtualMachine(negatedQuery, sources);
        using var filteredResult = filtered.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(filteredResult,
            expected.Where(row => row[1] is false).Select(row => new object?[] { row[0] }).ToArray());
    }

    [TestMethod]
    [DataRow("select Max(i.NullableValue) from #B.entities() i where i.Id >= o.Id group by i.Country")]
    [DataRow("select i.NullableValue from #B.entities() i where i.Id >= o.Id qualify RowNumber() over (order by i.Id desc) = 1")]
    public void GroupedAndWindowedBodies_ShouldCompareTheirFinalValues(string body)
    {
        var query = $"select 2 > ANY ({body}) as AnyValue, 2 > ALL ({body}) as AllValue from #A.entities() o";
        using var compiled = CreateAndRunVirtualMachine(query, ShapingSources());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [false, false]);
    }

    [TestMethod]
    public void DifferentComparisonsOverTheSameBody_ShouldRemainDistinct()
    {
        const string body = "select i.NullableValue from #B.entities() i where i.Id > o.Id";
        var query = $"select 2 > ANY ({body}) as Greater, 2 < ANY ({body}) as Less, " +
                    $"2 > ALL ({body}) as AllGreater, 2 < ALL ({body}) as AllLess from #A.entities() o";
        using var compiled = CreateAndRunVirtualMachine(query, ShapingSources());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [false, true, false, true]);
    }

    [TestMethod]
    [DataRow("ANY")]
    [DataRow("SOME")]
    [DataRow("ALL")]
    public void ShapedBody_ShouldCompareAfterOrderSkipTakeAndDistinct(string quantifier)
    {
        var query = $"select 2 > {quantifier} (select distinct i.NullableValue from #B.entities() i " +
                    "where i.Id >= o.Id order by i.NullableValue desc skip 0 take 1) as Outcome from #A.entities() o";
        using var compiled = CreateAndRunVirtualMachine(query, ShapingSources());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [false]);
    }

    [TestMethod]
    [DataRow("ANY", false)]
    [DataRow("SOME", false)]
    [DataRow("ALL", true)]
    public void EmptyShapedBody_ShouldUseQuantifierIdentityEvenWithNullLeft(string quantifier, bool expected)
    {
        var query = $"select o.NullableValue > {quantifier} (select i.NullableValue from #B.entities() i " +
                    "where i.Id >= o.Id take 0) as Outcome from #A.entities() o";
        var sources = ShapingSources();
        sources["#A"] = [new BasicEntity { Id = 1 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [expected]);
    }

    [TestMethod]
    [DataRow("union all")]
    [DataRow("union (Value)")]
    [DataRow("except (Value)")]
    [DataRow("intersect (Value)")]
    public void SetBody_ShouldRetainBranchOuterReferencesAndUnknown(string operation)
    {
        var body = "select i.NullableValue + o.Id as Value from #B.entities() i where i.Id = o.Id " +
                   operation + " select j.NullableValue + o.Id as Value from #B.entities() j where j.Id > o.Id";
        var query = $"select 3 > ANY ({body}) as AnyValue, 3 > ALL ({body}) as AllValue from #A.entities() o";
        var sources = ShapingSources();
        sources["#B"] = [new BasicEntity { Id = 1, NullableValue = 1 }, new BasicEntity { Id = 2 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        object?[] expected = operation switch
        {
            "except (Value)" => [true, true],
            "intersect (Value)" => [false, true],
            _ => [true, null]
        };
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    [TestMethod]
    public void NegationAndCoalesce_ShouldPreserveUnknownUntilExplicitlyReplaced()
    {
        const string body = "select i.NullableValue from #B.entities() i where i.Id = o.Id";
        var query = $"select not (2 > ANY ({body})) as Negated, " +
                    $"(2 > ALL ({body})) ?? false as Defaulted, " +
                    $"case when o.Id < 0 then (1 / (o.Id - 1)) > ALL ({body}) else true end as Guarded " +
                    "from #A.entities() o";
        var sources = ShapingSources();
        sources["#B"] = [new BasicEntity { Id = 1 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [null, false, true]);
    }

    [TestMethod]
    public void NestedQuantifier_ShouldCaptureGrandparentReferences()
    {
        const string query = "select 2 > ANY (select i.NullableValue from #B.entities() i where i.Id > o.Id " +
                             "and i.NullableValue < ANY (select j.NullableValue from #B.entities() j where j.Id = o.Id)) as Outcome " +
                             "from #A.entities() o";
        using var compiled = CreateAndRunVirtualMachine(query, ShapingSources());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [false]);
    }

    [TestMethod]
    public void HavingQuantifier_ShouldUseTheGroupedOuterBinding()
    {
        const string query = "select o.Country, Count(*) as Total from #A.entities() o group by o.Country " +
                             "having Count(*) > ANY (select i.Id from #B.entities() i where i.Country = o.Country take 1)";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Country = "P" }, new BasicEntity { Country = "P" }],
            ["#B"] = [new BasicEntity { Country = "P", Id = 1 }]
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["P", 2L]);
    }

    [TestMethod]
    public void QualifyQuantifier_ShouldRunAfterTheWindowResultExists()
    {
        const string query = "select o.Id, RowNumber() over (order by o.Id) as Position from #A.entities() o " +
                             "qualify RowNumber() over (order by o.Id) > ANY " +
                             "(select i.Id from #B.entities() i where i.Id = o.Id take 0)";
        using var compiled = CreateAndRunVirtualMachine(query, ShapingSources());
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    [DoNotParallelize]
    [DataRow("ANY", 0)]
    [DataRow("ANY", 2)]
    [DataRow("ALL", 0)]
    [DataRow("ALL", 2)]
    public void LeftOperand_ShouldBeEvaluatedOnceIncludingForEmptyInputs(string quantifier, int take)
    {
        var baseline = new Library().NextValue();
        var query = $"select NextValue() > {quantifier} (select i.Id from #B.entities() i where i.Id >= o.Id take {take}) as Outcome, " +
                    "NextValue() as Calls from #A.entities() o";
        using var compiled = CreateAndRunVirtualMachine(query, ShapingSources());
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(baseline + 2, result[0].ReadValue(1));
    }

    [TestMethod]
    [DoNotParallelize]
    [DataRow("ANY")]
    [DataRow("SOME")]
    [DataRow("ALL")]
    public void UnknownQuantifierOnLeftOfAnd_ShouldEvaluateTheRightOperand(string quantifier)
    {
        var baseline = new Library().NextValue();
        var query = $"select o.Id from #A.entities() o where 2 > {quantifier} " +
                    "(select i.NullableValue from #B.entities() i where i.Id = o.Id) and NextValue() > 0";
        var sources = ShapingSources();
        sources["#B"] = [new BasicEntity { Id = 1 }];
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(baseline + 2, new Library().NextValue());
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> ShapingSources() => new()
    {
        ["#A"] = [new BasicEntity { Id = 1, NullableValue = 2 }],
        ["#B"] = [new BasicEntity { Id = 1, NullableValue = 1 }, new BasicEntity { Id = 2, NullableValue = 3 }]
    };

    private static bool? Compare(int? left, int? right, string operation)
    {
        if (left is null || right is null)
            return null;
        return operation switch
        {
            "=" => left == right,
            "<>" => left != right,
            ">" => left > right,
            ">=" => left >= right,
            "<" => left < right,
            "<=" => left <= right,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static bool? Reduce(IEnumerable<bool?> comparisons, string quantifier)
    {
        var values = comparisons.ToArray();
        var decisive = quantifier != "ALL";
        if (values.Contains(decisive))
            return decisive;
        if (values.Contains(null))
            return null;
        return !decisive;
    }
}
