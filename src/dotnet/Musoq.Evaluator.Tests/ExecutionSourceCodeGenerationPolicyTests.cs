using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Tests.Common.Schema;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;
using TestSchemaColumn = Musoq.Evaluator.Tests.Components.SchemaColumn;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ExecutionSourceCodeGenerationPolicyTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    [TestMethod]
    public void CompileForInspection_WhenSourceEntityIsPrivate_ShouldReportGeneratedExecutionDiagnostic()
    {
        var exception = CompileExpectedFailure(typeof(PrivateEntity));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            DiagnosticPhase.Bind,
            "PrivateEntity");
        AssertHasGuidance(exception);
        AssertMessageContains(exception, "public CLR contract");
        AssertMessageContains(exception, "schema-indexed positional row (currently object[])");
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceEntityIsObject_ShouldReportGeneratedExecutionDiagnostic()
    {
        var exception = CompileExpectedFailure(typeof(object));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            DiagnosticPhase.Bind,
            "object-typed");
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceEntityIsCustomDynamic_ShouldReportGeneratedExecutionDiagnostic()
    {
        var exception = CompileExpectedFailure(typeof(CustomDynamicEntity));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            DiagnosticPhase.Bind,
            "custom runtime-dynamic");
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceEntityIsObjectArrayArray_ShouldRemainUnsupported()
    {
        var exception = CompileExpectedFailure(typeof(object[][]));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            DiagnosticPhase.Bind,
            "System.Object[][]");
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceEntityIsArbitraryList_ShouldRemainUnsupported()
    {
        var exception = CompileExpectedFailure(typeof(List<object[]>));

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            DiagnosticPhase.Bind,
            "List");
    }

    [TestMethod]
    public void CompileForInspection_WhenPositionalColumnHasNegativeIndex_ShouldReportGeneratedExecutionDiagnostic()
    {
        var exception = CompileExpectedFailure(
            new PositionalRowsSchemaProvider(
                [new TestSchemaColumn("Name", -1, typeof(string))],
                [["Ada"]]),
            "select a.Name from #positional.all() a");

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            DiagnosticPhase.Bind,
            "Name");
        AssertMessageContains(exception, "invalid negative index");
    }

    [TestMethod]
    public void CompileForInspection_WhenPositionalColumnTypeIsNotReferenceable_ShouldReportGeneratedExecutionDiagnostic()
    {
        var exception = CompileExpectedFailure(
            new PositionalRowsSchemaProvider(
                [
                    new TestSchemaColumn("Hidden", 0, typeof(InaccessibleColumnType)),
                    new TestSchemaColumn("Name", 1, typeof(string))
                ],
                [[new InaccessibleColumnType(), "Ada"]]),
            "select a.Name from #positional.all() a where a.Hidden != null");

        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
            DiagnosticPhase.Bind,
            "Hidden");
        AssertMessageContains(exception, "non-referenceable type");
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceEntityIsSupportedDictionary_ShouldUseGeneratedAdapter()
    {
        var provider = new AnySchemaNameProvider(
            new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
            {
                ["dynamic"] =
                (
                    new Dictionary<string, Type> { ["Id"] = typeof(int) },
                    new dynamic[] { new Dictionary<string, object?> { ["Id"] = 7 } }
                )
            });

        var inspection = InstanceCreator.CompileForInspection(
            "select d.Id from #dynamic.all() d",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetNestedValue", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetRowSourceChunks", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceEntityIsSchemaIndexedObjectArray_ShouldUseDirectPositionalAccess()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select a.Name, a.Age, a.[Address.City] from #positional.all() a",
            Guid.NewGuid().ToString(),
            new PositionalRowsSchemaProvider(
                [
                    new Musoq.Schema.DataSources.SchemaColumn("Name", 2, typeof(string)),
                    new Musoq.Schema.DataSources.SchemaColumn("Age", 0, typeof(int)),
                    new Musoq.Schema.DataSources.SchemaColumn("Department", 1, typeof(string)),
                    new Musoq.Schema.DataSources.SchemaColumn("Address.City", 3, typeof(string))
                ],
                [
                    [37, "Engineering", "Ada", "London"],
                    [29, "Research", "Bea", "Paris"]
                ]),
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("ExpandoAdapter", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GeneratedDictionaryAccess", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetColumnValue", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetNestedValue", StringComparison.Ordinal));
        Assert.Contains("[2]", inspection.GeneratedCSharpCode);
        Assert.Contains("[0]", inspection.GeneratedCSharpCode);
        Assert.Contains("[3]", inspection.GeneratedCSharpCode);
        Assert.Contains("position 0", inspection.ExecutionPlanText);
        Assert.Contains("position 2", inspection.ExecutionPlanText);
        Assert.Contains("position 3", inspection.ExecutionPlanText);
    }

    private MusoqQueryException CompileExpectedFailure(Type entityType)
    {
        return CompileExpectedFailure(
            new UnsafeSchemaProvider(entityType),
            "select a.Name from #unsafe.all() a");
    }

    private MusoqQueryException CompileExpectedFailure(
        ISchemaProvider provider,
        string query)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        Assert.IsTrue(result.HasErrors, "The inaccessible source contract should fail compilation.");
        return result.CaughtException is { } caughtException
            ? new MusoqQueryException(result.ToEnvelopes(), caughtException)
            : new MusoqQueryException(result.ToEnvelopes());
    }

    private sealed class UnsafeSchemaProvider(Type entityType) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new UnsafeSchema(entityType);
    }

    private sealed class UnsafeSchema(Type entityType) : SchemaBase("unsafe", CachedLibrary.Value)
    {
        private static readonly Lazy<MethodsAggregator> CachedLibrary =
            new(() => new MethodsAggregator(new MethodsManager()));

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new UnsafeTable(entityType);
        }

        public override SchemaMethodInfo[] GetRawConstructors(
            string methodName,
            SourceMetadataContext metadataContext)
        {
            return methodName.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? [new SchemaMethodInfo("all", SchemaConstructorInfo.Empty())]
                : [];
        }
    }

    private sealed class UnsafeTable(Type entityType) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new Musoq.Schema.DataSources.SchemaColumn("Name", 0, typeof(string))
        ];

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

        public SchemaTableMetadata Metadata { get; } = new(entityType);
    }

    private sealed class PrivateEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class CustomDynamicEntity : DynamicObject
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class InaccessibleColumnType
    {
    }

}
