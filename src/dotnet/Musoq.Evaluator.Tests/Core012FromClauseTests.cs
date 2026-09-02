using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core012FromClauseTests : BasicEntityTestBase
{
    [TestMethod]
    public void CteReference_NaturalNameRemainsVisibleAlongsideAnotherSource()
    {
        const string query = """
            with source as (
                select a.City from #A.Entities() a
            )
            select source.City, b.City
            from source
            inner join #B.Entities() b on source.City = b.City
            order by source.City
            """;

        var table = CreateAndRunVirtualMachine(query, CreateFromSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("source.City", typeof(string)),
            ("b.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN", "BERLIN"], ["WARSAW", "WARSAW"]);
    }

    [TestMethod]
    public void CteReference_AliasHidesOriginalNameWithRepairableDiagnostic()
    {
        const string query = """
            with p as (
                select a.City from #A.Entities() a
            )
            select p.City from p c
            """;

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateFromSources()));
        var aliasStart = query.LastIndexOf("p.City", StringComparison.Ordinal);
        var envelope = exception.PrimaryEnvelope;

        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(aliasStart, envelope.Offset);
        Assert.AreEqual(1, envelope.Length);
        Assert.AreEqual("p", envelope.Arguments["alias"]);
        Assert.AreEqual("c", envelope.Arguments["availableAliases"]);
        AssertHasGuidance(exception);

        var quickFix = envelope.Actions.Single(action => action.Kind == DiagnosticActionKind.QuickFix);
        Assert.AreEqual(new TextSpan(aliasStart, 1), quickFix.TextEdit!.Span);
        Assert.AreEqual("c", quickFix.TextEdit.NewText);
    }

    [TestMethod]
    public void CteDefinition_CannotConsumeOuterAliasAndReportsReferenceSpan()
    {
        const string query = """
            with p as (
                select b.City from #B.Entities() b
                where b.Country = a.Country
            )
            select a.City from #A.Entities() a
            where a.City in (select City from p)
            """;

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateFromSources()));
        var referenceStart = query.IndexOf("a.Country", StringComparison.Ordinal);
        var envelope = exception.PrimaryEnvelope;

        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(referenceStart, envelope.Offset);
        Assert.AreEqual(1, envelope.Length);
        Assert.AreEqual("a", envelope.Arguments["alias"]);
        Assert.AreEqual("b", envelope.Arguments["availableAliases"]);
        StringAssert.Contains(envelope.Message, "Unknown alias 'a'");
        AssertHasGuidance(exception);
        var quickFix = envelope.Actions.Single(action => action.Kind == DiagnosticActionKind.QuickFix);
        Assert.AreEqual(new TextSpan(referenceStart, 1), quickFix.TextEdit!.Span);
        Assert.AreEqual("b", quickFix.TextEdit.NewText);
    }

    [TestMethod]
    public void DerivedTable_OuterReferenceRequiresApplyWithRepairableDiagnostic()
    {
        const string query = """
            select a.City, d.City from #A.Entities() a
            inner join (
                select b.City, b.Country from #B.Entities() b
                where b.Country = a.Country
            ) d on a.Country = d.Country
            """;

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateFromSources()));
        var opening = query.IndexOf('(', query.IndexOf("inner join", StringComparison.OrdinalIgnoreCase));
        var closing = query.IndexOf(") d", opening, StringComparison.Ordinal);
        var envelope = exception.PrimaryEnvelope;

        Assert.AreEqual(DiagnosticCode.MQ2024_InvalidSubquery, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(opening, envelope.Offset);
        Assert.AreEqual(closing - opening + 1, envelope.Length);
        Assert.AreEqual("non-lateral-derived-table", envelope.Arguments["constraint"]);
        Assert.AreEqual("d", envelope.Arguments["alias"]);
        Assert.AreEqual("a", envelope.Arguments["outerAlias"]);
        Assert.AreEqual("CROSS APPLY, OUTER APPLY", envelope.Arguments["allowedOperators"]);
        StringAssert.Contains(envelope.Message, "Plain derived tables are not lateral");
        StringAssert.Contains(envelope.Message, "outer alias 'a'");
        AssertHasGuidance(exception);
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    [TestMethod]
    public void CoupledSource_UsesDeclaredTableShapeAndUnderlyingRows()
    {
        const string query = """
            table Snapshot {
                City: string,
                Population: decimal
            };
            couple #A.Entities with table Snapshot as CoupledRows;
            select rows.City, rows.Population from CoupledRows() rows
            order by rows.City
            """;

        var table = CreateAndRunVirtualMachine(query, CreateFromSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("rows.City", typeof(string)),
            ("rows.Population", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", 250m],
            ["WARSAW", 500m]);
    }

    [TestMethod]
    public void HostProvidedSystemRange_IsEndExclusive()
    {
        const string query = "select Value from system.range(1, 5)";

        var table = CreateAndRunVirtualMachine(
                query,
                schemaProvider: new RangeSchemaProvider())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int)));
        CollectionAssert.AreEqual(
            new object[] { 1, 2, 3, 4 },
            table.Select(row => row.Values[0]).ToArray());
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateFromSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("WARSAW", "POLAND", 500),
                new BasicEntity("BERLIN", "GERMANY", 250)
            ],
            ["#B"] =
            [
                new BasicEntity("WARSAW", "POLAND", 100),
                new BasicEntity("BERLIN", "GERMANY", 200),
                new BasicEntity("PARIS", "FRANCE", 300)
            ]
        };
    }

    private sealed class RangeSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "#system", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unexpected schema '{schema}'.");

            return new RangeSchema();
        }
    }

    private sealed class RangeSchema : SchemaBase
    {
        public RangeSchema()
            : base("system", new MethodsAggregator(new MethodsManager()))
        {
            AddSource<RangeSource>("range");
            AddTable<RangeTable>("range");
        }
    }

    public sealed class RangeSource(int start, int end) : RowSourceBase<RangeRow>
    {
        private readonly RangeRow[] _rows = Enumerable
            .Range(start, Math.Max(0, end - start))
            .Select(static value => new RangeRow(value))
            .ToArray();

        protected override void CollectChunks(IChunkWriter<RangeRow> writer)
        {
            writer.Write(_rows);
        }
    }

    public sealed class RangeTable : ISchemaTable
    {
        public RangeTable(int start, int end)
        {
        }

        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(RangeRow.Value), 0, typeof(int))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(RangeRow));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column =>
                string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column =>
                string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
    }

    public sealed class RangeRow(int value)
    {
        public int Value { get; } = value;
    }
}
