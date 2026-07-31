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
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;
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

    private MusoqQueryException CompileExpectedFailure(Type entityType)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select a.Name from #unsafe.all() a",
            Guid.NewGuid().ToString(),
            new UnsafeSchemaProvider(entityType),
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
}
