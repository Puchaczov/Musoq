using Musoq.Schema;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvProviderAndSchemaTests : CsvExampleTestBase
{
    [TestMethod]
    public void Provider_WhenSchemaNameIsCsv_ShouldResolveSchemaAndRecordRequest()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder);

        var schema = provider.GetSchema("#csv");

        Assert.IsInstanceOfType<CsvSchema>(schema);
        CollectionAssert.AreEqual(new[] { "#csv" }, recorder.SchemaRequests);
    }

    [TestMethod]
    public void Provider_WhenSchemaNameIsUnknown_ShouldThrowAndRecordRequest()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder);

        Assert.Throws<SourceNotFoundException>(() => provider.GetSchema("#unknown"));

        CollectionAssert.AreEqual(new[] { "#unknown" }, recorder.SchemaRequests);
    }

    [TestMethod]
    public void Schema_WhenRawConstructorsAreRequested_ShouldExposeFileOverloadsAndRecordMetadata()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var schema = new CsvSchema(recorder);
        using var cancellation = new CancellationTokenSource();
        var context = CreateMetadataContext(
            queryId: "metadata-query",
            cancellationToken: cancellation.Token);

        var constructors = schema.GetRawConstructors(CsvSchema.File, context);

        Assert.AreEqual(5, constructors.Length);
        Assert.AreEqual("file", constructors[0].MethodName);
        Assert.AreEqual("file", recorder.RawConstructorCalls.Single().MethodName);
        Assert.AreEqual("metadata-query", recorder.RawConstructorCalls.Single().Metadata.QueryId);
        Assert.IsTrue(recorder.RawConstructorCalls.Single().Metadata.CancellationCanBeCanceled);
    }

    [TestMethod]
    public void Schema_WhenTableIsRequested_ShouldReturnCoupledColumnsAndCsvRowMetadata()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var schema = new CsvSchema(recorder);
        var columns = new ISchemaColumn[]
        {
            Column("Name", 0, typeof(string), new Dictionary<string, string>
            {
                [ColumnReadModifiers.Trim] = "true"
            }),
            Column("Amount", 1, typeof(decimal?))
        };

        var table = schema.GetTableByName(CsvSchema.File, CreateMetadataContext(columns), "people.csv", true);

        Assert.AreEqual(typeof(CsvRow), table.Metadata.TableEntityType);
        Assert.AreEqual(2, table.Columns.Length);
        Assert.AreEqual("Name", table.Columns[0].ColumnName);
        Assert.AreEqual("true", table.Columns[0].ReadModifiers[ColumnReadModifiers.Trim]);
        Assert.AreSame(table.Columns[0], table.GetColumnByName("name"));
        Assert.AreEqual(1, table.GetColumnsByName("amount").Length);
        Assert.AreEqual("people.csv", recorder.GetTableCalls.Single().Parameters[0]);
        Assert.AreEqual(true, recorder.GetTableCalls.Single().Parameters[1]);
    }

    [TestMethod]
    public void Schema_WhenRuntimeSettingsAreDescribed_ShouldReturnEmptyRequirementsAndRecordContext()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var schema = new CsvSchema(recorder);
        var identity = new SourceIdentity("csv", "file", "source-id", "Rows");
        var metadata = CreateMetadataContext(queryId: "runtime-settings-query");

        var requirements = schema.DescribeSourceRuntimeSettings(
            CsvSchema.File,
            new SourceRuntimeSettingsDescribeContext(identity, metadata),
            "people.csv");

        Assert.AreEqual(0, requirements.Count);
        Assert.AreEqual(identity, recorder.RuntimeSettingsCalls.Single().Identity);
        Assert.AreEqual("runtime-settings-query", recorder.RuntimeSettingsCalls.Single().Metadata.QueryId);
        Assert.AreEqual("people.csv", recorder.RuntimeSettingsCalls.Single().Parameters[0]);
    }

    [TestMethod]
    public void Schema_WhenSourceIsDescribed_ShouldReturnDescriptorAndRecordColumns()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var schema = new CsvSchema(recorder);
        var identity = new SourceIdentity("csv", "file", "source-id", "Rows");
        var columns = new ISchemaColumn[] { Column("Name", 0, typeof(string)) };
        var metadata = CreateMetadataContext(columns, "describe-query");

        var descriptor = schema.DescribeSource(
            CsvSchema.File,
            new SourceDescribeContext(identity, metadata),
            "people.csv");

        Assert.AreEqual(identity, descriptor.Identity);
        Assert.AreEqual(typeof(CsvRow), descriptor.RowType);
        Assert.AreEqual(SourceTransferCapabilities.QueryScopedRows, descriptor.TransferCapabilities);
        Assert.AreEqual("Name", descriptor.Columns.Single().ColumnName);
        Assert.AreEqual("Name", recorder.DescribeSourceCalls.Single().Columns.Single().ColumnName);
        Assert.AreEqual("describe-query", recorder.DescribeSourceCalls.Single().Metadata.QueryId);
    }

    [TestMethod]
    public void Schema_WhenQueryScopedRowsAreExplicitlyDisabled_ShouldAdvertiseNoTransferCapability()
    {
        var schema = new CsvSchema(recorder: null, enableQueryScopedRows: false);
        var identity = new SourceIdentity("csv", "file", "source-id", "Rows");
        var columns = new ISchemaColumn[] { Column("Name", 0, typeof(string)) };

        var descriptor = schema.DescribeSource(
            CsvSchema.File,
            new SourceDescribeContext(identity, CreateMetadataContext(columns, "legacy-query")),
            "people.csv");

        Assert.AreEqual(SourceTransferCapabilities.None, descriptor.TransferCapabilities);
        Assert.AreEqual(typeof(CsvRow), descriptor.RowType);
    }

    [TestMethod]
    public void Schema_WhenSourceIsPlanned_ShouldAcceptProjectionAndRecordRequest()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var schema = new CsvSchema(recorder);
        var identity = new SourceIdentity("csv", "file", "source-id", "Rows");
        var request = SourcePlanRequest.Empty(identity) with
        {
            RequiredColumns = [new SourceColumnRef("Name")]
        };

        var result = schema.TryPlanSource(CsvSchema.File, request, "people.csv");

        Assert.AreEqual(identity, result.ExecutionPlan.Identity);
        Assert.AreEqual(1, result.AcceptedColumns.Count);
        Assert.AreEqual("Name", result.AcceptedColumns.Single().Name);
        Assert.AreEqual("Name", recorder.PlanCalls.Single().Request.RequiredColumns.Single().Name);
        Assert.AreEqual("people.csv", recorder.PlanCalls.Single().Parameters[0]);
    }

    [TestMethod]
    public void Schema_WhenRowSourceIsRequested_ShouldReturnCsvRowSourceAndRecordExecutionContext()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var schema = new CsvSchema(recorder);
        var identity = new SourceIdentity("csv", "file", "source-id", "Rows");
        var columns = new ISchemaColumn[] { Column("Name", 0, typeof(string)) };
        var plan = SourceExecutionPlan.Empty(identity) with
        {
            AcceptedColumns = [new SourceColumnRef("Name")]
        };
        var context = CreateExecutionContext(plan, columns);

        var source = schema.GetRowSource<CsvRow>(CsvSchema.File, context, "people.csv");

        Assert.IsInstanceOfType<CsvFileSource>(source);
        Assert.AreEqual(typeof(CsvRow), recorder.RowSourceCalls.Single().RequestedRowType);
        Assert.AreEqual(identity, recorder.RowSourceCalls.Single().Execution.Plan.Identity);
        Assert.AreEqual("Name", recorder.RowSourceCalls.Single().Execution.AllColumns.Single().ColumnName);
        Assert.AreEqual("people.csv", recorder.RowSourceCalls.Single().Parameters[0]);
    }
}
