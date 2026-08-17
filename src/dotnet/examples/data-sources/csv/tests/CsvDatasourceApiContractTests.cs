using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvDatasourceApiContractTests : CsvExampleTestBase
{
    private const string RuntimeSettingName = "CSV_TOKEN";
    private const string RuntimeSettingValue = "runtime-token";
    private const string SourceNameModifier = "source.name";
    private const string SourceIndexModifier = "source.index";

    [TestMethod]
    public void Engine_WhenRunningCoupledCsvQuery_ShouldPropagateDatasourceApiContexts()
    {
        var path = WriteTempCsv(
            "skip-1\n" +
            "skip-2\n" +
            "skip-3\n" +
            "FullName,Amount,Ignored,Status\n");
        var expectedPathArgument = path.Replace('\\', '/');
        var recorder = new CsvDataSourceApiRecorder();
        recorder.RuntimeSettingRequirements.Add(new SourceRuntimeSettingRequirement(
            RuntimeSettingName,
            false,
            false,
            SourceRuntimeSettingPhase.All,
            "Token used by CSV tests."));
        var settings = new Dictionary<string, string>
        {
            [RuntimeSettingName] = RuntimeSettingValue
        };
        var resolver = new StaticSettingsResolver(settings);
        var options = CreateOptionsWithRuntimeSettings(
                settings,
                resolver)
            .WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries);
        var provider = new CsvSchemaProvider(recorder);
        var query =
            "table CsvShape { Name: string trim source name 'FullName', Amount: decimal culture 'pl-PL', Status: string source index '3' };" +
            "couple #csv.file with settings csvProfile and table CsvShape as Rows;" +
            $"select r.Name from Rows({SqlString(path)}, true, 3) r " +
            "where r.Amount > 10 and r.Status in ('Open', 'Closed') " +
            "order by r.Name desc skip 1 take 2";
        var progress = new List<DataSourceEventArgs>();
        using var cancellation = new CancellationTokenSource();

        var compiled = Compile(query, provider, options);
        compiled.DataSourceProgress += (_, args) => progress.Add(args);
        var profile = compiled.RunWithProfile(cancellation.Token);

        Assert.AreEqual(0, profile.Result.Count);
        Assert.AreEqual("csvProfile", resolver.Requests.Single().ProfileName);
        AssertStaticParameters(resolver.Requests.Single().Parameters, expectedPathArgument);

        var getTable = recorder.GetTableCalls.Last();
        Assert.AreEqual("file", getTable.Name);
        AssertStaticParameters(getTable.Parameters, expectedPathArgument);
        Assert.AreEqual(RuntimeSettingValue, getTable.Metadata.SourceRuntimeSettings[RuntimeSettingName]);
        Assert.IsFalse(getTable.Metadata.CancellationCanBeCanceled);
        AssertReadModifier(getTable.Columns, "Name", ColumnReadModifiers.Trim, "true");
        AssertReadModifier(getTable.Columns, "Name", SourceNameModifier, "FullName");
        AssertReadModifier(getTable.Columns, "Amount", ColumnReadModifiers.Culture, "pl-PL");
        AssertReadModifier(getTable.Columns, "Status", SourceIndexModifier, "3");

        var runtimeSettings = recorder.RuntimeSettingsCalls.Last();
        Assert.AreEqual("#csv", runtimeSettings.Identity.SchemaName);
        Assert.AreEqual("file", runtimeSettings.Identity.MethodName);
        Assert.AreEqual("r", runtimeSettings.Identity.Alias);
        Assert.IsFalse(string.IsNullOrWhiteSpace(runtimeSettings.Identity.SourceContextId));
        AssertStaticParameters(runtimeSettings.Parameters, expectedPathArgument);
        Assert.AreEqual(0, runtimeSettings.Metadata.SourceRuntimeSettings.Count);

        var describe = recorder.DescribeSourceCalls.Last();
        Assert.AreEqual(runtimeSettings.Identity, describe.Identity);
        AssertStaticParameters(describe.Parameters, expectedPathArgument);
        Assert.AreEqual(RuntimeSettingValue, describe.Metadata.SourceRuntimeSettings[RuntimeSettingName]);
        Assert.AreEqual(describe.Identity.SourceContextId, describe.Metadata.QueryId);
        AssertReadModifier(describe.Columns, "Status", SourceIndexModifier, "3");

        var plan = recorder.PlanCalls.Last();
        Assert.AreEqual(runtimeSettings.Identity, plan.Request.Identity);
        AssertStaticParameters(plan.Parameters, expectedPathArgument);
        Assert.AreEqual(RuntimeSettingValue, plan.Request.SourceRuntimeSettings[RuntimeSettingName]);
        AssertRequiredColumn(plan.Request.RequiredColumns, "Name", ColumnReadModifiers.Trim, "true");
        AssertRequiredColumn(plan.Request.RequiredColumns, "Name", SourceNameModifier, "FullName");
        AssertRequiredColumn(plan.Request.RequiredColumns, "Amount", ColumnReadModifiers.Culture, "pl-PL");
        AssertRequiredColumn(plan.Request.RequiredColumns, "Status", SourceIndexModifier, "3");
        Assert.IsInstanceOfType<SourcePredicateLogical>(plan.Request.Predicate);
        Assert.AreEqual(1, plan.Request.OrderBy.Count);
        Assert.AreEqual("Name", plan.Request.OrderBy[0].Column.Name);
        Assert.AreEqual(OrderDirection.Descending, plan.Request.OrderBy[0].Direction);
        Assert.AreEqual(1L, plan.Request.Skip);
        Assert.AreEqual(2L, plan.Request.Take);

        var rowSource = recorder.RowSourceCalls.Single();
        Assert.AreEqual(typeof(CsvRow), rowSource.RequestedRowType);
        AssertStaticParameters(rowSource.Parameters, expectedPathArgument);
        Assert.AreEqual(runtimeSettings.Identity, rowSource.Execution.Plan.Identity);
        Assert.AreEqual(3, rowSource.Execution.Plan.AcceptedColumns.Count);
        AssertRequiredColumn(rowSource.Execution.Plan.AcceptedColumns, "Name", ColumnReadModifiers.Trim, "true");
        Assert.IsInstanceOfType<SourcePredicateLogical>(rowSource.Execution.Plan.AcceptedPredicate);
        Assert.AreEqual(0, rowSource.Execution.Plan.AcceptedOrderBy.Count);
        Assert.IsFalse(rowSource.Execution.Plan.AcceptedSkip.HasValue);
        Assert.IsFalse(rowSource.Execution.Plan.AcceptedTake.HasValue);
        Assert.AreEqual(RuntimeSettingValue, rowSource.Execution.SourceRuntimeSettings[RuntimeSettingName]);
        Assert.IsTrue(rowSource.Execution.CancellationCanBeCanceled);
        Assert.IsTrue(rowSource.Execution.Diagnostics.IsEnabled);
        Assert.IsTrue(rowSource.Execution.AllColumns.Any(column => column.ColumnName == "Name"));

        CollectionAssert.AreEqual(
            new[]
            {
                DataSourcePhase.Begin,
                DataSourcePhase.End
            },
            progress.Select(static item => item.Phase).ToArray());
        Assert.IsTrue(progress.All(item => item.QueryId == rowSource.Execution.QueryId));
    }

    [TestMethod]
    public void Engine_WhenSourceAcceptsPlan_ShouldPassAcceptedExecutionPlanToRowSource()
    {
        var path = WriteTempCsv(string.Empty);
        var recorder = new CsvDataSourceApiRecorder
        {
            PlanResultFactory = CreateAcceptedPlanResult
        };
        var provider = new CsvSchemaProvider(recorder);
        var query =
            "table CsvShape { Name: string trim, Amount: decimal };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select r.Name from Rows({SqlString(path)}) r order by r.Name desc skip 1 take 2";

        var table = Run(query, provider);

        Assert.AreEqual(0, table.Count);
        var request = recorder.PlanCalls.Single().Request;
        var execution = recorder.RowSourceCalls.Single().Execution;

        Assert.AreEqual(request.Identity, execution.Plan.Identity);
        Assert.AreEqual(1, execution.Plan.AcceptedOrderBy.Count);
        Assert.AreEqual("Name", execution.Plan.AcceptedOrderBy[0].Column.Name);
        Assert.AreEqual(OrderDirection.Descending, execution.Plan.AcceptedOrderBy[0].Direction);
        Assert.AreEqual(1L, execution.Plan.AcceptedSkip);
        Assert.AreEqual(2L, execution.Plan.AcceptedTake);
        AssertRequiredColumn(execution.Plan.AcceptedColumns, "Name", ColumnReadModifiers.Trim, "true");
        Assert.IsTrue(execution.AllColumns.Any(column => column.ColumnName == "Name"));
    }

    [TestMethod]
    public void Engine_WhenDirectAndCoupledSourcesAreUsed_ShouldPopulateSourceIdentity()
    {
        var directRecorder = new CsvDataSourceApiRecorder();
        var directPath = WriteTempCsv(string.Empty);
        Run($"select 1 from #csv.file({SqlString(directPath)}) d", new CsvSchemaProvider(directRecorder));

        var directIdentity = directRecorder.DescribeSourceCalls.Last().Identity;
        Assert.AreEqual("#csv", directIdentity.SchemaName);
        Assert.AreEqual("file", directIdentity.MethodName);
        Assert.AreEqual("d", directIdentity.Alias);
        Assert.IsFalse(string.IsNullOrWhiteSpace(directIdentity.SourceContextId));

        var coupledRecorder = new CsvDataSourceApiRecorder();
        var coupledPath = WriteTempCsv(string.Empty);
        var coupledQuery =
            "table CsvShape { Name: string };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Rows.Name from Rows({SqlString(coupledPath)}) Rows";
        Run(coupledQuery, new CsvSchemaProvider(coupledRecorder));

        var coupledIdentity = coupledRecorder.DescribeSourceCalls.Last().Identity;
        Assert.AreEqual("#csv", coupledIdentity.SchemaName);
        Assert.AreEqual("file", coupledIdentity.MethodName);
        Assert.AreEqual("Rows", coupledIdentity.Alias);
        Assert.IsFalse(string.IsNullOrWhiteSpace(coupledIdentity.SourceContextId));
    }

    [TestMethod]
    public void Engine_WhenSourceArgumentIsRuntimeParameter_ShouldPassItOnlyToExecution()
    {
        var path = WriteTempCsv(string.Empty);
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder);
        const string query =
            "param(path: string) " +
            "table CsvShape { Name: string };" +
            "couple #csv.file with table CsvShape as Rows;" +
            "select Name from Rows($path)";

        var compiled = Compile(query, provider);
        compiled.Parameters["path"] = path;
        var table = compiled.Run();

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(0, recorder.GetTableCalls.Last().Parameters.Count);
        Assert.AreEqual(0, recorder.DescribeSourceCalls.Last().Parameters.Count);
        Assert.AreEqual(0, recorder.PlanCalls.Last().Parameters.Count);
        Assert.AreEqual(path, recorder.RowSourceCalls.Single().Parameters.Single());
    }

    [TestMethod]
    public void Engine_WhenSourceReportsContractDiagnostics_ShouldDeduplicateAndMapToModifierSpan()
    {
        var recorder = new CsvDataSourceApiRecorder();
        var warning = SourceContractDiagnostic.Warning(
            "Encoding modifier 'windows-1250' is ignored by #csv.file().",
            "CsvUnsupportedEncoding") with
        {
            ColumnName = "Name",
            ModifierKey = ColumnReadModifiers.Encoding
        };
        recorder.DescribeSourceContractDiagnostics.Add(warning);
        recorder.PlanContractDiagnostics.Add(warning);
        const string query =
            "table CsvShape { Name: string encoding 'windows-1250' };" +
            "couple #csv.file with table CsvShape as Rows;" +
            "select Name from Rows('diagnostic.csv')";

        var result = CompileWithDiagnostics(query, new CsvSchemaProvider(recorder));

        Assert.IsTrue(result.Succeeded);
        var warnings = result.Warnings
            .Where(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning)
            .ToArray();
        Assert.AreEqual(1, warnings.Length);
        StringAssert.Contains(warnings[0].Message, "origin=DescribeSource");
        StringAssert.Contains(warnings[0].Message, "sourceCode=CsvUnsupportedEncoding");
        StringAssert.Contains(warnings[0].Message, "column=Name");
        StringAssert.Contains(warnings[0].Message, "modifier=encoding");
        Assert.AreEqual(CreateExpectedSpan(query, "encoding 'windows-1250'"), warnings[0].Span);
    }

    private static void AssertStaticParameters(IReadOnlyList<object?> parameters, string path)
    {
        Assert.AreEqual(3, parameters.Count);
        Assert.AreEqual(path, parameters[0]);
        Assert.AreEqual(true, parameters[1]);
        Assert.AreEqual(3, parameters[2]);
    }

    private static void AssertStaticParameters(object?[] parameters, string path)
    {
        AssertStaticParameters((IReadOnlyList<object?>)parameters, path);
    }

    private static void AssertReadModifier(
        IEnumerable<ISchemaColumn> columns,
        string columnName,
        string key,
        string value)
    {
        Assert.AreEqual(value, columns.Single(column => column.ColumnName == columnName).ReadModifiers[key]);
    }

    private static void AssertRequiredColumn(
        IEnumerable<SourceColumnRef> columns,
        string columnName,
        string key,
        string value)
    {
        Assert.AreEqual(value, columns.Single(column => column.Name == columnName).ReadModifiers[key]);
    }

    private static TextSpan CreateExpectedSpan(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, start);
        return new TextSpan(start, text.Length);
    }

    private static SourcePlanResult CreateAcceptedPlanResult(SourcePlanRequest request)
    {
        return new SourcePlanResult
        {
            ExecutionPlan = new SourceExecutionPlan
            {
                Identity = request.Identity,
                AcceptedColumns = request.RequiredColumns,
                AcceptedPredicate = request.Predicate,
                AcceptedOrderBy = request.OrderBy,
                AcceptedSkip = request.Skip,
                AcceptedTake = request.Take
            },
            AcceptedColumns = request.RequiredColumns,
            AcceptedPredicate = request.Predicate,
            AcceptedOrderBy = request.OrderBy,
            AcceptedSkip = request.Skip,
            AcceptedTake = request.Take
        };
    }
}
