using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
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
    public void MetadataLessSource_RejectsNamedArguments()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() => Analyze(
            "select 1 from #matrix.source(value: 1)",
            new MatrixSchemaProvider(MatrixSignatures.MetadataLess, _ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3083_NamedSourceArgumentsRequireMetadata, exception.Code);
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

        Assert.AreEqual(DiagnosticCode.MQ3082_AmbiguousSourceInvocation, exception.Code);
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

        Assert.AreEqual(DiagnosticCode.MQ3013_CannotResolveMethod, exception.Code);
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
