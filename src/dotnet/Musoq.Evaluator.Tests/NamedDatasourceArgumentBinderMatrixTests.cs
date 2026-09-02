using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Parser;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NamedDatasourceArgumentBinderMatrixTests
{
    [TestMethod]
    public void DuplicateNamedArguments_ReportStableDiagnostic()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(value: 1, VALUE: 2)",
            new MatrixSchemaProvider(MatrixSignatures.Required, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3080_DuplicateSourceArgument, exception.Code);
    }

    [TestMethod]
    public void DuplicateNamedArguments_ReportRepairableFactsAtDuplicateLabel()
    {
        const string validQuery = "select 1 from #matrix.source(value: 1)";
        var query = validQuery.Replace("value: 1", "value: 1, VALUE: 2", StringComparison.Ordinal);
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            query,
            new MatrixSchemaProvider(MatrixSignatures.Required, _ => { })));

        var duplicateSpan = new TextSpan(query.IndexOf("VALUE", StringComparison.Ordinal), "VALUE".Length);
        Assert.AreEqual(DiagnosticCode.MQ3080_DuplicateSourceArgument, exception.Code);
        Assert.AreEqual(duplicateSpan, exception.Span);

        var envelope = MusoqErrorEnvelope.FromException(exception, query);
        Assert.AreEqual(duplicateSpan.Start, envelope.Offset);
        Assert.AreEqual(duplicateSpan.Length, envelope.Length);
        Assert.AreEqual("VALUE", envelope.Arguments["argument"]);
        Assert.AreEqual("value", envelope.Arguments["parameter"]);
        Assert.AreEqual("value", envelope.Arguments["candidateParameters"]);
        StringAssert.Contains(envelope.Message, "more than once");
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    [TestMethod]
    public void PositionalAndNamedDuplicate_ReportStableDiagnostic()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(1, value: 2)",
            new MatrixSchemaProvider(MatrixSignatures.Required, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3080_DuplicateSourceArgument, exception.Code);
    }

    [TestMethod]
    public void MissingRequiredArgument_ReportStableDiagnostic()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(value: 1)",
            new MatrixSchemaProvider(MatrixSignatures.RequiredTwo, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3081_MissingRequiredSourceArgument, exception.Code);
    }

    [TestMethod]
    public void UnknownNamedArgumentTypo_ReportsRepairableSuggestionAndQuickFix()
    {
        const string validQuery = "select 1 from #matrix.source(value: 1)";
        var query = validQuery.Replace("value", "vlaue", StringComparison.Ordinal);
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            query,
            new MatrixSchemaProvider(MatrixSignatures.Required, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3079_UnknownSourceArgument, exception.Code);
        Assert.IsNotNull(exception.Span);

        var typoSpan = new TextSpan(query.IndexOf("vlaue", StringComparison.Ordinal), "vlaue".Length);
        Assert.AreEqual(typoSpan, exception.Span!.Value);

        var envelope = MusoqErrorEnvelope.FromException(exception, query);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(typoSpan.Start, envelope.Offset);
        Assert.AreEqual(typoSpan.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
        Assert.IsTrue(envelope.Arguments.TryGetValue("argument", out var argument));
        Assert.AreEqual("vlaue", argument);
        Assert.IsTrue(envelope.Arguments.TryGetValue("suggestion", out var suggestion));
        Assert.AreEqual("value", suggestion);
        StringAssert.Contains(envelope.Message, "vlaue");
        StringAssert.Contains(envelope.Message, "Did you mean 'value'?");

        var action = envelope.Actions.Single(candidate => candidate.Kind == DiagnosticActionKind.QuickFix);
        Assert.IsNotNull(action.TextEdit);
        Assert.AreEqual(typoSpan, action.TextEdit!.Span);
        Assert.AreEqual("value", action.TextEdit.NewText);

        var repairedQuery = query.Remove(action.TextEdit.Span.Start, action.TextEdit.Span.Length)
            .Insert(action.TextEdit.Span.Start, action.TextEdit.NewText);
        Assert.AreEqual(validQuery, repairedQuery);
        Analyze(repairedQuery, new MatrixSchemaProvider(MatrixSignatures.Required, _ => { }));
    }

    [TestMethod]
    public void UnknownNamedArgumentInMultiParameterSignature_OffersSafeQuickFix()
    {
        const string validQuery = "select 1 from #matrix.source(value: 1, other: 2)";
        var query = validQuery.Replace("other: 2", "othre: 2", StringComparison.Ordinal);
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            query,
            new MatrixSchemaProvider(MatrixSignatures.RequiredTwo, _ => { })));

        var typoSpan = new TextSpan(query.IndexOf("othre", StringComparison.Ordinal), "othre".Length);
        var envelope = MusoqErrorEnvelope.FromException(exception, query);
        Assert.AreEqual(DiagnosticCode.MQ3079_UnknownSourceArgument, envelope.Code);
        Assert.AreEqual("othre", envelope.Arguments["argument"]);
        Assert.AreEqual("other", envelope.Arguments["suggestion"]);
        Assert.AreEqual(typoSpan.Start, envelope.Offset);
        Assert.AreEqual(typoSpan.Length, envelope.Length);

        var action = envelope.Actions.Single(candidate => candidate.Kind == DiagnosticActionKind.QuickFix);
        Assert.IsNotNull(action.TextEdit);
        Assert.AreEqual(typoSpan, action.TextEdit!.Span);
        Assert.AreEqual("other", action.TextEdit.NewText);

        var repairedQuery = query.Remove(action.TextEdit.Span.Start, action.TextEdit.Span.Length)
            .Insert(action.TextEdit.Span.Start, action.TextEdit.NewText);
        Assert.AreEqual(validQuery, repairedQuery);
        Analyze(repairedQuery, new MatrixSchemaProvider(MatrixSignatures.RequiredTwo, _ => { }));
    }

    [TestMethod]
    public void MissingRequiredArgument_ReportsInsertionSpanAndNamedFact()
    {
        const string validQuery = "select 1 from #matrix.source(value: 1, other: 2)";
        var query = validQuery.Replace(", other: 2", string.Empty, StringComparison.Ordinal);
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            query,
            new MatrixSchemaProvider(MatrixSignatures.RequiredTwo, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3081_MissingRequiredSourceArgument, exception.Code);
        var insertionSpan = new TextSpan(query.IndexOf(')'), 0);
        Assert.AreEqual(insertionSpan, exception.Span);

        var envelope = MusoqErrorEnvelope.FromException(exception, query);
        Assert.AreEqual(insertionSpan.Start, envelope.Offset);
        Assert.AreEqual(0, envelope.Length);
        Assert.IsTrue(envelope.Arguments.TryGetValue("missingArgument", out var missingArgument));
        Assert.AreEqual("other", missingArgument);
        Assert.IsTrue(envelope.Arguments.TryGetValue("candidateParameters", out var candidateParameters));
        StringAssert.Contains(candidateParameters, "other");
        StringAssert.Contains(envelope.Message, "other");
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
    }

    [TestMethod]
    public void MetadataLessSource_RejectsNamedArguments()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(value: 1)",
            new MatrixSchemaProvider(MatrixSignatures.MetadataLess, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata, exception.Code);
    }

    [TestMethod]
    public void MetadataLessSource_ReportsNamedLabelAndBindingRequirement()
    {
        const string validQuery = "select 1 from #matrix.source(1)";
        var query = validQuery.Replace("1)", "value: 1)", StringComparison.Ordinal);
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            query,
            new MatrixSchemaProvider(MatrixSignatures.MetadataLess, _ => { })));

        var argumentSpan = new TextSpan(query.IndexOf("value", StringComparison.Ordinal), "value".Length);
        Assert.AreEqual(DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata, exception.Code);
        Assert.AreEqual(argumentSpan, exception.Span);

        var envelope = MusoqErrorEnvelope.FromException(exception, query);
        Assert.AreEqual(argumentSpan.Start, envelope.Offset);
        Assert.AreEqual(argumentSpan.Length, envelope.Length);
        Assert.AreEqual("value", envelope.Arguments["argument"]);
        Assert.AreEqual("true", envelope.Arguments["requiresMetadata"]);
        StringAssert.Contains(envelope.Message, "reflected constructor metadata");
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    [TestMethod]
    public void MetadataLessSource_PreservesPositionalCompatibility()
    {
        var captured = new List<object?[]>();

        Analyze(
            "select 1 from #matrix.source(1)",
            new MatrixSchemaProvider(MatrixSignatures.MetadataLess, values => captured.Add(values)));

        Assert.HasCount(1, captured);
        CollectionAssert.AreEqual(new object?[] { 1 }, captured[0]);
    }

    [TestMethod]
    public void HiddenSourceExecutionContext_IsExcludedFromNamedSignature()
    {
        var captured = new List<object?[]>();

        Analyze(
            "select 1 from #matrix.source(value: 1)",
            new MatrixSchemaProvider(MatrixSignatures.HiddenContext, values => captured.Add(values)));

        Assert.HasCount(1, captured);
        CollectionAssert.AreEqual(new object?[] { 1 }, captured[0]);
    }

    [TestMethod]
    public void MismatchedReflectedMetadata_RemainsPositionalOnly()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(value: 'text')",
            new MatrixSchemaProvider(MatrixSignatures.MismatchedReflection, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata, exception.Code);
    }

    [TestMethod]
    public void UnqualifiedFunctionShapedSource_ReportsInvalidNamedArgument()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from MissingSource(value: 1)",
            new MatrixSchemaProvider(MatrixSignatures.Required, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ2034_InvalidNamedSourceArgument, exception.Code);
    }

    [TestMethod]
    public void AssignableOverloadsWithEqualScore_ReportDeterministicAmbiguity()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(value: 'text')",
            new MatrixSchemaProvider(MatrixSignatures.Ambiguous, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3089_AmbiguousCallableOverload, exception.Code);
        StringAssert.Contains(exception.Message, "IComparable");
        Assert.IsFalse(exception.Message.Contains("System.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AssignableOverloadsWithEqualScore_ReportCandidatesWithoutUnsafeEdit()
    {
        const string query = "select 1 from #matrix.source(value: 'text')";
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            query,
            new MatrixSchemaProvider(MatrixSignatures.Ambiguous, _ => { })));

        var argumentsStart = query.IndexOf('(');
        var argumentsSpan = new TextSpan(argumentsStart, query.IndexOf(')') - argumentsStart + 1);
        var envelope = MusoqErrorEnvelope.FromException(exception, query);

        Assert.AreEqual(DiagnosticCode.MQ3089_AmbiguousCallableOverload, envelope.Code);
        Assert.AreEqual(argumentsSpan.Start, envelope.Offset);
        Assert.AreEqual(argumentsSpan.Length, envelope.Length);
        Assert.AreEqual("source", envelope.Arguments["callable"]);
        Assert.AreEqual("String", envelope.Arguments["actualTypes"]);
        StringAssert.Contains(envelope.Arguments["candidateSignatures"], "IComparable");
        StringAssert.Contains(envelope.Arguments["candidateSignatures"], "IConvertible");
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    [TestMethod]
    public void ExactOverloadWinsOverAssignableOverload()
    {
        var captured = new List<object?[]>();
        Analyze(
            "select 1 from #matrix.source(value: 'text')",
            new MatrixSchemaProvider(MatrixSignatures.ExactAndAssignable, values => captured.Add(values)));

        Assert.HasCount(1, captured);
        CollectionAssert.AreEqual(new object?[] { "text" }, captured[0]);
    }

    [TestMethod]
    public void FullyPositionalFullArityMatchKeepsLegacyResolution()
    {
        var captured = new List<object?[]>();
        Analyze(
            "select 1 from #matrix.source(1)",
            new MatrixSchemaProvider(MatrixSignatures.PositionalOverloads, values => captured.Add(values)));

        Assert.HasCount(1, captured);
        CollectionAssert.AreEqual(new object?[] { 1 }, captured[0]);
    }

    [TestMethod]
    public void IncompatibleArgumentReportsCannotResolveMethod()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(value: 'text')",
            new MatrixSchemaProvider(MatrixSignatures.IntegerOnly, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, exception.Code);
    }

    [TestMethod]
    public void WrongSourceArity_ReportsInvalidCallableArity()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source()",
            new MatrixSchemaProvider(MatrixSignatures.Required, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3087_InvalidCallableArity, exception.Code);
    }

    [TestMethod]
    public void PublicSchemaContractsRemainDictionaryFree()
    {
        var publicMembers = new[]
        {
            typeof(ISchema),
            typeof(SchemaMethodInfo),
            typeof(SchemaConstructorInfo)
        }
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .Cast<MethodBase>()))
            .SelectMany(member => member.GetParameters())
            .Select(parameter => parameter.ParameterType);

        Assert.IsFalse(publicMembers.Any(type =>
            type.Name.Contains("Dictionary", StringComparison.Ordinal) ||
            type.Name.Contains("ArgumentName", StringComparison.Ordinal)));
    }

    private static void Analyze(string query, ISchemaProvider schemaProvider)
    {
        var tree = new Musoq.Parser.Parser(new Lexer(query, true)).ComposeAll();
        var visitor = new BuildMetadataAndInferTypesVisitor(
            schemaProvider,
            new Dictionary<string, string[]>(),
            new Mock<ILogger<BuildMetadataAndInferTypesVisitor>>().Object);

        tree.Accept(new BuildMetadataAndInferTypesTraverseVisitor(visitor));
    }

    private sealed class MatrixSchemaProvider(SchemaMethodInfo[] methods, Action<object?[]> capture) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new MatrixSchema(methods, capture);
    }

    private sealed class MatrixSchema(SchemaMethodInfo[] methods, Action<object?[]> capture)
        : SchemaBase("matrix", new MethodsAggregator(new MethodsManager()))
    {
        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) => methods;

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            capture(parameters);
            return new MatrixTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters) => throw new NotSupportedException();
    }

    private sealed class MatrixTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [];
        public ISchemaColumn? GetColumnByName(string name) => null;
        public ISchemaColumn[] GetColumnsByName(string name) => [];
        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }

    private static class MatrixSignatures
    {
        public static SchemaMethodInfo[] Required =>
        [
            Method(typeof(RequiredTable), ("value", typeof(int)))
        ];

        public static SchemaMethodInfo[] RequiredTwo =>
        [
            Method(typeof(RequiredTwoTable), ("value", typeof(int)), ("other", typeof(int)))
        ];

        public static SchemaMethodInfo[] MetadataLess =>
        [
            new SchemaMethodInfo("source", SchemaConstructorInfo.Empty())
        ];

        public static SchemaMethodInfo[] HiddenContext =>
        [
            new SchemaMethodInfo(
                "source",
                new SchemaConstructorInfo(
                    typeof(HiddenContextTable)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(),
                    false,
                    ("value", typeof(int))))
        ];

        public static SchemaMethodInfo[] MismatchedReflection =>
        [
            Method(typeof(MismatchedReflectionTable), ("value", typeof(string)))
        ];

        public static SchemaMethodInfo[] Ambiguous =>
        [
            Method(typeof(AmbiguousComparableTable), ("value", typeof(IComparable))),
            Method(typeof(AmbiguousConvertibleTable), ("value", typeof(IConvertible)))
        ];

        public static SchemaMethodInfo[] ExactAndAssignable =>
        [
            Method(typeof(StringTable), ("value", typeof(string))),
            Method(typeof(ObjectTable), ("value", typeof(object)))
        ];

        public static SchemaMethodInfo[] PositionalOverloads =>
        [
            Method(typeof(RequiredTable), ("value", typeof(int))),
            Method(typeof(OptionalTable), ("value", typeof(int)), ("other", typeof(int)))
        ];

        public static SchemaMethodInfo[] IntegerOnly =>
        [
            Method(typeof(RequiredTable), ("value", typeof(int)))
        ];

        private static SchemaMethodInfo Method(Type tableType, params (string Name, Type Type)[] arguments)
        {
            var constructor = tableType
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(candidate => candidate.GetParameters().Length == arguments.Length);
            return new SchemaMethodInfo("source", new SchemaConstructorInfo(constructor, false, arguments));
        }
    }

    private sealed class RequiredTable : MatrixTableBase
    {
        public RequiredTable(int value) => _ = value;
    }

    private sealed class RequiredTwoTable : MatrixTableBase
    {
        public RequiredTwoTable(int value, int other)
        {
            _ = (value, other);
        }
    }

    private sealed class OptionalTable : MatrixTableBase
    {
        public OptionalTable(int value, int other = 9)
        {
            _ = (value, other);
        }
    }

    private sealed class HiddenContextTable : MatrixTableBase
    {
        public HiddenContextTable(SourceExecutionContext context, int value)
        {
            _ = (context, value);
        }
    }

    private sealed class MismatchedReflectionTable : MatrixTableBase
    {
        public MismatchedReflectionTable(int value) => _ = value;
    }

    private sealed class AmbiguousComparableTable : MatrixTableBase
    {
        public AmbiguousComparableTable(IComparable value) => _ = value;
    }

    private sealed class AmbiguousConvertibleTable : MatrixTableBase
    {
        public AmbiguousConvertibleTable(IConvertible value) => _ = value;
    }

    private sealed class StringTable : MatrixTableBase
    {
        public StringTable(string value) => _ = value;
    }

    private sealed class ObjectTable : MatrixTableBase
    {
        public ObjectTable(object value) => _ = value;
    }

    private abstract class MatrixTableBase : ISchemaTable
    {
        protected MatrixTableBase()
        {
        }

        public ISchemaColumn[] Columns => [];
        public ISchemaColumn? GetColumnByName(string name) => null;
        public ISchemaColumn[] GetColumnsByName(string name) => [];
        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }
}
