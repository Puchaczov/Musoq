using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.QueryRows;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    [TestMethod]
    public void SourcePlanning_ShouldCarryAuthoritativeConcreteColumnTypes()
    {
        var rows = new[] { new PlanningTypeRow(1, 1.25m), new PlanningTypeRow(2, null), new PlanningTypeRow(3, -3m) };
        var schema = new PlanningTypeSchema(rows);
        var provider = new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#schema"] = schema });
        const string sql = "select Position, Amount from #schema.first() where Position >= 0 order by Position";
        using var compiled = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Amount", typeof(decimal?)));
        CollectionAssert.AreEqual(new[] { (1, (decimal?)1.25m), (2, (decimal?)null), (3, (decimal?)-3m) },
            table.Select(row => (row.GetValue<int>(0), row.GetValue<decimal?>(1))).ToArray());
        var columns = schema.Requests.SelectMany(request => request.RequiredColumns).DistinctBy(column => column.Name).ToArray();
        Assert.AreEqual(typeof(int).AssemblyQualifiedName, columns.Single(column => column.Name == "Position").ValueTypeName);
        Assert.AreEqual(typeof(decimal?).AssemblyQualifiedName, columns.Single(column => column.Name == "Amount").ValueTypeName);
    }

    [TestMethod]
    public void QueryScopedMetadataMarker_ShouldProduceConcreteNativeRows()
    {
        const string sql = "select Id, Name from #queryrowsample.rows() order by Id";
        using var compiled = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"),
            new GeneratedQueryRowSampleSchemaProvider(GeneratedQueryRowSampleShape.Narrow, true), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        CollectionAssert.AreEqual(new[] { (1, "alpha"), (2, "beta") },
            table.Select(row => (row.GetValue<int>(0), row.GetValue<string>(1))).ToArray());
    }

    [TestMethod]
    public void QueryScopedMetadataMarker_WhenNativeTransferIsMissing_ShouldDiagnoseDuringCompilation()
    {
        var schema = new GenericSchema<LibraryBase>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["first"] = (new GeneratedQueryRowSampleTable([new SchemaColumn("Id", 0, typeof(int))]),
                new GenericChunkSource<PlanningTypeRow>([], GenericEntityTable<PlanningTypeRow>.NameToIndexMap,
                    GenericEntityTable<PlanningTypeRow>.IndexToObjectAccessMap))
        });
        var provider = new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#schema"] = schema });
        const string sql = "select Id from #schema.first()";
        var error = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("native QueryScopedRows transfer", error.Message);
    }

    [TestMethod]
    public void ErasedDictionaryTransfer_ShouldReportCompilationDiagnostic()
    {
        const string sql = "select Position from #schema.first()";
        var schema = new ErasedPlanningTypeSchema([new PlanningTypeRow(1, 2m)]);
        var provider = new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#schema"] = schema });
        var error = Assert.ThrowsExactly<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions));
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        Assert.Contains("Erased dynamic rows require native QueryScopedRows transfer", error.Message);
    }

    private sealed class ErasedPlanningTypeSchema(PlanningTypeRow[] rows) : GenericSchema<LibraryBase>(
        new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["first"] = (new GenericEntityTable<PlanningTypeRow>(), new GenericChunkSource<PlanningTypeRow>(rows,
                GenericEntityTable<PlanningTypeRow>.NameToIndexMap, GenericEntityTable<PlanningTypeRow>.IndexToObjectAccessMap))
        })
    {
        public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
            => base.DescribeSource(name, context, parameters) with
            {
                RowType = typeof(IReadOnlyDictionary<string, object>),
                TransferCapabilities = SourceTransferCapabilities.None
            };
    }

    [TestMethod]
    public void CaseDistinctNativeColumns_ThroughCteGrouping_ShouldRetainTypesAndValues()
    {
        CaseDistinctPlanningRow[] rows = [new(1, "first", 2), new(2, "second", 1), new(3, "first", 2)];
        const string sql = "with data as (select Position, Name, name from #schema.first()) " +
            "select Name, name, Count(Position) as Present from data group by Name, name order by name";
        using var compiled = CreateAndRunVirtualMachine(sql, rows);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("name", typeof(int?)), ("Present", typeof(long)));
        CollectionAssert.AreEqual(new[] { ("second", (int?)1, 1L), ("first", (int?)2, 2L) },
            table.Select(row => (row.GetValue<string>(0), row.GetValue<int?>(1), row.GetValue<long>(2))).ToArray());
    }

    public sealed record CaseDistinctPlanningRow(int Position, string Name, int? name);

    public sealed record PlanningTypeRow(int Position, decimal? Amount);
    private sealed class PlanningTypeSchema(PlanningTypeRow[] rows) : GenericSchema<LibraryBase>(
        new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["first"] = (new GenericEntityTable<PlanningTypeRow>(), new GenericChunkSource<PlanningTypeRow>(rows,
                GenericEntityTable<PlanningTypeRow>.NameToIndexMap, GenericEntityTable<PlanningTypeRow>.IndexToObjectAccessMap))
        })
    {
        internal List<SourcePlanRequest> Requests { get; } = [];
        public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
        {
            Requests.Add(request);
            return SourcePlanResult.RejectAll(request);
        }
    }
}
