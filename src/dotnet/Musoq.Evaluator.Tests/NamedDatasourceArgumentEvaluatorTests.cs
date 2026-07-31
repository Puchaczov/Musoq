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
using Musoq.Schema.Optimization;
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

        public ISchemaColumn? GetColumnByName(string name) => null;

        public ISchemaColumn[] GetColumnsByName(string name) => [];

        public SchemaTableMetadata Metadata { get; } = new(typeof(NamedCaptureEntity));
    }

    public sealed class NamedCaptureEntity;
}
