using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
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
public sealed class DiagnosticCouple037CoupleInvocationTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void CoupledAlias_WithoutArguments_UsesNoArgumentConstructorAndCaseInsensitiveAlias()
    {
        var provider = new Couple037SchemaProvider();
        var table = Run(
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select d.DisplayName, d.RankValue from data() d;",
            provider);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("d.DisplayName", typeof(string)),
            ("d.RankValue", typeof(int?)));
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("<no-arguments>", table[0][0]);
        Assert.AreEqual(0, table[0][1]);
        CollectionAssert.AreEqual(Array.Empty<object?>(), provider.RuntimeArguments.Single());
    }

    [TestMethod]
    public void CoupledAlias_WithPositionalArguments_UsesTableAliasAndDeclaredShape()
    {
        var provider = new Couple037SchemaProvider();
        var table = Run(
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select d.DisplayName, d.RankValue from Data('positional', 12) d;",
            provider);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("d.DisplayName", typeof(string)),
            ("d.RankValue", typeof(int?)));
        Assert.AreEqual("positional", table[0][0]);
        Assert.AreEqual(12, table[0][1]);
        CollectionAssert.AreEqual(
            new object?[] { "positional", 12 },
            provider.RuntimeArguments.Single());
        Assert.IsTrue(provider.MetadataColumns.Any(columns =>
            columns.Select(column => column.ColumnName).SequenceEqual(["DisplayName", "RankValue"])));
    }

    [TestMethod]
    public void CoupledAlias_NamedArguments_AreCaseInsensitiveAndCanonicalized()
    {
        var provider = new Couple037SchemaProvider();
        const string query =
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select DisplayName, RankValue from Data(PAGESIZE: 5, SOURCEPATH: 'named');";
        var table = Run(query, provider);

        Assert.AreEqual(1, table.Count);
        AssertCapturedArguments(provider, "named", 5);
    }

    [TestMethod]
    public void CoupledAlias_MixedArguments_AllowPositionalPrefix()
    {
        var provider = new Couple037SchemaProvider();
        var table = Run(
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select DisplayName, RankValue from Data('mixed', pageSize: 6);",
            provider);

        Assert.AreEqual(1, table.Count);
        AssertCapturedArguments(provider, "mixed", 6);
    }

    [TestMethod]
    public void CoupledAlias_OmittedOptionalArgument_UsesReflectedDefault()
    {
        var provider = new Couple037SchemaProvider();
        var table = Run(
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select DisplayName, RankValue from Data('defaulted');",
            provider);

        Assert.AreEqual(1, table.Count);
        AssertCapturedArguments(provider, "defaulted", 7);
    }

    [TestMethod]
    public void CoupledAlias_PositionalAfterNamedArgumentReportsParserDiagnostic()
    {
        const string query =
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select 1 from Data(pageSize: 2, 'after');";
        var expectedSpan = SpanOf(query, "'after'");

        var diagnostic = AssertCoupledDiagnostic(
            query,
            DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
            expectedSpan,
            "positional after named coupled argument",
            DiagnosticPhase.Parse);

        Assert.AreEqual("positional-after-named", diagnostic.Arguments["argumentKind"]);
    }

    [TestMethod]
    public void CoupledAlias_UnknownNamedArgumentReportsLabelSpanAndSuggestion()
    {
        const string query =
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select 1 from Data(sourcePah: 'unknown');";
        var expectedSpan = SpanOf(query, "sourcePah");

        var diagnostic = AssertCoupledDiagnostic(
            query,
            DiagnosticCode.MQ3079_UnknownSourceArgument,
            expectedSpan,
            "unknown coupled constructor argument");

        Assert.AreEqual("sourcePah", diagnostic.Arguments["argument"]);
        StringAssert.Contains(diagnostic.Arguments["candidateParameters"], "sourcePath");
        Assert.IsTrue(diagnostic.Arguments.ContainsKey("suggestion"));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void CoupledAlias_DuplicateNamedArgumentReportsSecondLabelSpan()
    {
        const string query =
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select 1 from Data(sourcePath: 'first', SOURCEPATH: 'second');";
        var expectedSpan = SpanOf(query, "SOURCEPATH");

        var diagnostic = AssertCoupledDiagnostic(
            query,
            DiagnosticCode.MQ3080_DuplicateSourceArgument,
            expectedSpan,
            "duplicate coupled constructor argument");

        Assert.AreEqual("SOURCEPATH", diagnostic.Arguments["argument"]);
        Assert.AreEqual("sourcePath", diagnostic.Arguments["parameter"]);
    }

    [TestMethod]
    public void CoupledAlias_MissingRequiredArgumentReportsInsertionSpan()
    {
        const string query =
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select 1 from Data(pageSize: 2);";
        var expectedSpan = new TextSpan(query.IndexOf(')', StringComparison.Ordinal), 0);

        var diagnostic = AssertCoupledDiagnostic(
            query,
            DiagnosticCode.MQ3081_MissingRequiredSourceArgument,
            expectedSpan,
            "missing coupled constructor argument");

        Assert.AreEqual("sourcePath", diagnostic.Arguments["missingArgument"]);
        Assert.AreEqual("String", diagnostic.Arguments["expectedType"]);
    }

    [TestMethod]
    public void CoupledAlias_WrongArityReportsArgumentsSpanAndExpectedCounts()
    {
        const string query =
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select 1 from Data('too', 1, 2);";
        var expectedSpan = SpanOf(query, "('too', 1, 2)");

        var diagnostic = AssertCoupledDiagnostic(
            query,
            DiagnosticCode.MQ3087_InvalidCallableArity,
            expectedSpan,
            "wrong coupled constructor arity");

        StringAssert.Contains(diagnostic.Arguments["expectedCounts"], "1..2");
        StringAssert.Contains(diagnostic.Arguments["candidateSignatures"], "sourcePath");
    }

    [TestMethod]
    public void CoupledAlias_WrongArgumentTypeReportsArgumentsSpanAndCandidates()
    {
        const string query =
            "table Published { DisplayName: string, RankValue: int };" +
            "couple #couple037.items with table Published as Data;" +
            "select 1 from Data(1, 2);";
        var expectedSpan = SpanOf(query, "(1, 2)");

        var diagnostic = AssertCoupledDiagnostic(
            query,
            DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            expectedSpan,
            "wrong coupled constructor argument type");

        StringAssert.Contains(diagnostic.Arguments["actualTypes"], "Int32");
        StringAssert.Contains(diagnostic.Arguments["candidateSignatures"], "sourcePath");
    }

    private static void AssertCapturedArguments(
        Couple037SchemaProvider provider,
        string sourcePath,
        int pageSize)
    {
        var expected = new object?[] { sourcePath, pageSize };
        Assert.IsTrue(provider.MetadataArguments.Any(arguments => arguments.SequenceEqual(expected)));
        CollectionAssert.AreEqual(expected, provider.RuntimeArguments.Single());
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
                new Couple037SchemaProvider(),
                compilationOptions: CompilationOptions)
            .Analyze(query);
    }

    private static Diagnostic AssertCoupledDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        TextSpan expectedSpan,
        string context,
        DiagnosticPhase expectedPhase = DiagnosticPhase.Bind)
    {
        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(result, expectedCode, context);

        Assert.AreEqual(expectedSpan, diagnostic.Span, context);
        Assert.AreEqual(expectedPhase, diagnostic.Phase, context);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, context);
        Assert.IsTrue(diagnostic.Location.IsValid, context);
        Assert.IsTrue(diagnostic.EndLocation.IsValid, context);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code, context);
        Assert.AreEqual(expectedPhase, envelope.Phase, context);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind, context);
        Assert.AreEqual(expectedSpan.Start, envelope.Offset, context);
        Assert.AreEqual(expectedSpan.Length, envelope.Length, context);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation), context);
        Assert.IsNotEmpty(envelope.SuggestedFixes, context);
        Assert.IsNotEmpty(envelope.Actions, context);

        return diagnostic;
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{query}'.");
        return new TextSpan(start, text.Length);
    }

    private static Table Run(string query, Couple037SchemaProvider provider)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            CompilationOptions);
        return compiled.Run(CancellationToken.None);
    }

    private sealed class Couple037SchemaProvider : ISchemaProvider
    {
        public Couple037Schema Schema { get; } = new();

        public ISchema GetSchema(string schema) => Schema;

        public List<object?[]> MetadataArguments => Schema.MetadataArguments;

        public List<object?[]> RuntimeArguments => Schema.RuntimeArguments;

        public List<ISchemaColumn[]> MetadataColumns => Schema.MetadataColumns;
    }

    private sealed class Couple037Schema : SchemaBase
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new(
                "items",
                new SchemaConstructorInfo(
                    typeof(Couple037NoArgumentConstructor)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(),
                    false)),
            new(
                "items",
                new SchemaConstructorInfo(
                    typeof(Couple037ParameterizedConstructor)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(),
                    false,
                    ("sourcePath", typeof(string)),
                    ("pageSize", typeof(int))))
        ];

        public Couple037Schema()
            : base("couple037", new MethodsAggregator(new MethodsManager()))
        {
        }

        public List<object?[]> MetadataArguments { get; } = [];

        public List<object?[]> RuntimeArguments { get; } = [];

        public List<ISchemaColumn[]> MetadataColumns { get; } = [];

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) => Constructors;

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            MetadataArguments.Add(parameters.ToArray());
            var columns = metadataContext.AllColumns.ToArray();
            MetadataColumns.Add(columns);
            return new Couple037Table(columns);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            RuntimeArguments.Add(parameters.ToArray());
            var sourcePath = parameters.Length == 0 ? "<no-arguments>" : (string)parameters[0]!;
            var pageSize = parameters.Length < 2 ? 0 : (int)parameters[1]!;
            return EnsureSourceType<T, IReadOnlyDictionary<string, object?>>(
                name,
                new Couple037RowsSource(sourcePath, pageSize));
        }
    }

    private sealed class Couple037NoArgumentConstructor
    {
        public Couple037NoArgumentConstructor()
        {
        }
    }

    private sealed class Couple037ParameterizedConstructor
    {
        public Couple037ParameterizedConstructor(string sourcePath, int pageSize = 7)
        {
            _ = (sourcePath, pageSize);
        }
    }

    private sealed class Couple037Table(IEnumerable<ISchemaColumn> columns) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = columns.ToArray();

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column =>
                column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

        public SchemaTableMetadata Metadata { get; } =
            new(typeof(IReadOnlyDictionary<string, object?>));
    }

    private sealed class Couple037RowsSource(string sourcePath, int pageSize)
        : RowSourceBase<IReadOnlyDictionary<string, object?>>
    {
        protected override void CollectChunks(IChunkWriter<IReadOnlyDictionary<string, object?>> writer)
        {
            writer.Write(
            [
                new Dictionary<string, object?>
                {
                    ["DisplayName"] = sourcePath,
                    ["RankValue"] = pageSize
                }
            ]);
        }
    }
}
