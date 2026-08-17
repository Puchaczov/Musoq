using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SuspiciousOrdinaryStringEscapeContextTests
{
    [TestMethod]
    public void Analyze_RelativePathSourceArgument_ReportsOneWarning()
    {
        var result = Analyze("select 1 from #context.files('some\\text', true)");

        AssertWarning(result, "some\\text");
    }

    [TestMethod]
    public void Analyze_NamedRelativePathSourceArgument_ReportsOneWarning()
    {
        var result = Analyze("select 1 from #context.files(recursive: true, path: 'some\\text')");

        AssertWarning(result, "some\\text");
    }

    [TestMethod]
    public void Analyze_RelativePathFunctionArgument_ReportsOneWarning()
    {
        var result = Analyze("select ReadFile('some\\text') from #context.rows()");

        AssertWarning(result, "some\\text");
    }

    [TestMethod]
    public void Analyze_PathScriptVariableInitializer_ReportsOneWarning()
    {
        var result = Analyze("let path: string = 'some\\text'; select $path from #context.rows()");

        AssertWarning(result, "some\\text");
    }

    [TestMethod]
    public void Analyze_NonPathContextsRemainQuiet()
    {
        var projection = Analyze("select 'some\\text' from #context.rows()");
        var message = Analyze("select FormatMessage('Hello\\nWorld') from #context.rows()");
        var raw = Analyze("select ReadFile(r'some\\text') from #context.rows()");
        var doubled = Analyze("select ReadFile('some\\\\text') from #context.rows()");

        Assert.AreEqual(0, projection.Warnings.Count());
        Assert.AreEqual(0, message.Warnings.Count());
        Assert.AreEqual(0, raw.Warnings.Count());
        Assert.AreEqual(0, doubled.Warnings.Count());
    }

    [TestMethod]
    public void Analyze_ContextualUnknownAndIntentionalEscapesRemainQuiet()
    {
        var unknown = Analyze("select ReadFile('some\\q') from #context.rows()");
        var intentional = Analyze("select ReadFile('\\n') from #context.rows()");
        var malformed = Analyze("select ReadFile('some\\u123') from #context.rows()");

        Assert.AreEqual(0, unknown.Warnings.Count());
        Assert.AreEqual(0, intentional.Warnings.Count());
        Assert.IsTrue(malformed.Errors.Any(static error =>
            error.Code == DiagnosticCode.MQ1004_InvalidEscapeSequence));
        Assert.AreEqual(0, malformed.Warnings.Count());
    }

    [TestMethod]
    public void Analyze_RootedPathPassedToPathParameter_IsReportedOnlyByParser()
    {
        var result = Analyze("select ReadFile('C:\\new\\test') from #context.rows()");

        AssertWarning(result, "C:\\new\\test");
    }

    [TestMethod]
    public void Analyze_AmbiguousSourceMetadata_DoesNotGuessPathIntent()
    {
        var result = new QueryAnalyzer(new ContextSchemaProvider()).Analyze(
            "select 1 from #ambiguous.files('some\\text')");

        Assert.IsFalse(result.HasErrors, string.Join("\n", result.Errors));
        Assert.AreEqual(0, result.Warnings.Count());
    }

    [TestMethod]
    public void Analyze_MissingSourceMetadata_DoesNotGuessPathIntent()
    {
        var result = new QueryAnalyzer(new ContextSchemaProvider()).Analyze(
            "select 1 from #metadatafree.files('some\\text')");

        Assert.IsFalse(result.HasErrors, string.Join("\n", result.Errors));
        Assert.AreEqual(0, result.Warnings.Count());
    }

    [TestMethod]
    public void ValidateSyntax_RelativePathHasNoSemanticContext()
    {
        var result = new QueryAnalyzer(new ContextSchemaProvider()).ValidateSyntax(
            "select ReadFile('some\\text') from #context.rows()");

        Assert.AreEqual(0, result.Warnings.Count());
    }

    private static QueryAnalysisResult Analyze(string query) =>
        new QueryAnalyzer(new ContextSchemaProvider()).Analyze(query);

    private static void AssertWarning(QueryAnalysisResult result, string sourceValue)
    {
        Assert.IsTrue(result.IsParsed, string.Join("\n", result.Diagnostics));
        Assert.IsFalse(result.HasErrors, string.Join("\n", result.Errors));

        var warnings = result.Warnings.Where(static warning =>
            warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape).ToArray();
        Assert.AreEqual(1, warnings.Length, string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code} {diagnostic.Message} @{diagnostic.Location.Offset}")));
        Assert.AreEqual(DiagnosticSeverity.Warning, warnings[0].Severity);
        Assert.IsTrue(warnings[0].Message.Contains("raw literal", StringComparison.Ordinal));

        var expectedStart = result.Diagnostics.First().Location.Offset;
        Assert.IsTrue(warnings[0].Message.Contains("\\", StringComparison.Ordinal));
        Assert.IsTrue(sourceValue.Length > 0);
        Assert.IsTrue(warnings[0].Location.Offset >= expectedStart);
    }

    private sealed class ContextSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => schema switch
        {
            "#ambiguous" => new AmbiguousSchema(),
            "#metadatafree" => new MetadataFreeSchema(),
            _ => new ContextSchema()
        };
    }

    private class ContextSchema : SchemaBase
    {
        private static readonly Lazy<MethodsAggregator> CachedLibrary = new(CreateLibrary);
        private static readonly SchemaMethodInfo[] FilesConstructors =
        [
            new(
                "files",
                new SchemaConstructorInfo(
                    typeof(PathTable).GetConstructor([typeof(string), typeof(bool)])!,
                    false,
                    ("path", typeof(string)),
                    ("recursive", typeof(bool))))
        ];

        public ContextSchema()
            : base("context", CachedLibrary.Value)
        {
        }

        public override SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext) =>
            methodName.Equals("files", StringComparison.OrdinalIgnoreCase)
                ? FilesConstructors
                : [new SchemaMethodInfo("rows", SchemaConstructorInfo.Empty())];

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => new ContextTable();

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters) => new OneRowSource<T>();

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new ContextLibrary());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class AmbiguousSchema : ContextSchema
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new(
                "files",
                new SchemaConstructorInfo(
                    typeof(AmbiguousTable).GetConstructor([typeof(string)])!,
                    false,
                    ("path", typeof(string)))),
            new(
                "files",
                new SchemaConstructorInfo(
                    typeof(AmbiguousTable).GetConstructor([typeof(string)])!,
                    false,
                    ("message", typeof(string))))
        ];

        public override SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext) =>
            methodName.Equals("files", StringComparison.OrdinalIgnoreCase) ? Constructors : [];
    }

    private sealed class MetadataFreeSchema : ContextSchema
    {
        public override SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext) => [];
    }

    private sealed class ContextLibrary : LibraryBase
    {
        [BindableMethod]
        public string ReadFile(string filePath) => filePath;

        [BindableMethod]
        public string FormatMessage(string message) => message;
    }

    private sealed class OneRowSource<T> : RowSource<T>
    {
        public override IEnumerable<IReadOnlyList<T>> Chunks =>
            new[] { (IReadOnlyList<T>)new[] { default(T)! } };
    }

    private class ContextTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [];

        public SchemaTableMetadata Metadata { get; } = new(typeof(ContextRow));

        public ISchemaColumn? GetColumnByName(string name) => null;

        public ISchemaColumn[] GetColumnsByName(string name) => [];
    }

    private sealed class PathTable(string path, bool recursive) : ContextTable
    {
        public string Path { get; } = path;

        public bool Recursive { get; } = recursive;
    }

    private sealed class AmbiguousTable(string value) : ContextTable
    {
        public string Value { get; } = value;
    }

    private sealed class ContextRow;
}
