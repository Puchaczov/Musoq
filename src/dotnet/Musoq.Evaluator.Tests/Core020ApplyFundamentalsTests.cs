using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core020ApplyFundamentalsTests : GenericEntityTestBase
{
    [TestMethod]
    public void CrossApply_CorrelatedSchemaArguments_ShouldEvaluatePerLeftRowIncludingNull()
    {
        const string query =
            "select a.Key, b.FilterKey, b.Score from #schema.first() a " +
            "cross apply #schema.second(a.Key, a.Text) b";

        var parents = new[]
        {
            new ApplyParent { Key = "one", Text = "alpha" },
            new ApplyParent { Key = null, Text = "none" },
            new ApplyParent { Key = "two", Text = "beta" }
        };
        var children = new[]
        {
            new ApplyChild { FilterKey = "one", Score = 10 },
            new ApplyChild { FilterKey = null, Score = 0 },
            new ApplyChild { FilterKey = "two", Score = 20 }
        };
        var observedKeys = new List<object?>();
        var observedTexts = new List<object?>();

        var vm = CreateAndRunVirtualMachine(
            query,
            parents,
            children,
            filterSecondRowsSource: (parameters, source) =>
            {
                observedKeys.Add(parameters[0]);
                observedTexts.Add(parameters[1]);
                var key = parameters[0];
                return source.Filter(row => Equals(row.FilterKey, key)).ToArray();
            });

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("b.FilterKey", typeof(string)),
            ("b.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["one", "one", 10],
            [null, null, 0],
            ["two", "two", 20]);
        CollectionAssert.AreEqual(
            new object?[] { "one", null, "two" },
            observedKeys.ToArray());
        CollectionAssert.AreEqual(
            new object?[] { "alpha", "none", "beta" },
            observedTexts.ToArray());
    }

    [TestMethod]
    public void OuterApply_CorrelatedSchemaWithoutMatch_ShouldPreserveLeftAndNullExtend()
    {
        const string query =
            "select a.Key, b.FilterKey, b.Score from #schema.first() a " +
            "outer apply #schema.second(a.Key) b";

        var parents = new[]
        {
            new ApplyParent { Key = "one", Text = "alpha" },
            new ApplyParent { Key = "missing", Text = "none" },
            new ApplyParent { Key = null, Text = "null-key" }
        };
        var children = new[]
        {
            new ApplyChild { FilterKey = "one", Score = 10 }
        };
        var observedKeys = new List<object?>();

        var vm = CreateAndRunVirtualMachine(
            query,
            parents,
            children,
            filterSecondRowsSource: (parameters, source) =>
            {
                observedKeys.Add(parameters[0]);
                var key = parameters[0];
                return source.Filter(row => Equals(row.FilterKey, key)).ToArray();
            });

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("b.FilterKey", typeof(string)),
            ("b.Score", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["one", "one", 10],
            ["missing", null, null],
            [null, null, null]);
        CollectionAssert.AreEqual(
            new object?[] { "one", "missing", null },
            observedKeys.ToArray());
    }

    [TestMethod]
    public void OuterApply_EmptyComplexCollection_ShouldPromoteValueColumnsAndPreserveLeft()
    {
        const string query =
            "select a.Key, b.Label, b.Score from #schema.first() a outer apply a.Items b";

        var parents = new[]
        {
            new ApplyParent
            {
                Key = "matched",
                Items = [new ApplyItem { Label = "item", Score = 7 }]
            },
            new ApplyParent { Key = "empty", Items = [] }
        };

        var vm = CreateAndRunVirtualMachine(query, parents);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("b.Label", typeof(string)),
            ("b.Score", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["matched", "item", 7],
            ["empty", null, null]);
    }

    [TestMethod]
    public void CrossApply_RowMethodChain_ShouldExposeOnlyMatchingPrimitiveRows()
    {
        const string query =
            "select a.Key, b.Value, c.Value from #schema.first() a " +
            "cross apply a.Split(a.Text, '|') b " +
            "cross apply b.ToCharArray(b.Value) c";

        var parents = new[]
        {
            new ApplyParent { Key = "words", Text = "a|bc" },
            new ApplyParent { Key = "empty", Text = string.Empty }
        };

        var vm = CreateAndRunVirtualMachine(query, parents);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("b.Value", typeof(string)),
            ("c.Value", typeof(char)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["words", "a", 'a'],
            ["words", "bc", 'b'],
            ["words", "bc", 'c']);
    }

    [TestMethod]
    public void CrossApply_PrimitiveAndComplexCollections_ShouldExposeDistinctElementShapes()
    {
        const string query =
            "select n.Value, i.Label, i.Score from #schema.first() a " +
            "cross apply a.Numbers n " +
            "cross apply a.Items i";

        var parents = new[]
        {
            new ApplyParent
            {
                Numbers = [1, 2],
                Items =
                [
                    new ApplyItem { Label = "x", Score = 10 },
                    new ApplyItem { Label = "y", Score = 20 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, parents);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("n.Value", typeof(int)),
            ("i.Label", typeof(string)),
            ("i.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, "x", 10],
            [1, "y", 20],
            [2, "x", 10],
            [2, "y", 20]);
    }

    [TestMethod]
    public void CrossApply_NestedCollectionPropertyChain_ShouldExposeComplexRows()
    {
        const string query =
            "select b.Label, b.Score from #schema.first() a " +
            "cross apply a.Container.Items b";

        var parents = new[]
        {
            new ApplyParent
            {
                Container = new ApplyContainer
                {
                    Items = [new ApplyItem { Label = "nested", Score = 3 }]
                }
            }
        };

        var vm = CreateAndRunVirtualMachine(query, parents);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("b.Label", typeof(string)),
            ("b.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["nested", 3]);
    }

    [TestMethod]
    public void MissingOuterApplyAlias_ShouldReportExactRequiredAliasContract()
    {
        const string query =
            "select 1 from #schema.first() a outer apply #schema.second() where 1 = 1";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new ApplyParent()],
                [new ApplyChild()]));

        var source = "#schema.second()";
        var expectedOffset = query.IndexOf(source, StringComparison.Ordinal) + source.Length;
        var envelope = AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ2035_MissingRequiredAlias,
            DiagnosticPhase.Parse,
            "The OUTER APPLY source requires an alias before WHERE.",
            expectedOffset,
            0,
            "A source in a multi-source query needs a stable alias so JOIN and APPLY expressions can address it reliably. Derived tables and VALUES sources always require an alias.",
            "Core Spec - Aliasing",
            [
                "Add an alias immediately after the source, for example: FROM #schema.items() items.",
                "Use AS for clarity, or bracket an alias that is also a SQL keyword."
            ]);

        Assert.HasCount(5, envelope.Arguments);
        Assert.AreEqual("required-source-alias", envelope.Arguments["aliasKind"]);
        Assert.AreEqual("schema", envelope.Arguments["sourceKind"]);
        Assert.AreEqual("OUTER APPLY", envelope.Arguments["operator"]);
        Assert.AreEqual("WHERE", envelope.Arguments["boundary"]);
        Assert.AreEqual("false", envelope.Arguments["isFirstSource"]);
    }

    [TestMethod]
    public void CrossApplyScalarProperty_ShouldReportExactArrayDiagnostic()
    {
        const string query =
            "select b.Value from #schema.first() a cross apply a.Scalar as b";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, [new ScalarApplyParent { Scalar = 5 }]));

        var expectedOffset = query.LastIndexOf("b", StringComparison.Ordinal);
        AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ3025_ColumnMustBeArray,
            DiagnosticPhase.Bind,
            "Column must be an array or implement IEnumerable<T> interface",
            expectedOffset,
            1,
            "The expression must return an array or enumerable value for this operation.",
            "Core Spec - APPLY and Arrays",
            [
                "Use a column that returns an array or IEnumerable value.",
                "Remove the array operation if the source value is scalar."
            ]);
    }

    [TestMethod]
    public void CrossApplyInvalidBindableProperty_ShouldReportExactBindableDiagnostic()
    {
        const string query =
            "select b.Value from #schema.first() a cross apply a.NotACollection as b";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new InvalidBindableApplyParent { NotACollection = new ApplyItem() }]));

        var expectedOffset = query.LastIndexOf("b", StringComparison.Ordinal);
        AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ3026_ColumnNotBindable,
            DiagnosticPhase.Bind,
            "Column 'NotACollection' must be marked with BindablePropertyAsTable attribute to be used in this context.",
            expectedOffset,
            1,
            "The selected column cannot be bound as a table source.",
            "Core Spec - Bindable Properties",
            [
                "Expose the property as bindable in the schema when it should be used as a nested table.",
                "Use a regular property reference if the value should stay scalar."
            ]);
    }

    private static MusoqErrorEnvelope AssertExactEnvelope(
        MusoqQueryException exception,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase,
        string expectedMessage,
        int expectedOffset,
        int expectedLength,
        string expectedExplanation,
        string expectedDocsReference,
        string[] expectedFixes)
    {
        Assert.HasCount(1, exception.Envelopes,
            $"Expected one diagnostic but got: {string.Join(" | ", exception.Envelopes.Select(item => item.Code))}");

        var envelope = exception.PrimaryEnvelope;
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(expectedPhase, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedMessage, envelope.Message);
        Assert.AreEqual(expectedOffset, envelope.Offset);
        Assert.AreEqual(expectedOffset + expectedLength, envelope.EndOffset);
        Assert.AreEqual(expectedLength, envelope.Length);
        Assert.IsNotNull(envelope.Snippet);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        CollectionAssert.AreEqual(
            expectedFixes,
            envelope.Actions.Select(static action => action.Title).ToArray());
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));

        return envelope;
    }

    public sealed class ApplyParent
    {
        public string? Key { get; set; }

        public string Text { get; set; } = string.Empty;

        public int[] Numbers { get; set; } = [];

        [BindablePropertyAsTable]
        public ApplyItem[] Items { get; set; } = [];

        public ApplyContainer Container { get; set; } = new();
    }

    public sealed class ApplyChild
    {
        public string? FilterKey { get; set; }

        public int Score { get; set; }
    }

    public sealed class ApplyItem
    {
        public string Label { get; set; } = string.Empty;

        public int Score { get; set; }
    }

    public sealed class ApplyContainer
    {
        [BindablePropertyAsTable]
        public List<ApplyItem> Items { get; set; } = [];
    }

    public sealed class ScalarApplyParent
    {
        public int Scalar { get; set; }
    }

    public sealed class InvalidBindableApplyParent
    {
        [BindablePropertyAsTable]
        public ApplyItem? NotACollection { get; set; }
    }
}
