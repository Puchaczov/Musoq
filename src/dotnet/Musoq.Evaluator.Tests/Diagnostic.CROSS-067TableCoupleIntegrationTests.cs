using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.ReadModifiers;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCross067TableCoupleIntegrationTests : BasicEntityTestBase
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void TableCouple_CteSetApplyAndAggregation_ShouldHonorNamedArgumentsAndDescOrdering()
    {
        const string query = """
            table Sale { Id: int, Product: string, Amount: decimal };
            table Item { SaleId: int, Label: string, Cost: decimal };
            couple #cross067.sales with table Sale as SalesData;
            couple #cross067.items with table Item as Items;
            with filtered as (
                select s.Id as Id, s.Product as Product, s.Amount as Amount
                from SalesData(SOURCEPATH: 'north', PAGESIZE: 2) s
                where s.Amount >= 100
            ),
            joined as (
                select f.Product as Product, i.Cost as Cost
                from filtered f
                cross apply Items(f.Id) i
            ),
            combined as (
                select Product, Cost from joined
                union all (Product, Cost)
                select s.Product as Product, 0::Decimal as Cost
                from SalesData(sourcePath: 'south', pageSize: 1) s
                where s.Amount < 100
            )
            select Product, Sum(Cost) as TotalCost
            from combined
            group by Product
            having Sum(Cost) > 0
            order by TotalCost desc, Product asc
            """;
        var provider = new Cross067SchemaProvider();

        var table = TableMaterializationTestHelper.Materialize(
            Run(query, provider).Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Product", typeof(string)),
            ("TotalCost", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Widget", 45m],
            ["Gadget", 20m]);

        Assert.IsTrue(
            provider.Schema.Arguments.Any(arguments => arguments.SequenceEqual(new object?[] { "north", 2 })),
            "The named north source arguments were not canonicalized and forwarded.");
        Assert.IsTrue(
            provider.Schema.Arguments.Any(arguments => arguments.SequenceEqual(new object?[] { "south", 1 })),
            "The named south source arguments were not canonicalized and forwarded.");
        CollectionAssert.AreEquivalent(
            new object?[] { 1, 2, 4 },
            provider.Schema.Arguments
                .Where(arguments => arguments.Length == 1)
                .Select(arguments => arguments[0])
                .ToArray());
    }

    [TestMethod]
    public void CoupledSettingsProfiles_CteSetAndAggregation_ShouldStayIsolatedAndDescRedacted()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new ProfileResolver();
        const string query = """
            table Shape { Token: string };
            couple #settings.items with table Shape and settings prod as Prod;
            couple #settings.items with settings staging and table Shape as Stage;
            with combined as (
                select p.Token as Token from Prod() p
                union all (Token)
                select s.Token as Token from Stage() s
            )
            select c.Token, Count(c.Token) as Uses
            from combined c
            group by c.Token
            order by Uses desc, c.Token desc
            """;

        var table = TableMaterializationTestHelper.Materialize(
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                provider,
                LoggerResolver,
                new CompilationOptions(sourceRuntimeSettingsResolver: resolver))
            .Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Token", typeof(string)),
            ("Uses", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["staging-token", 1L],
            ["prod-token", 1L]);
        CollectionAssert.AreEquivalent(new[] { "prod", "staging" }, resolver.ProfileNames.ToArray());

        var descTable = TableMaterializationTestHelper.Materialize(
            InstanceCreator.CompileForExecution(
                "couple #settings.items with settings prod as Prod;desc settings prod;",
                Guid.NewGuid().ToString(),
                provider,
                LoggerResolver,
                new CompilationOptions(sourceRuntimeSettingsResolver: resolver))
            .Run(TestContext.CancellationToken));

        Assert.IsFalse(
            descTable.Rows.SelectMany(static row => row.Values).Any(value => Equals(value, "prod-token")),
            "DESC SETTINGS must not expose resolved setting values.");
        Assert.IsTrue(resolver.ProfileNames.Contains("prod"));
    }

    [TestMethod]
    public void SourceContractDiagnostics_InsideCteIntegration_ShouldRetainWarningAndErrorMetadata()
    {
        const string warningQuery =
            "table RecordsShape { Name: string encoding 'windows-1250' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "with named as (select r.Name as Name from Records() r)" +
            "select Name, Count(Name) as Uses from named group by Name order by Uses desc;";
        var warningResult = InstanceCreator.CompileWithDiagnostics(
            warningQuery,
            Guid.NewGuid().ToString(),
            new ReadModifiersSchemaProvider(
                [new Dictionary<string, object?> { ["Name"] = "warning" }],
                ReadModifiersValidationMode.LenientUnsupportedModifiers),
            LoggerResolver,
            CompilationOptions);

        Assert.IsTrue(warningResult.Succeeded);
        var warning = warningResult.Warnings.Single();
        Assert.AreEqual(DiagnosticCode.MQ5013_SourceContractWarning, warning.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind);
        Assert.AreEqual(SpanOf(warningQuery, "encoding 'windows-1250'"), warning.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(warning.ContextSnippet));
        var warningEnvelope = warningResult.ToAllEnvelopes().Single();
        Assert.AreEqual(warning.Span.Start, warningEnvelope.Offset);
        Assert.AreEqual(warning.Span.Length, warningEnvelope.Length);
        Assert.AreEqual("Table/Couple Spec - Source Contract Diagnostics", warningEnvelope.DocsReference);
        Assert.IsNotEmpty(warningEnvelope.SuggestedFixes);
        Assert.IsNotEmpty(warningEnvelope.Actions);

        const string errorQuery =
            "table RecordsShape { Amount: decimal };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "with amounts as (select r.Amount as Amount from Records() r)" +
            "select Amount, Count(Amount) as Uses from amounts group by Amount;";
        var errorResult = InstanceCreator.CompileWithDiagnostics(
            errorQuery,
            Guid.NewGuid().ToString(),
            new ReadModifiersSchemaProvider(
                [new Dictionary<string, object?> { ["Amount"] = "12.50" }],
                ReadModifiersValidationMode.ValidateSourceKinds,
                new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Amount"] = typeof(string)
                }),
            LoggerResolver,
            CompilationOptions);

        Assert.IsFalse(errorResult.Succeeded);
        var error = errorResult.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, error.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, error.SourceKind);
        Assert.AreEqual(SpanOf(errorQuery, "Amount: decimal"), error.Span);
        var errorEnvelope = errorResult.ToEnvelopes().Single();
        Assert.AreEqual(error.Span.Start, errorEnvelope.Offset);
        Assert.AreEqual(error.Span.Length, errorEnvelope.Length);
        Assert.AreEqual("Table/Couple Spec - Source Contract Diagnostics", errorEnvelope.DocsReference);
        Assert.IsNotEmpty(errorEnvelope.SuggestedFixes);
        Assert.IsNotEmpty(errorEnvelope.Actions);
    }

    [TestMethod]
    public void CoupledAliasEscapingCteScope_ShouldReportStructuredUnknownAlias()
    {
        const string query =
            "table Sale { Id: int, Product: string, Amount: decimal };" +
            "couple #cross067.sales with table Sale as SalesData;" +
            "with Data as (select s.Id from SalesData('north') s)" +
            "select s.Id from Data;";
        var diagnostic = new QueryAnalyzer(
                new Cross067SchemaProvider(),
                compilationOptions: CompilationOptions)
            .Analyze(query)
            .Errors
            .Single();

        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, diagnostic.Code);
        var expectedSpan = new TextSpan(query.LastIndexOf("s.Id", StringComparison.Ordinal), 1);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedSpan.Start, envelope.Offset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    private static CompiledQuery Run(string query, Cross067SchemaProvider provider)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            CompilationOptions);
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in the query.");
        return new TextSpan(start, text.Length);
    }

    private sealed class ProfileResolver : ISourceRuntimeSettingsResolver
    {
        public List<string?> ProfileNames { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            ProfileNames.Add(request.ProfileName);
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TOKEN"] = $"{request.ProfileName}-token"
            };
        }
    }

    private sealed class Cross067SchemaProvider : ISchemaProvider
    {
        public Cross067Schema Schema { get; } = new();

        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "cross067", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "#cross067", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(schema);
            }

            return Schema;
        }
    }

    private sealed class Cross067Schema : SchemaBase
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new(
                "sales",
                new SchemaConstructorInfo(
                    typeof(SalesConstructor)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(),
                    false,
                    ("sourcePath", typeof(string)),
                    ("pageSize", typeof(int)))),
            new(
                "items",
                new SchemaConstructorInfo(
                    typeof(ItemsConstructor)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(),
                    false,
                    ("saleId", typeof(int?))))
        ];

        public Cross067Schema()
            : base("cross067", new MethodsAggregator(new MethodsManager()))
        {
        }

        public List<object?[]> Arguments { get; } = [];

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) => Constructors;

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new Cross067Table(metadataContext.AllColumns.ToArray());
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            Arguments.Add(parameters.ToArray());
            var rows = name.Equals("sales", StringComparison.OrdinalIgnoreCase)
                ? CreateSalesRows()
                : name.Equals("items", StringComparison.OrdinalIgnoreCase)
                    ? CreateItemRows(parameters)
                    : throw new NotSupportedException(name);
            return EnsureSourceType<T, Cross067Entity>(name, new Cross067RowSource(rows));
        }

        private static IReadOnlyList<Cross067Entity> CreateSalesRows() =>
        [
            new() { Id = 1, Product = "Widget", Amount = 100m },
            new() { Id = 2, Product = "Widget", Amount = 200m },
            new() { Id = 3, Product = "Gizmo", Amount = 50m },
            new() { Id = 4, Product = "Gadget", Amount = 150m }
        ];

        private static IReadOnlyList<Cross067Entity> CreateItemRows(object?[] parameters)
        {
            var saleId = parameters.Length == 0 || parameters[0] is null
                ? 0
                : Convert.ToInt32(parameters[0]);
            return saleId switch
            {
                1 =>
                [
                    new() { SaleId = 1, Label = "primary", Cost = 10m },
                    new() { SaleId = 1, Label = "secondary", Cost = 5m }
                ],
                2 => [new() { SaleId = 2, Label = "primary", Cost = 30m }],
                4 => [new() { SaleId = 4, Label = "primary", Cost = 20m }],
                _ => []
            };
        }
    }

    private sealed class SalesConstructor(string sourcePath, int pageSize = 100)
    {
        public string SourcePath { get; } = sourcePath;

        public int PageSize { get; } = pageSize;
    }

    private sealed class ItemsConstructor(int? saleId)
    {
        public int? SaleId { get; } = saleId;
    }

    private sealed class Cross067Table(ISchemaColumn[] columns) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = columns;

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(Cross067Entity));
    }

    private sealed class Cross067RowSource(IReadOnlyList<Cross067Entity> rows)
        : RowSourceBase<Cross067Entity>
    {
        protected override void CollectChunks(IChunkWriter<Cross067Entity> writer)
        {
            writer.Write(rows);
        }
    }

    public sealed class Cross067Entity
    {
        public int Id { get; init; }

        public string? Product { get; init; }

        public decimal Amount { get; init; }

        public int SaleId { get; init; }

        public string? Label { get; init; }

        public decimal Cost { get; init; }
    }
}
