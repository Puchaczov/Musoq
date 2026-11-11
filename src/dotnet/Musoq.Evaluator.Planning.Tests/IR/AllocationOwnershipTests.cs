using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AllocationOwnershipTests
{
    public static IEnumerable<object[]> Cases => AllocationOwnershipCases.Cases();

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void Constructor_ShouldOwnItsCollectionStorage(Type type, string property, string kind)
    {
        AllocationOwnershipCases.AssertOwns(type, property, kind, clone: false);
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void RecordCloneInitializer_ShouldOwnItsCollectionStorage(Type type, string property, string kind)
    {
        AllocationOwnershipCases.AssertOwns(type, property, kind, clone: true);
    }

    [TestMethod]
    public void NestedValuesConstructor_ShouldCopyOuterAndInnerCollections()
    {
        var first = new ExecutionRowValue("value", new ExecutionLiteral(1, typeof(int)));
        var row = new[] { first };
        IReadOnlyList<ExecutionRowValue>[] rows = [row];
        var node = new ExecutionCreateValuesRows(
            new ExecutionVariable("rows", typeof(object)),
            new GeneratedRowShape("Row", []),
            rows
        );

        row[0] = new ExecutionRowValue("changed", new ExecutionLiteral(2, typeof(int)));
        rows[0] = [];

        Assert.AreSame(first, node.Values[0][0]);
    }

    [TestMethod]
    public void NestedValuesWithExpression_ShouldCopyOuterAndInnerCollections()
    {
        var first = new ExecutionRowValue("value", new ExecutionLiteral(1, typeof(int)));
        var row = new[] { first };
        IReadOnlyList<ExecutionRowValue>[] rows = [row];
        var original = new ExecutionCreateValuesRows(
            new ExecutionVariable("rows", typeof(object)),
            new GeneratedRowShape("Row", []),
            []
        );
        var clone = original with { Values = rows };

        row[0] = new ExecutionRowValue("changed", new ExecutionLiteral(2, typeof(int)));
        rows[0] = [];

        Assert.AreSame(first, clone.Values[0][0]);
    }
}
