using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git.Tests;

[TestClass]
public sealed class GitDatasourceApiContractTests : GitExampleTestBase
{
    private const string RuntimeRepository = "docs";
    private const string RuntimeTokenName = "GIT_EXAMPLE_TRACE_TOKEN";
    private const string RuntimeTokenValue = "trace-token";

    [TestMethod]
    public void Engine_WhenRunningDirectGitQuery_ShouldPropagateDatasourceApiContexts()
    {
        var recorder = new GitDataSourceApiRecorder();
        recorder.RuntimeSettingRequirements.Add(new SourceRuntimeSettingRequirement(
            RuntimeTokenName,
            Required: false,
            Secret: false,
            SourceRuntimeSettingPhase.All,
            "Token used by Git datasource API contract tests."));
        var settings = new Dictionary<string, string>
        {
            [GitSchema.RepositoryRuntimeSetting] = RuntimeRepository,
            [RuntimeTokenName] = RuntimeTokenValue
        };
        var resolver = new StaticSettingsResolver(settings);
        var options = new CompilationOptions(
                usePrimitiveTypeValidation: false,
                sourceRuntimeSettingsResolver: resolver)
            .WithInstrumentationMode(QueryInstrumentationMode.SourceBoundaries);
        var provider = new GitSchemaProvider(recorder);
        const string query =
            "select g.ShortSha from #git.commits('musoq') g " +
            "where g.AuthorName = 'Bob Evaluator' " +
            "order by g.AuthoredAt desc skip 0 take 1";
        var progress = new List<DataSourceEventArgs>();
        using var cancellation = new CancellationTokenSource();

        var compiled = Compile(query, provider, options);
        compiled.DataSourceProgress += (_, args) => progress.Add(args);
        var profile = compiled.RunWithProfile(cancellation.Token);

        Assert.AreEqual(1, profile.Result.Count);
        Assert.AreEqual("b2c3d4e", profile.Result[0][0]);

        var runtimeSettingsRequest = resolver.Requests.Single();
        Assert.AreEqual("#git", runtimeSettingsRequest.Identity.SchemaName);
        Assert.AreEqual(GitSchema.Commits, runtimeSettingsRequest.Identity.MethodName);
        Assert.AreEqual("g", runtimeSettingsRequest.Identity.Alias);
        Assert.IsFalse(string.IsNullOrWhiteSpace(runtimeSettingsRequest.Identity.SourceContextId));
        Assert.IsNull(runtimeSettingsRequest.ProfileName);
        Assert.AreEqual("musoq", runtimeSettingsRequest.Parameters.Single());
        Assert.IsTrue(runtimeSettingsRequest.Requirements.Any(item => item.Name == GitSchema.RepositoryRuntimeSetting));
        Assert.IsTrue(runtimeSettingsRequest.Requirements.Any(item => item.Name == RuntimeTokenName));

        var runtimeSettings = recorder.RuntimeSettingsCalls.Single();
        Assert.AreEqual(runtimeSettingsRequest.Identity, runtimeSettings.Identity);
        Assert.AreEqual("musoq", runtimeSettings.Parameters.Single());
        Assert.AreEqual(0, runtimeSettings.Metadata.SourceRuntimeSettings.Count);
        Assert.IsFalse(runtimeSettings.Metadata.CancellationCanBeCanceled);

        var describe = recorder.DescribeSourceCalls.Single();
        Assert.AreEqual(runtimeSettings.Identity, describe.Identity);
        Assert.AreEqual("musoq", describe.Parameters.Single());
        Assert.AreEqual(typeof(GitCommitRow), describe.RowType);
        AssertContainsColumn(describe.Columns, nameof(GitCommitRow.ShortSha));
        Assert.AreEqual(RuntimeRepository, describe.Metadata.SourceRuntimeSettings[GitSchema.RepositoryRuntimeSetting]);
        Assert.AreEqual(RuntimeTokenValue, describe.Metadata.SourceRuntimeSettings[RuntimeTokenName]);
        Assert.AreEqual(describe.Identity.SourceContextId, describe.Metadata.QueryId);

        var plan = recorder.PlanCalls.Single();
        Assert.AreEqual(runtimeSettings.Identity, plan.Request.Identity);
        Assert.AreEqual("musoq", plan.Parameters.Single());
        Assert.AreEqual(RuntimeRepository, plan.Request.SourceRuntimeSettings[GitSchema.RepositoryRuntimeSetting]);
        Assert.AreEqual(RuntimeTokenValue, plan.Request.SourceRuntimeSettings[RuntimeTokenName]);
        AssertRequiredColumn(plan.Request.RequiredColumns, nameof(GitCommitRow.ShortSha));
        AssertRequiredColumn(plan.Request.RequiredColumns, nameof(GitCommitRow.AuthorName));
        AssertRequiredColumn(plan.Request.RequiredColumns, nameof(GitCommitRow.AuthoredAt));
        Assert.IsInstanceOfType<SourcePredicateComparison>(plan.Request.Predicate);
        Assert.AreEqual(1, plan.Request.OrderBy.Count);
        Assert.AreEqual(nameof(GitCommitRow.AuthoredAt), plan.Request.OrderBy[0].Column.Name);
        Assert.AreEqual(OrderDirection.Descending, plan.Request.OrderBy[0].Direction);
        Assert.AreEqual(0L, plan.Request.Skip);
        Assert.AreEqual(1L, plan.Request.Take);

        var rowSource = recorder.RowSourceCalls.Single();
        Assert.AreEqual(typeof(GitCommitRow), rowSource.RequestedRowType);
        Assert.AreEqual("musoq", rowSource.Parameters.Single());
        Assert.AreEqual(runtimeSettings.Identity, rowSource.Execution.Plan.Identity);
        AssertRequiredColumn(rowSource.Execution.Plan.AcceptedColumns, nameof(GitCommitRow.ShortSha));
        Assert.IsInstanceOfType<SourcePredicateComparison>(rowSource.Execution.Plan.AcceptedPredicate);
        Assert.AreEqual(1, rowSource.Execution.Plan.AcceptedOrderBy.Count);
        Assert.AreEqual(0L, rowSource.Execution.Plan.AcceptedSkip);
        Assert.AreEqual(1L, rowSource.Execution.Plan.AcceptedTake);
        Assert.AreEqual(RuntimeRepository, rowSource.Execution.SourceRuntimeSettings[GitSchema.RepositoryRuntimeSetting]);
        Assert.AreEqual(RuntimeTokenValue, rowSource.Execution.SourceRuntimeSettings[RuntimeTokenName]);
        Assert.IsTrue(rowSource.Execution.CancellationCanBeCanceled);
        Assert.IsTrue(rowSource.Execution.Diagnostics.IsEnabled);
        Assert.IsTrue(rowSource.Execution.AllColumns.Any(column => column.ColumnName == nameof(GitCommitRow.ShortSha)));

        Assert.IsTrue(recorder.GetTableCalls.Any(call =>
            call.Name == GitSchema.Commits &&
            call.Columns.Any(column => column.ColumnName == nameof(GitCommitRow.ShortSha))));
        Assert.IsTrue(recorder.GetTableCalls.Any(call =>
            call.Metadata.SourceRuntimeSettings.TryGetValue(RuntimeTokenName, out var token) &&
            token == RuntimeTokenValue));

        CollectionAssert.AreEqual(
            new[]
            {
                DataSourcePhase.Begin,
                DataSourcePhase.RowsKnown,
                DataSourcePhase.RowsRead,
                DataSourcePhase.End
            },
            progress.Select(static item => item.Phase).ToArray());
        Assert.IsTrue(progress.All(item => item.QueryId == rowSource.Execution.QueryId));
    }

    [TestMethod]
    public void Engine_WhenSourceAcceptsPlan_ShouldPassAcceptedExecutionPlanToRowSource()
    {
        var recorder = new GitDataSourceApiRecorder
        {
            PlanResultFactory = CreateAcceptedPlanResult
        };
        var provider = new GitSchemaProvider(recorder);
        const string query =
            "select g.ShortSha from #git.commits() g order by g.AuthoredAt desc skip 1 take 2";

        var table = Run(query, provider);

        Assert.AreEqual(2, table.Count);
        var request = recorder.PlanCalls.Single().Request;
        var execution = recorder.RowSourceCalls.Single().Execution;

        Assert.AreEqual(request.Identity, execution.Plan.Identity);
        Assert.AreEqual(1, execution.Plan.AcceptedOrderBy.Count);
        Assert.AreEqual(nameof(GitCommitRow.AuthoredAt), execution.Plan.AcceptedOrderBy[0].Column.Name);
        Assert.AreEqual(OrderDirection.Descending, execution.Plan.AcceptedOrderBy[0].Direction);
        Assert.AreEqual(1L, execution.Plan.AcceptedSkip);
        Assert.AreEqual(2L, execution.Plan.AcceptedTake);
        AssertRequiredColumn(execution.Plan.AcceptedColumns, nameof(GitCommitRow.ShortSha));
        Assert.IsTrue(execution.AllColumns.Any(column => column.ColumnName == nameof(GitCommitRow.ShortSha)));
    }

    [TestMethod]
    public void Engine_WhenSourceArgumentIsRuntimeParameter_ShouldUseDefaultForMetadataAndRuntimeValueForExecution()
    {
        var recorder = new GitDataSourceApiRecorder();
        var provider = new GitSchemaProvider(recorder);
        const string query =
            "param(repo: string = 'musoq') " +
            "select ShortSha from #git.commits($repo) order by AuthoredAt";

        var compiled = Compile(query, provider);
        compiled.Parameters["repo"] = "docs";
        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(recorder.GetTableCalls.Any(call =>
            call.Parameters.Count == 1 &&
            Equals(call.Parameters[0], "musoq")));
        Assert.AreEqual("docs", recorder.GetTableCalls.Last().Parameters.Single());
        Assert.AreEqual("musoq", recorder.RuntimeSettingsCalls.Last().Parameters.Single());
        Assert.IsFalse(recorder.DescribeSourceCalls.Last().Parameters.Contains("docs"));
        Assert.IsFalse(recorder.PlanCalls.Last().Parameters.Contains("docs"));
        Assert.AreEqual("docs", recorder.RowSourceCalls.Single().Parameters.Single());
    }

    [TestMethod]
    public void Schema_WhenRawConstructorsAreRequested_ShouldRecordMetadataAndReturnCommitsConstructor()
    {
        using var cancellation = new CancellationTokenSource();
        var recorder = new GitDataSourceApiRecorder();
        var schema = new GitSchema(recorder);
        var context = CreateMetadataContext(
            queryId: "raw-constructor-query",
            cancellationToken: cancellation.Token);

        var allConstructors = schema.GetRawConstructors(context);
        var commitsConstructors = schema.GetRawConstructors(GitSchema.Commits, context);

        Assert.IsTrue(allConstructors.Any(item => item.MethodName == GitSchema.Commits));
        Assert.IsTrue(commitsConstructors.All(item => item.MethodName == GitSchema.Commits));
        Assert.AreEqual(2, recorder.RawConstructorCalls.Count);
        Assert.IsNull(recorder.RawConstructorCalls[0].MethodName);
        Assert.AreEqual(GitSchema.Commits, recorder.RawConstructorCalls[1].MethodName);
        Assert.AreEqual("raw-constructor-query", recorder.RawConstructorCalls[0].Metadata.QueryId);
        Assert.IsTrue(recorder.RawConstructorCalls[0].Metadata.CancellationCanBeCanceled);
    }

    [TestMethod]
    public void Schema_WhenRuntimeSettingsAreDescribed_ShouldRecordIdentityAndReturnRequirements()
    {
        var recorder = new GitDataSourceApiRecorder();
        var schema = new GitSchema(recorder);
        var identity = new SourceIdentity("#git", GitSchema.Commits, "source-id", "g");
        var metadata = CreateMetadataContext(queryId: "runtime-settings-query");

        var requirements = schema.DescribeSourceRuntimeSettings(
            GitSchema.Commits,
            new SourceRuntimeSettingsDescribeContext(identity, metadata),
            "musoq");

        Assert.IsTrue(requirements.Any(item => item.Name == GitSchema.RepositoryRuntimeSetting));
        var call = recorder.RuntimeSettingsCalls.Single();
        Assert.AreEqual(identity, call.Identity);
        Assert.AreEqual("runtime-settings-query", call.Metadata.QueryId);
        Assert.AreEqual("musoq", call.Parameters.Single());
    }

    [TestMethod]
    public void Schema_WhenSourceIsDescribed_ShouldReturnMetadataRowTypeAndRecordContext()
    {
        var recorder = new GitDataSourceApiRecorder();
        var schema = new GitSchema(recorder);
        var identity = new SourceIdentity("#git", GitSchema.Commits, "source-id", "g");
        var metadata = CreateMetadataContext(
            columns:
            [
                new Musoq.Schema.DataSources.SchemaColumn(nameof(GitCommitRow.ShortSha), 0, typeof(string))
            ],
            queryId: "describe-query",
            sourceRuntimeSettings: new Dictionary<string, string>
            {
                [GitSchema.RepositoryRuntimeSetting] = RuntimeRepository
            });

        var descriptor = schema.DescribeSource(
            GitSchema.Commits,
            new SourceDescribeContext(identity, metadata),
            "musoq");

        Assert.AreEqual(identity, descriptor.Identity);
        Assert.AreEqual(typeof(GitCommitRow), descriptor.RowType);
        AssertContainsColumn(descriptor.Columns, nameof(GitCommitRow.ShortSha));
        var call = recorder.DescribeSourceCalls.Single();
        Assert.AreEqual(identity, call.Identity);
        Assert.AreEqual("describe-query", call.Metadata.QueryId);
        Assert.AreEqual(RuntimeRepository, call.Metadata.SourceRuntimeSettings[GitSchema.RepositoryRuntimeSetting]);
        Assert.AreEqual("musoq", call.Parameters.Single());
    }

    [TestMethod]
    public void Engine_WhenSourceReportsContractDiagnostics_ShouldDeduplicateDiagnostic()
    {
        var recorder = new GitDataSourceApiRecorder();
        var warning = SourceContractDiagnostic.Warning(
            "Git contract diagnostic emitted by recorder.",
            "GitContractProbe");
        recorder.DescribeSourceContractDiagnostics.Add(warning);
        recorder.PlanContractDiagnostics.Add(warning);

        var result = CompileWithDiagnostics(
            "select ShortSha from #git.commits() g",
            new GitSchemaProvider(recorder));

        Assert.IsTrue(result.Succeeded);
        var warnings = result.Warnings
            .Where(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning)
            .ToArray();
        Assert.AreEqual(1, warnings.Length);
        StringAssert.Contains(warnings[0].Message, "origin=DescribeSource");
        StringAssert.Contains(warnings[0].Message, "sourceCode=GitContractProbe");
    }

    private static void AssertContainsColumn(
        IEnumerable<ISchemaColumn> columns,
        string columnName)
    {
        Assert.IsTrue(columns.Any(column => column.ColumnName == columnName), columnName);
    }

    private static void AssertRequiredColumn(
        IEnumerable<SourceColumnRef> columns,
        string columnName)
    {
        Assert.IsTrue(columns.Any(column => column.Name == columnName), columnName);
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
