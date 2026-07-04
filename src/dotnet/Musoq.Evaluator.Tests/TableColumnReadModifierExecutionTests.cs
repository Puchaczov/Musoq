using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TableColumnReadModifierExecutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void TableColumnReadModifiers_ShouldReachMetadataPlanningAndExecutionContexts()
    {
        const string query =
            "table LegacyRecord { Id: int, Name: string encoding 'windows-1250' trim, Payload: string source codec 'base64' };" +
            "couple #capture.items with table LegacyRecord as Records;" +
            "select Id, Name, Payload from Records()";
        var provider = new CapturingReadModifierSchemaProvider([
            new Dictionary<string, object?>
            {
                ["Id"] = 1,
                ["Name"] = "Zosia",
                ["Payload"] = "cGF5bG9hZA=="
            }
        ]);

        var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int?)),
            ("Name", typeof(string)),
            ("Payload", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, "Zosia", "cGF5bG9hZA=="]);
        AssertReadModifiers(provider.Recorder.GetTableColumns.Last());
        AssertReadModifiers(provider.Recorder.DescriptorColumns.Single());
        AssertReadModifiers(provider.Recorder.ExecutionColumns.Single());

        var request = provider.Recorder.PlanRequests.Single();
        Assert.AreEqual("windows-1250", request.RequiredColumns.Single(column => column.Name == "Name").ReadModifiers["encoding"]);
        Assert.AreEqual("base64", request.RequiredColumns.Single(column => column.Name == "Payload").ReadModifiers["source.codec"]);
    }

    private static void AssertReadModifiers(IReadOnlyCollection<ISchemaColumn> columns)
    {
        var nameColumn = columns.Single(column => column.ColumnName == "Name");
        Assert.AreEqual("windows-1250", nameColumn.ReadModifiers["encoding"]);
        Assert.AreEqual("true", nameColumn.ReadModifiers["trim"]);

        var payloadColumn = columns.Single(column => column.ColumnName == "Payload");
        Assert.AreEqual("base64", payloadColumn.ReadModifiers["source.codec"]);
    }

    private sealed class CapturingReadModifierSchemaProvider(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        : ISchemaProvider
    {
        public ReadModifierRecorder Recorder { get; } = new();

        public ISchema GetSchema(string schema)
        {
            return new CapturingReadModifierSchema(rows, Recorder);
        }
    }

    private sealed class CapturingReadModifierSchema(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        ReadModifierRecorder recorder)
        : SchemaBase("capture", new MethodsAggregator(new MethodsManager()))
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            recorder.GetTableColumns.Add(metadataContext.AllColumns.ToArray());
            return new CapturingReadModifierTable(metadataContext.AllColumns.ToArray());
        }

        public override SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters)
        {
            var columns = context.MetadataContext.AllColumns.ToArray();
            recorder.DescriptorColumns.Add(columns);
            return new SourceDescriptor
            {
                Identity = context.Identity,
                RowType = typeof(IReadOnlyDictionary<string, object?>),
                Columns = columns
            };
        }

        public override SourcePlanResult TryPlanSource(
            string name,
            SourcePlanRequest request,
            params object?[] parameters)
        {
            recorder.PlanRequests.Add(request);
            return SourcePlanResult.AcceptAll(request);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            recorder.ExecutionColumns.Add(executionContext.AllColumns.ToArray());
            return EnsureSourceType<T, IReadOnlyDictionary<string, object?>>(
                name,
                new DynamicSource(rows));
        }
    }

    private sealed class CapturingReadModifierTable(ISchemaColumn[] columns) : ISchemaTable
    {
        public ISchemaColumn[] Columns => columns;

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(IReadOnlyDictionary<string, object?>));
    }

    private sealed class ReadModifierRecorder
    {
        public List<IReadOnlyCollection<ISchemaColumn>> GetTableColumns { get; } = [];

        public List<IReadOnlyCollection<ISchemaColumn>> DescriptorColumns { get; } = [];

        public List<SourcePlanRequest> PlanRequests { get; } = [];

        public List<IReadOnlyCollection<ISchemaColumn>> ExecutionColumns { get; } = [];
    }
}
