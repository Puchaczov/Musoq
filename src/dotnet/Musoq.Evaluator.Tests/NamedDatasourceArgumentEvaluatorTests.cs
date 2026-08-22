using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Components;
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
public sealed class NamedDatasourceArgumentEvaluatorTests
{
    [TestMethod]
    public void NamedArguments_AreCanonicalizedBeforeMetadataInvocation()
    {
        object?[]? captured = null;

        Analyze(
            "select 1 from #capture.any(second: 4, first: 'value')",
            new NamedCaptureSchemaProvider(arguments => captured = arguments));

        Assert.IsNotNull(captured);
        CollectionAssert.AreEqual(new object?[] { "value", 4 }, captured);
    }

    [TestMethod]
    public void NamedArguments_UseReflectedOptionalDefaults()
    {
        object?[]? captured = null;

        Analyze(
            "select 1 from #capture.any(first: 'value')",
            new NamedCaptureSchemaProvider(arguments => captured = arguments));

        Assert.IsNotNull(captured);
        CollectionAssert.AreEqual(new object?[] { "value", 7 }, captured);
    }

    [TestMethod]
    public void NamedArguments_AreCanonicalizedForCoupledSources()
    {
        object?[]? captured = null;

        Analyze(
            "table T { Value: int }; couple #capture.any with table T as Source; select 1 from Source(second: 4, first: 'value')",
            new NamedCaptureSchemaProvider(arguments => captured = arguments));

        Assert.IsNotNull(captured);
        CollectionAssert.AreEqual(new object?[] { "value", 4 }, captured);
    }

    [TestMethod]
    public void NamedArguments_AreCanonicalizedForJoinSources()
    {
        var captured = new List<object?[]>();

        Analyze(
            "select 1 from #capture.any(second: 4, first: 'value') a join #capture.any(first: 'other', second: 8) b on 1 = 1",
            new NamedCaptureSchemaProvider(arguments => captured.Add(arguments)));

        Assert.HasCount(2, captured);
        CollectionAssert.AreEqual(new object?[] { "value", 4 }, captured[0]);
        CollectionAssert.AreEqual(new object?[] { "other", 8 }, captured[1]);
    }

    [TestMethod]
    public void NamedArguments_AreCanonicalizedForApplySources()
    {
        var captured = new List<object?[]>();

        Analyze(
            "select 1 from #capture.any(first: 'outer', second: 4) a cross apply #capture.any(second: 8, first: 'inner') b",
            new NamedCaptureSchemaProvider(arguments => captured.Add(arguments)));

        Assert.HasCount(2, captured);
        CollectionAssert.AreEqual(new object?[] { "outer", 4 }, captured[0]);
        CollectionAssert.AreEqual(new object?[] { "inner", 8 }, captured[1]);
    }

    [TestMethod]
    public void NamedArguments_AreCanonicalizedInsideDescQuery()
    {
        object?[]? captured = null;

        Analyze(
            "desc query (select 1 from #capture.any(second: 4, first: 'value'))",
            new NamedCaptureSchemaProvider(arguments => captured = arguments));

        Assert.IsNotNull(captured);
        CollectionAssert.AreEqual(new object?[] { "value", 4 }, captured);
    }

    [TestMethod]
    public void NamedArguments_AreCanonicalizedForDescAndDescSettings()
    {
        Analyze(
            "desc #capture.any(second: 4, first: 'value')",
            new NamedCaptureSchemaProvider(_ => { }));

        Analyze(
            "desc settings #capture.any(second: 4, first: 'value')",
            new NamedCaptureSchemaProvider(_ => { }));
    }

    [TestMethod]
    public void DescSignatures_ShowUsableReflectedDefaults()
    {
        var schema = new NamedCaptureSchema(_ => { });
        var context = new SourceExecutionContext(
            "desc",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);

        var table = EvaluationHelper.GetConstructorsForSpecificMethod(schema, "any", context);

        Assert.AreEqual("first: System.String", table[0][1]);
        Assert.AreEqual("second: System.Int32 = 7", table[0][2]);
    }

    [TestMethod]
    public void NamedArguments_AreCanonicalizedAtRuntime()
    {
        object?[]? captured = null;
        var provider = new NamedCaptureSchemaProvider(arguments => captured = arguments);
        var vm = InstanceCreator.CompileForExecution(
            "select 1 from #capture.any(second: 4, first: 'value')",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver());

        var result = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        CollectionAssert.AreEqual(new object?[] { "value", 4 }, captured);
    }

    [TestMethod]
    public void RequiredParameterInFirstSourcePosition_PreservesLaterLiterals()
    {
        AssertPositionalSourceArguments(
            "$value, 'middle', 'last'",
            [],
            ["value", "middle", "last"]);
    }

    [TestMethod]
    public void RequiredParameterInMiddleSourcePosition_PreservesLaterLiterals()
    {
        AssertPositionalSourceArguments(
            "'first', $value, 'last'",
            ["first"],
            ["first", "value", "last"]);
    }

    [TestMethod]
    public void RequiredParameterInLastSourcePosition_PreservesEarlierLiterals()
    {
        AssertPositionalSourceArguments(
            "'first', 'middle', $value",
            ["first", "middle"],
            ["first", "middle", "value"]);
    }

    [TestMethod]
    public void NamedArguments_WithRequiredParameter_PreserveCanonicalRuntimeOrder()
    {
        var metadata = new List<object?[]>();
        var runtime = new List<object?[]>();
        var provider = new TripleCaptureSchemaProvider(metadata.Add, runtime.Add);
        var vm = InstanceCreator.CompileForExecution(
            "param(value: string) select Value from #triple.any(second: 'middle', first: $value, last: 'last')",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver());

        vm.Parameters["value"] = "first";
        var result = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.IsNotEmpty(metadata);
        Assert.IsTrue(metadata.All(arguments => arguments.Length == 0));
        CollectionAssert.AreEqual(new object?[] { "first", "middle", "last" }, runtime.Single());
    }

    [TestMethod]
    public void NamedArguments_UseDeclaredParameterTypeForOverloadBinding()
    {
        var runtime = new List<object?[]>();
        var provider = new TripleCaptureSchemaProvider(_ => { }, runtime.Add);
        var vm = InstanceCreator.CompileForExecution(
            "param(value: int) select Value from #triple.any(second: $value, first: 'first', last: 'last')",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver());

        vm.Parameters["value"] = 7;
        var result = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        CollectionAssert.AreEqual(new object?[] { "first", 7, "last" }, runtime.Single());
    }

    [TestMethod]
    public void NamedArguments_UnknownNameHasStableDiagnostic()
    {
        var exception = Assert.Throws<CannotResolveMethodException>(() =>
            Analyze(
                "select 1 from #capture.any(unknown: 4, first: 'value')",
                new NamedCaptureSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3079_UnknownSourceArgument, exception.Code);
    }

    private static BuildMetadataAndInferTypesVisitor Analyze(string query, ISchemaProvider schemaProvider)
    {
        var tree = new Musoq.Parser.Parser(new Lexer(query, true)).ComposeAll();
        var logger = new Mock<ILogger<BuildMetadataAndInferTypesVisitor>>();
        var visitor = new BuildMetadataAndInferTypesVisitor(
            schemaProvider,
            new Dictionary<string, string[]>(),
            logger.Object);

        tree.Accept(new BuildMetadataAndInferTypesTraverseVisitor(visitor));
        return visitor;
    }

    private static void AssertPositionalSourceArguments(
        string arguments,
        object?[] expectedMetadataPrefix,
        object?[] expectedRuntimeArguments)
    {
        var metadata = new List<object?[]>();
        var runtime = new List<object?[]>();
        var provider = new TripleCaptureSchemaProvider(metadata.Add, runtime.Add);
        var vm = InstanceCreator.CompileForExecution(
            $"param(value: string) select Value from #triple.any({arguments})",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver());

        vm.Parameters["value"] = "value";
        var result = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.IsNotEmpty(metadata);
        foreach (var captured in metadata)
            CollectionAssert.AreEqual(expectedMetadataPrefix, captured);

        CollectionAssert.AreEqual(expectedRuntimeArguments, runtime.Single());
    }

    private sealed class NamedCaptureSchemaProvider(Action<object?[]> capture) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new NamedCaptureSchema(capture);
    }

    private sealed class NamedCaptureSchema(Action<object?[]> capture)
        : SchemaBase("capture", new MethodsAggregator(new MethodsManager()))
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new(
                "any",
                new SchemaConstructorInfo(
                    typeof(NamedCaptureTable)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(constructor => constructor.GetParameters().Length == 2),
                    false,
                    ("first", typeof(string)),
                    ("second", typeof(int))))
        ];

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) => Constructors;

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            capture(parameters);
            return new NamedCaptureTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            capture(parameters);
            return new SingleRowSource<T>();
        }
    }

    private sealed class SingleRowSource<T> : RowSource<T>
    {
        public override IEnumerable<IReadOnlyList<T>> Chunks =>
            new[] { (IReadOnlyList<T>)new[] { default(T)! } };
    }

    private sealed class NamedCaptureTable : ISchemaTable
    {
        public NamedCaptureTable()
            : this(string.Empty, 7)
        {
        }

        public NamedCaptureTable(string first, int second = 7)
        {
            _ = first;
            _ = second;
        }

        public ISchemaColumn[] Columns => [];

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(NamedCaptureEntity));
    }

    public sealed class NamedCaptureEntity;

    private sealed class TripleCaptureSchemaProvider(
        Action<object?[]> captureMetadata,
        Action<object?[]> captureRuntime) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new TripleCaptureSchema(captureMetadata, captureRuntime);
    }

    private sealed class TripleCaptureSchema(
        Action<object?[]> captureMetadata,
        Action<object?[]> captureRuntime)
        : SchemaBase("triple", new MethodsAggregator(new MethodsManager()))
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new(
                "any",
                new SchemaConstructorInfo(
                    typeof(TripleCaptureTable)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(constructor => constructor.GetParameters().Length == 3),
                    false,
                    ("first", typeof(string)),
                    ("second", typeof(string)),
                    ("last", typeof(string)))),
            new(
                "any",
                new SchemaConstructorInfo(
                    typeof(TripleCaptureIntTable)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(),
                    false,
                    ("first", typeof(string)),
                    ("second", typeof(int)),
                    ("last", typeof(string))))
        ];

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) => Constructors;

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            captureMetadata(parameters);
            return new TripleCaptureTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            captureRuntime(parameters);
            return new TripleCaptureRowSource<T>();
        }
    }

    private sealed class TripleCaptureRowSource<T> : RowSource<T>
    {
        public override IEnumerable<IReadOnlyList<T>> Chunks =>
            new[] { (IReadOnlyList<T>)new[] { CreateRow() } };

        private static T CreateRow()
        {
            if (typeof(T) == typeof(TripleCaptureEntity))
                return (T)(object)new TripleCaptureEntity { Value = 1 };

            return default!;
        }
    }

    private sealed class TripleCaptureTable : ISchemaTable
    {
        public TripleCaptureTable()
        {
        }

        public TripleCaptureTable(string first, string second, string last)
        {
            _ = (first, second, last);
        }

        public ISchemaColumn[] Columns => [new Musoq.Schema.DataSources.SchemaColumn(nameof(TripleCaptureEntity.Value), 0, typeof(int))];

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(TripleCaptureEntity));
    }

    private sealed class TripleCaptureIntTable : ISchemaTable
    {
        public TripleCaptureIntTable(string first, int second, string last)
        {
            _ = (first, second, last);
        }

        public ISchemaColumn[] Columns => [new Musoq.Schema.DataSources.SchemaColumn(nameof(TripleCaptureEntity.Value), 0, typeof(int))];

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(TripleCaptureEntity));
    }

    public sealed class TripleCaptureEntity
    {
        public int Value { get; init; }
    }
}
