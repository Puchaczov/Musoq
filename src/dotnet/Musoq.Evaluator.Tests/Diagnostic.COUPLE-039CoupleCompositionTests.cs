using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCouple039CoupleCompositionTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void CoupledSource_InsideCte_ShouldExportUnqualifiedProjectionNames()
    {
        const string query =
            "table TypedRow { Id: int, Name: string };" +
            "couple #test.whatever with table TypedRow as TypedSource;" +
            "with FilteredData as (" +
            "select t.Id, t.Name from TypedSource() t where t.Id > 10" +
            ") select Id, Name from FilteredData order by Id;";
        var provider = CreateProvider(
            ("#test", [
                Row(("Id", 5), ("Name", "Low")),
                Row(("Id", 15), ("Name", "High")),
                Row(("Id", 25), ("Name", "Higher"))
            ]));

        var table = Run(query, provider);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int?)),
            ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [15, "High"],
            [25, "Higher"]);
    }

    [TestMethod]
    public void CoupledSources_WithInnerAndAsOfJoins_ShouldPreserveJoinSemantics()
    {
        var provider = new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#events"] =
            [
                new BasicEntity { Country = "A", Population = 100, Name = "event-100" },
                new BasicEntity { Country = "A", Population = 250, Name = "event-250" },
                new BasicEntity { Country = "B", Population = 50, Name = "event-b" }
            ],
            ["#snapshots"] =
            [
                new BasicEntity { Country = "A", Population = 80, City = "old" },
                new BasicEntity { Country = "A", Population = 200, City = "new" },
                new BasicEntity { Country = "B", Population = 100, City = "future" }
            ]
        });
        const string prefix =
            "table Events { Country: string, Population: decimal, Name: string };" +
            "table Snapshots { Country: string, Population: decimal, City: string };" +
            "couple #events.entities with table Events as Events;" +
            "couple #snapshots.entities with table Snapshots as Snapshots;";

        var inner = Run(
            prefix +
            "select e.Name, s.City from Events() e inner join Snapshots() s on e.Country = s.Country " +
            "order by e.Name, s.City;",
            provider);
        var asOf = Run(
            prefix +
            "select e.Name, s.City from Events() e asof left join Snapshots() s " +
            "on e.Country = s.Country and e.Population >= s.Population order by e.Country, e.Population;",
            provider);

        TableMaterializationTestHelper.AssertRowsUnordered(
            inner,
            ["event-100", "old"],
            ["event-100", "new"],
            ["event-250", "old"],
            ["event-250", "new"],
            ["event-b", "future"]);
        TableMaterializationTestHelper.AssertRowsInOrder(
            asOf,
            ["event-100", "old"],
            ["event-250", "new"],
            ["event-b", null]);
    }

    [TestMethod]
    public void CoupledSource_InCrossApply_ShouldAcceptCorrelatedSourceArguments()
    {
        const string query =
            "table Container { Id: int };" +
            "table Item { Value: int };" +
            "couple #containers.rows with table Container as Containers;" +
            "couple #items.rows with table Item as Items;" +
            "select c.Id, i.Value from Containers() c cross apply Items(c.Id) i " +
            "order by c.Id, i.Value;";
        var provider = CreateProvider(
            ("#containers", [Row(("Id", 1)), Row(("Id", 2))]),
            ("#items", [Row(("Value", 10)), Row(("Value", 20))]));

        var table = Run(query, provider);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Id", typeof(int?)),
            ("i.Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, 10],
            [1, 20],
            [2, 10],
            [2, 20]);
    }

    [TestMethod]
    public void CoupledSource_AsCteArgument_ShouldExposeTheCoupledOutputShape()
    {
        const string query =
            "table OutputSchema { Text: string };" +
            "couple #processor.transform with table OutputSchema as Transformer;" +
            "with InputData as (select Value from #input.source()) " +
            "select Text from Transformer(InputData);";
        var provider = CreateProvider(
            ("#processor", [Row(("Text", "transformed"))]),
            ("#input", [Row(("Value", "input"))]));

        var table = Run(query, provider);

        TableMaterializationTestHelper.AssertColumns(table, ("Text", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["transformed"]);
    }

    [TestMethod]
    public void CoupledSource_WithGroupingAndHaving_ShouldAggregateTheCoupledRows()
    {
        const string query =
            "table Sales { Product: string, Amount: decimal };" +
            "couple #sales.rows with table Sales as SalesData;" +
            "select Product, Sum(Amount) as Total from SalesData() " +
            "group by Product having Sum(Amount) > 100 order by Product;";
        var provider = CreateProvider(
            ("#sales", [
                Row(("Product", "Widget"), ("Amount", 100m)),
                Row(("Product", "Widget"), ("Amount", 200m)),
                Row(("Product", "Gizmo"), ("Amount", 50m))
            ]));

        var table = Run(query, provider);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Product", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Widget", 300m]);
    }

    [TestMethod]
    public void CoupledSources_WithSetOperators_ShouldPreserveFirstProjectionAndKeys()
    {
        const string prefix =
            "table Record { Id: int, Name: string };" +
            "couple #left.rows with table Record as SourceA;" +
            "couple #right.rows with table Record as SourceB;";
        var provider = CreateProvider(
            ("#left", [Row(("Id", 1), ("Name", "left")), Row(("Id", 2), ("Name", "only-left"))]),
            ("#right", [Row(("Id", 1), ("Name", "right")), Row(("Id", 3), ("Name", "only-right"))]));

        var union = Run(
            prefix + "select Id, Name from SourceA() union (Id) select Id, Name from SourceB();",
            provider);
        var except = Run(
            prefix + "select Id, Name from SourceA() except (Id) select Id, Name from SourceB();",
            provider);
        var intersect = Run(
            prefix + "select Id, Name from SourceA() intersect (Id) select Id, Name from SourceB();",
            provider);

        TableMaterializationTestHelper.AssertRowsUnordered(
            union,
            [1, "left"],
            [2, "only-left"],
            [3, "only-right"]);
        TableMaterializationTestHelper.AssertRowsUnordered(except, [2, "only-left"]);
        TableMaterializationTestHelper.AssertRowsUnordered(intersect, [1, "left"]);
    }

    [TestMethod]
    public void CoupledCte_WhenQualifiedInnerAliasIsUsedOutside_ShouldReportUnknownColumn()
    {
        const string query =
            "table Row { Id: int };" +
            "couple #test.rows with table Row as Source;" +
            "with Data as (select s.Id from Source() s) " +
            "select s.Id from Data;";
        var result = new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ3015_UnknownAlias,
            "qualified coupled CTE alias escaping its local scope");
        var expectedStart = query.LastIndexOf("s.Id", StringComparison.Ordinal);

        Assert.AreEqual(new TextSpan(expectedStart, 1), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "s");

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(1, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    private static Table Run(string query, ISchemaProvider provider)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            CompilationOptions);
        return compiled.Run();
    }

    private static DynamicCoupleSchemaProvider CreateProvider(
        params (string Schema, IEnumerable<dynamic> Rows)[] sources)
    {
        return new DynamicCoupleSchemaProvider(
            sources.ToDictionary(
                source => source.Schema,
                source => source.Rows,
                StringComparer.OrdinalIgnoreCase));
    }

    private static Dictionary<string, object?> Row(params (string Name, object? Value)[] values)
    {
        return values.ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
    }

    private sealed class DynamicCoupleSchemaProvider(
        IReadOnlyDictionary<string, IEnumerable<dynamic>> rowsBySchema) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            if (!rowsBySchema.TryGetValue(schema, out var rows))
                throw new InvalidOperationException($"Unknown test schema '{schema}'.");

            return new UnknownSchema(rows);
        }
    }
}
