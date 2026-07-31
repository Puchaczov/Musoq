using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ThirdRoundApplyResultTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void ChainedOuterApplyWithEmptyIntermediate_ShouldPreserveOuterRowAndResetOrdinals()
    {
        const string query = @"
            select a.Name, first.Value, first.Ordinal, second.Value, second.Ordinal
            from #schema.first() a
            outer apply a.FirstValues first with ordinality
            outer apply a.SecondValues second with ordinality
            order by a.Name, first.Ordinal, second.Ordinal";
        var source = new[]
        {
            new ChainedApplyRow { Name = "Empty", FirstValues = [], SecondValues = [7, 8] },
            new ChainedApplyRow { Name = "Full", FirstValues = [1, 2], SecondValues = [9] }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("first.Value", typeof(int?)),
            ("first.Ordinal", typeof(int?)),
            ("second.Value", typeof(int?)),
            ("second.Ordinal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Empty", null, null, 7, 0],
            ["Empty", null, null, 8, 1],
            ["Full", 1, 0, 9, 0],
            ["Full", 2, 1, 9, 0]);
    }

    [TestMethod]
    public void OuterApplyObjectRowsWithWindowAndQualify_ShouldRetainNullExtendedMetadata()
    {
        const string query = @"
            select a.Name, child.Value, child.Ordinal,
                   RowNumber() over (partition by a.Name order by child.Ordinal) as rn
            from #schema.first() a
            outer apply a.Children child with ordinality
            qualify RowNumber() over (partition by a.Name order by child.Ordinal) <= 2
            order by a.Name, child.Ordinal";
        var source = new[]
        {
            new ObjectApplyRow
            {
                Name = "Empty",
                Children = [],
            },
            new ObjectApplyRow
            {
                Name = "Full",
                Children =
                [
                    new ObjectApplyChild { Value = "A" },
                    new ObjectApplyChild { Value = "B" },
                    new ObjectApplyChild { Value = "C" }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("child.Value", typeof(string)),
            ("child.Ordinal", typeof(int?)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Empty", null, null, 1L],
            ["Full", "A", 0, 1L],
            ["Full", "B", 1, 2L]);
    }

    [TestMethod]
    public void CrossApplyDuplicateAndNullRows_ShouldPreserveMultiplicityAndValues()
    {
        const string query = @"
            select a.Name, value.Value
            from #schema.first() a
            cross apply a.Values value with ordinality
            order by a.Name, value.Ordinal";
        var source = new[]
        {
            new NullableApplyRow { Name = "A", Values = [1, 1, null] },
            new NullableApplyRow { Name = "B", Values = [2] }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("value.Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["A", 1],
            ["A", 1],
            ["A", null],
            ["B", 2]);
    }

    public sealed class ChainedApplyRow
    {
        public string Name { get; init; } = string.Empty;

        public int[] FirstValues { get; init; } = [];

        public int[] SecondValues { get; init; } = [];
    }

    public sealed class ObjectApplyRow
    {
        public string Name { get; init; } = string.Empty;

        [BindablePropertyAsTable]
        public ObjectApplyChild[] Children { get; init; } = [];
    }

    public sealed class ObjectApplyChild
    {
        public string Value { get; init; } = string.Empty;
    }

    public sealed class NullableApplyRow
    {
        public string Name { get; init; } = string.Empty;

        public IEnumerable<int?> Values { get; init; } = [];
    }
}
