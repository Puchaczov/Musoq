using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core021ApplyAdvancedTests : GenericEntityTestBase
{
    [TestMethod]
    public void CrossApplyDerivedTableWithOrdinality_ShouldResetOrdinalPerLeftRow()
    {
        const string query = """
            select a.Key, d.Value, d.Ordinal
            from #schema.first() a
            cross apply (
                select b.Key, b.Value
                from #schema.second() b
                where b.Key = a.Key
            ) d with ordinality
            order by a.Key, d.Ordinal
            """;
        var parents = new[]
        {
            new Apply021Parent { Key = "one" },
            new Apply021Parent { Key = "two" },
            new Apply021Parent { Key = "missing" }
        };
        var children = new[]
        {
            new Apply021Child { Key = "one", Value = "first" },
            new Apply021Child { Key = "one", Value = "second" },
            new Apply021Child { Key = "two", Value = "only" }
        };

        var schema = new GenericSchema<GenericLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["first"] = (new GenericEntityTable<Apply021Parent>(), new GenericChunkSource<Apply021Parent>(
                parents,
                GenericEntityTable<Apply021Parent>.NameToIndexMap,
                GenericEntityTable<Apply021Parent>.IndexToObjectAccessMap)),
            ["second"] = (new GenericEntityTable<Apply021Child>(), new GenericChunkSource<Apply021Child>(
                children,
                GenericEntityTable<Apply021Child>.NameToIndexMap,
                GenericEntityTable<Apply021Child>.IndexToObjectAccessMap))
        });
        var table = CreateAndRunVirtualMachine(query, parents, children)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("d.Value", typeof(string)),
            ("d.Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["one", "first", 0],
            ["one", "second", 1],
            ["two", "only", 0]);
    }

    [TestMethod]
    public void OuterApplyDerivedTableWithOrdinality_ShouldNullExtendValuesAndOrdinal()
    {
        const string query = """
            select a.Key, d.Score, d.Ordinal
            from #schema.first() a
            outer apply (
                select b.Key, b.Score
                from #schema.second() b
                where b.Key = a.Key
            ) d with ordinality
            """;
        var parents = new[]
        {
            new Apply021Parent { Key = "matched" },
            new Apply021Parent { Key = "empty" }
        };
        var children = new[]
        {
            new Apply021Child { Key = "matched", Score = 17 }
        };

        var table = CreateAndRunVirtualMachine(query, parents, children)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("d.Score", typeof(int?)),
            ("d.Ordinal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["matched", 17, 0],
            ["empty", null, null]);
    }

    [TestMethod]
    public void ChainedAppliesWithOrdinality_ShouldResetEachDependentSource()
    {
        const string query = """
            select a.Key, b.Value, b.Ordinal, c.Value, c.Ordinal
            from #schema.first() a
            cross apply a.Split(a.Text, '|') b with ordinality
            cross apply b.ToCharArray(b.Value) c with ordinality
            order by b.Ordinal, c.Ordinal
            """;
        var parents = new[]
        {
            new Apply021Parent { Key = "chain", Text = "a|bc" }
        };

        var table = CreateAndRunVirtualMachine(query, parents)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("b.Value", typeof(string)),
            ("b.Ordinal", typeof(int)),
            ("c.Value", typeof(char)),
            ("c.Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["chain", "a", 0, 'a', 0],
            ["chain", "bc", 1, 'b', 0],
            ["chain", "bc", 1, 'c', 1]);
    }

    [TestMethod]
    public void IndependentApplies_ShouldProduceCartesianProduct()
    {
        const string query = """
            select n.Value, w.Value
            from #schema.first() a
            cross apply a.Numbers n
            cross apply a.Words w
            order by n.Value, w.Value
            """;
        var parents = new[]
        {
            new Apply021Parent
            {
                Numbers = [1, 2],
                Words = ["x", "y", "z"]
            }
        };

        var table = CreateAndRunVirtualMachine(query, parents)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("n.Value", typeof(int)),
            ("w.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, "x"],
            [1, "y"],
            [1, "z"],
            [2, "x"],
            [2, "y"],
            [2, "z"]);
    }

    [TestMethod]
    public void CrossApplyDerivedSetBranches_ShouldShareCorrelationKey()
    {
        const string query = """
            select a.Key, d.Value
            from #schema.first() a
            cross apply (
                select b.Key, b.Value
                from #schema.second() b
                where b.Key = a.Key
                union (Key, Value)
                select c.Key, c.Value
                from #schema.third() c
                where c.Key = a.Key
            ) d
            """;
        var parents = new[]
        {
            new Apply021Parent { Key = "one" },
            new Apply021Parent { Key = "two" }
        };
        var children = new[]
        {
            new Apply021Child { Key = "one", Value = "second-one" }
        };
        var alternatives = new[]
        {
            new Apply021Alternative { Key = "one", Value = "third-one" },
            new Apply021Alternative { Key = "two", Value = "third-two" }
        };

        var table = CreateAndRunVirtualMachine(query, parents, children, alternatives)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Key", typeof(string)),
            ("d.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["one", "second-one"],
            ["one", "third-one"],
            ["two", "third-two"]);
    }

    [TestMethod]
    public void CorrelatedDerivedTableMissingVisibleKey_ShouldReportExactDiagnostic()
    {
        const string query = """
            select a.Key, d.Value
            from #schema.first() a
            cross apply (
                select b.Value
                from #schema.second() b
                where b.Key = a.Key
            ) d
            """;
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new Apply021Parent { Key = "one" }],
                [new Apply021Child { Key = "one", Value = "value" }]));
        var opening = query.IndexOf('(', query.IndexOf("cross apply", StringComparison.OrdinalIgnoreCase));
        var closing = query.IndexOf(") d", opening, StringComparison.Ordinal);

        var envelope = AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticPhase.Parse,
            "Visitor 'SubqueryToCteRewriteVisitor' failed during 'derived table rewrite': Correlated APPLY derived table must project local correlation column 'b.Key'.",
            opening,
            closing - opening + 1,
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            "Core Spec - Subqueries",
            [
                "Ensure the subquery starts with SELECT and is enclosed in parentheses.",
                "Use the subquery only in a supported expression or source position."
            ]);

        Assert.HasCount(0, envelope.Arguments);
    }

    [TestMethod]
    public void CorrelatedDerivedSetBranchesWithDifferentKeys_ShouldReportExactDiagnostic()
    {
        const string query = """
            select a.Key, d.Value
            from #schema.first() a
            cross apply (
                select b.Key, b.Value
                from #schema.second() b
                where b.Key = a.Key
                union (Key, Value)
                select c.OtherKey as Key, c.Value
                from #schema.third() c
                where c.OtherKey = a.Key
            ) d
            """;
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new Apply021Parent { Key = "one" }],
                [new Apply021Child { Key = "one", Value = "value" }],
                [new Apply021Alternative { OtherKey = "one", Value = "other" }]));
        var opening = query.IndexOf('(', query.IndexOf("cross apply", StringComparison.OrdinalIgnoreCase));
        var closing = query.IndexOf(") d", opening, StringComparison.Ordinal);

        var envelope = AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticPhase.Parse,
            "Visitor 'SubqueryToCteRewriteVisitor' failed during 'derived table rewrite': Every branch of a correlated APPLY set-operator derived table must use the same projected correlation key.",
            opening,
            closing - opening + 1,
            "A subquery is malformed or appears in a location where this parser path cannot accept it.",
            "Core Spec - Subqueries",
            [
                "Ensure the subquery starts with SELECT and is enclosed in parentheses.",
                "Use the subquery only in a supported expression or source position."
            ]);

        Assert.HasCount(0, envelope.Arguments);
    }

    [TestMethod]
    public void MissingApplyAliasBeforeOrdinality_ShouldReportExactRequiredAliasContract()
    {
        const string query = "select 1 from #schema.first() a cross apply a.Values with ordinality";
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, [new Apply021Parent()]));
        var source = "a.Values";
        var expectedOffset = query.IndexOf(source, StringComparison.Ordinal) + source.Length;

        var envelope = AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ2035_MissingRequiredAlias,
            DiagnosticPhase.Parse,
            "The CROSS APPLY source requires an alias before WITH ORDINALITY.",
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
        Assert.AreEqual("property", envelope.Arguments["sourceKind"]);
        Assert.AreEqual("CROSS APPLY", envelope.Arguments["operator"]);
        Assert.AreEqual("WITH ORDINALITY", envelope.Arguments["boundary"]);
        Assert.AreEqual("false", envelope.Arguments["isFirstSource"]);
    }

    [TestMethod]
    public void MissingDerivedApplyAliasBeforeOrdinality_ShouldReportExactRequiredAliasContract()
    {
        const string query = """
            select 1
            from #schema.first() a
            cross apply (select b.Value from #schema.second() b) with ordinality
            """;
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new Apply021Parent { Key = "one" }],
                [new Apply021Child { Value = "value" }]));
        var closing = query.LastIndexOf(')');

        var envelope = AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ2035_MissingRequiredAlias,
            DiagnosticPhase.Parse,
            "The derived table source requires an alias after the closing parenthesis.",
            closing + 1,
            0,
            "A source in a multi-source query needs a stable alias so JOIN and APPLY expressions can address it reliably. Derived tables and VALUES sources always require an alias.",
            "Core Spec - Aliasing",
            [
                "Add an alias immediately after the source, for example: FROM #schema.items() items.",
                "Use AS for clarity, or bracket an alias that is also a SQL keyword."
            ]);

        Assert.HasCount(3, envelope.Arguments);
        Assert.AreEqual("required-source-alias", envelope.Arguments["aliasKind"]);
        Assert.AreEqual("derived table", envelope.Arguments["sourceKind"]);
        Assert.AreEqual("the closing parenthesis", envelope.Arguments["boundary"]);
    }

    [TestMethod]
    public void InvalidOrdinalityKeyword_ShouldReportExactBoundaryDiagnostic()
    {
        const string query = "select 1 from #schema.first() a cross apply a.Values b with numbering";
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, [new Apply021Parent()]));
        var expectedOffset = query.IndexOf("with", StringComparison.OrdinalIgnoreCase);

        var envelope = AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ2002_MissingToken,
            DiagnosticPhase.Parse,
            "Expected ORDINALITY after WITH in APPLY source.",
            expectedOffset,
            4,
            "A required keyword, delimiter, or closing token is missing at this position.",
            "Core Spec - Statement Structure",
            [
                "Insert the missing keyword or delimiter near the highlighted location.",
                "Check for a missing FROM clause, comma, or closing parenthesis."
            ]);

        Assert.HasCount(0, envelope.Arguments);
    }

    [TestMethod]
    public void OrdinalityCollision_ShouldReportExactUnsupportedSyntaxContract()
    {
        const string query = "select item.Value from #schema.first() a cross apply a.OrdinalItems item with ordinality";
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, [new Apply021Parent
            {
                OrdinalItems = [new Apply021OrdinalItem { Value = "existing", Ordinal = 9 }]
            }]));

        var envelope = AssertExactEnvelope(
            exception,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticPhase.Parse,
            "Visitor 'BuildMetadataAndInferTypesTraverseVisitor' failed during 'ApplyOrdinalityIfNeeded': WITH ORDINALITY cannot be used because apply alias 'item' already exposes an Ordinal column.",
            0,
            0,
            "The query uses syntax that Musoq does not support or that is not valid in this position.",
            "Core Spec - Statement Structure",
            [
                "Rewrite the clause using Musoq SQL syntax.",
                "If this came from another SQL dialect, check the Musoq equivalent keywords."
            ],
            requireSnippet: true);

        Assert.HasCount(0, envelope.Arguments);
    }

    private static MusoqErrorEnvelope AssertExactEnvelope(
        MusoqQueryException exception,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase,
        string expectedMessage,
        int? expectedOffset,
        int expectedLength,
        string expectedExplanation,
        string expectedDocsReference,
        string[] expectedFixes,
        bool requireSnippet = true)
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
        Assert.AreEqual(expectedOffset.HasValue ? expectedOffset.Value + expectedLength : null, envelope.EndOffset);
        Assert.AreEqual(expectedLength, envelope.Length);
        if (requireSnippet)
            Assert.IsNotNull(envelope.Snippet);
        else
            Assert.IsNull(envelope.Snippet);
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

    public sealed class Apply021Parent
    {
        public string? Key { get; set; }

        public string Text { get; set; } = string.Empty;

        [BindablePropertyAsTable]
        public int[] Values { get; set; } = [];

        [BindablePropertyAsTable]
        public int[] Numbers { get; set; } = [];

        [BindablePropertyAsTable]
        public string[] Words { get; set; } = [];

        [BindablePropertyAsTable]
        public Apply021OrdinalItem[] OrdinalItems { get; set; } = [];
    }

    public sealed class Apply021Child
    {
        public string? Key { get; set; }

        public string? Value { get; set; }

        public int Score { get; set; }
    }

    public sealed class Apply021Alternative
    {
        public string? Key { get; set; }

        public string? OtherKey { get; set; }

        public string? Value { get; set; }
    }

    public sealed class Apply021OrdinalItem
    {
        public string? Value { get; set; }

        public int Ordinal { get; set; }
    }
}
