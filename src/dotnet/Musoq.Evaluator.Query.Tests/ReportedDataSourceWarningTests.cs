using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ReportedDataSourceWarningTests
{
    private static DataSourceDiagnostic Warning(string code) => new(code, "format", "A malformed record was skipped.",
        new DataSourceDiagnosticOptions { Severity = DataSourceDiagnosticSeverity.Warning, SuggestedFixes = ["Correct the record or enable strict parsing."] });

    [TestMethod]
    public async Task RuntimeWarnings_ContinueProducingRowsAndRemainExecutionLocal()
    {
        var provider = new Provider(new ReportingSchema());
        using var compiled = InstanceCreator.CompileForExecution("select Name from #test.entities() e",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        var first = new DataSourceWarningCollector();
        var table = compiled.Run(new TypedQueryRunOptions { DataSourceWarnings = first });
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("CSV.SKIPPED", first.Snapshot().Single().Arguments["providerCode"]);
        Assert.AreEqual("e", first.Snapshot().Single().Arguments["alias"]);
        var second = new DataSourceWarningCollector();
        var next = await compiled.RunAsync(new TypedQueryRunOptions { DataSourceWarnings = second });
        Assert.AreEqual(2, next.Count);
        Assert.AreEqual("1", first.Snapshot().Single().Arguments["occurrenceCount"]);
        Assert.AreEqual("1", second.Snapshot().Single().Arguments["occurrenceCount"]);
    }

    [TestMethod]
    public void MetadataAndPlanningReports_ReachPublicCompilationDiagnostics()
    {
        var provider = new Provider(new ReportingSchema { MetadataReport = Warning("CSV.METADATA"), PlanningReport = Warning("CSV.PLAN") });
        var result = InstanceCreator.CompileWithDiagnostics("select Name from #test.entities() e",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        CollectionAssert.AreEquivalent(new[] { "CSV.METADATA", "CSV.PLAN" }, result.Warnings
            .Where(item => item.Code == DiagnosticCode.MQ5027_ReportedDataSourceWarning).Select(item => item.Arguments["providerCode"]).ToArray());
        result.CompiledQuery?.Dispose();
        provider.Schema.PlanningReport = new DataSourceDiagnostic("API.BAD_CONFIG", "configuration", "A required setting is missing.", new DataSourceDiagnosticOptions { Severity = DataSourceDiagnosticSeverity.Error, SuggestedFixes = ["Configure the service endpoint."], FailureKind = DataSourceFailureKind.InvalidConfiguration });
        var failed = InstanceCreator.CompileWithDiagnostics("select Name from #test.entities() e",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        Assert.IsFalse(failed.Succeeded);
        Assert.AreEqual(DiagnosticCode.MQ7020_ReportedDataSourceFailure, failed.Errors.Single().Code);
        Assert.AreEqual("API.BAD_CONFIG", failed.Errors.Single().Arguments["providerCode"]);
    }

    [TestMethod]
    public void ErrorAfterProducingRows_FailsInsteadOfCompletingSuccessfully()
    {
        var provider = new Provider(new ReportingSchema { FailAfterFirstChunk = true });
        using var compiled = InstanceCreator.CompileForExecution("select Name from #test.entities() e",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        var collector = new DataSourceWarningCollector();
        var exception = Assert.Throws<QueryExecutionException>(() =>
        {
            var table = compiled.Run(new TypedQueryRunOptions { DataSourceWarnings = collector });
            _ = table.Count;
        });
        Assert.AreEqual(DiagnosticCode.MQ7020_ReportedDataSourceFailure, exception.Envelope!.Code);
        Assert.AreEqual("API.READ_FAILED", exception.Envelope.Arguments["providerCode"]);
        Assert.AreEqual(1, collector.Snapshot().Count);
    }

    [TestMethod]
    public void TypedDeferredExecution_ReportsWarningsToTheSuppliedExecutionCollector()
    {
        var compiled = InstanceCreator.CompileForTypedExecution<NameRow>("select Name from #test.entities() e",
            Guid.NewGuid().ToString(), new Provider(new ReportingSchema()), new TestsLoggerResolver());
        var collector = new DataSourceWarningCollector();
        var rows = compiled.Run(new TypedQueryRunOptions { DataSourceWarnings = collector });
        Assert.AreEqual(0, collector.Snapshot().Count);
        Assert.AreEqual(2, rows.Count());
        Assert.AreEqual("CSV.SKIPPED", collector.Snapshot().Single().Arguments["providerCode"]);
    }

    public readonly record struct NameRow(string Name);

    private sealed class Provider(ReportingSchema schema) : ISchemaProvider
    {
        internal ReportingSchema Schema { get; } = schema;
        public ISchema GetSchema(string name) => Schema;
    }

    private sealed class ReportingSchema() : GenericSchema<BasicEntity, BasicEntityTable>([], BasicEntity.TestNameToIndexMap, BasicEntity.TestIndexToObjectAccessMap)
    {
        internal DataSourceDiagnostic? MetadataReport { get; set; }
        internal DataSourceDiagnostic? PlanningReport { get; set; }
        internal bool FailAfterFirstChunk { get; init; }
        public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
            => base.DescribeSource(name, context, parameters) with { ReportedDiagnostics = MetadataReport is null ? [] : [MetadataReport] };
        public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
            => SourcePlanResult.RejectAll(request) with { ReportedDiagnostics = PlanningReport is null ? [] : [PlanningReport] };
        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments arguments)
            => EnsureSourceType<T, BasicEntity>(name, new ReportingRows(context, FailAfterFirstChunk));
    }

    private sealed class ReportingRows(SourceExecutionContext context, bool fail) : RowSource<BasicEntity>
    {
        public override IEnumerable<IReadOnlyList<BasicEntity>> Chunks
        {
            get
            {
                yield return new[] { new BasicEntity("first") };
                context.ReportWarning(Warning("CSV.SKIPPED"));
                if (fail) throw new DataSourceException(new DataSourceDiagnostic("API.READ_FAILED", "remote-service", "The service stopped producing rows.", new DataSourceDiagnosticOptions { Severity = DataSourceDiagnosticSeverity.Error, SuggestedFixes = ["Retry later."], FailureKind = DataSourceFailureKind.Unavailable }));
                yield return new[] { new BasicEntity("second") };
            }
        }
    }
}
