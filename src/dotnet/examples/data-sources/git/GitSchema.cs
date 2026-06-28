using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;

namespace Musoq.Examples.DataSources.Git;

public sealed class GitSchema : SchemaBase
{
    public const string SchemaName = "git";
    public const string Commits = "commits";
    public const string RepositoryRuntimeSetting = "GIT_EXAMPLE_REPOSITORY";

    private readonly IGitHistoryStore _store;
    private readonly GitDataSourceApiRecorder? _recorder;

    public GitSchema()
        : this(InMemoryGitHistoryStore.CreateDefault())
    {
    }

    public GitSchema(IGitHistoryStore store)
        : this(store, null)
    {
    }

    internal GitSchema(GitDataSourceApiRecorder recorder)
        : this(InMemoryGitHistoryStore.CreateDefault(), recorder)
    {
    }

    internal GitSchema(IGitHistoryStore store, GitDataSourceApiRecorder? recorder)
        : base(SchemaName, CreateLibrary())
    {
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _recorder = recorder;
        AddTable<GitCommitsTable>(Commits);
        AddSource<GitCommitsSource>(Commits, store);
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        var table = base.GetTableByName(name, metadataContext, parameters);
        _recorder?.GetTableCalls.Add(new GitGetTableCall(
            name,
            GitSourceMetadataSnapshot.From(metadataContext),
            parameters.ToArray(),
            table.Columns));

        return table;
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters);
        _recorder?.DescribeSourceCalls.Add(new GitDescribeSourceCall(
            name,
            context.Identity,
            GitSourceMetadataSnapshot.From(context.MetadataContext),
            parameters.ToArray(),
            descriptor.Columns,
            descriptor.RowType));

        if (_recorder == null ||
            (_recorder.DescribeSourceDiagnostics.Count == 0 &&
             _recorder.DescribeSourceContractDiagnostics.Count == 0))
            return descriptor;

        return descriptor with
        {
            Diagnostics = descriptor.Diagnostics.Concat(_recorder.DescribeSourceDiagnostics).ToArray(),
            ContractDiagnostics = descriptor.ContractDiagnostics.Concat(_recorder.DescribeSourceContractDiagnostics).ToArray()
        };
    }

    public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters)
    {
        if (!string.Equals(name, Commits, StringComparison.OrdinalIgnoreCase))
            return base.DescribeSourceRuntimeSettings(name, context, parameters);

        _recorder?.RuntimeSettingsCalls.Add(new GitRuntimeSettingsCall(
            name,
            context.Identity,
            GitSourceMetadataSnapshot.From(context.MetadataContext),
            parameters.ToArray()));

        SourceRuntimeSettingRequirement[] requirements =
        [
            new SourceRuntimeSettingRequirement(
                RepositoryRuntimeSetting,
                Required: false,
                Secret: false,
                SourceRuntimeSettingPhase.All,
                "Optional repository name used by #git.commits() when no repository argument is supplied.")
        ];

        return _recorder?.RuntimeSettingRequirements.Count > 0
            ? requirements.Concat(_recorder.RuntimeSettingRequirements).ToArray()
            : requirements;
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        if (!string.Equals(name, Commits, StringComparison.OrdinalIgnoreCase))
            return SourcePlanResult.RejectAll(request);

        _recorder?.PlanCalls.Add(new GitPlanCall(
            name,
            request,
            parameters.ToArray()));

        if (_recorder?.PlanResultFactory != null)
            return AddRecordedPlanDiagnostics(_recorder.PlanResultFactory(request));

        var predicatePlan = GitCommitPlan.PlanPredicate(request.Predicate);
        var acceptedPredicate = predicatePlan.AcceptedPredicate;
        var residualPredicate = predicatePlan.ResidualPredicate;
        var acceptsOrderBy = request.OrderBy.All(order => GitCommitPlan.CanPushDownColumn(order.Column.Name));
        var acceptedOrderBy = acceptsOrderBy ? request.OrderBy : [];
        var residualOrderBy = acceptsOrderBy ? [] : request.OrderBy;
        var acceptsWindow = residualPredicate == null && residualOrderBy.Count == 0;

        var acceptedColumns = GitCommitPlan.CanReadColumns(request.RequiredColumns) ? request.RequiredColumns : [];
        var acceptedSkip = acceptsWindow ? request.Skip : null;
        var acceptedTake = acceptsWindow ? request.Take : null;
        var residualSkip = acceptsWindow ? null : request.Skip;
        var residualTake = acceptsWindow ? null : request.Take;
        var resolvedRepository = ResolveRepository(parameters, request.SourceRuntimeSettings);
        var cardinality = EstimateCardinality(
            resolvedRepository,
            acceptedPredicate,
            acceptedOrderBy,
            acceptedSkip,
            acceptedTake);
        var diagnostics = CreatePlanningDiagnostics(
            request,
            residualPredicate,
            residualOrderBy,
            residualSkip,
            residualTake);

        return AddRecordedPlanDiagnostics(new SourcePlanResult
        {
            ExecutionPlan = new SourceExecutionPlan
            {
                Identity = request.Identity,
                AcceptedColumns = acceptedColumns,
                AcceptedPredicate = acceptedPredicate,
                AcceptedOrderBy = acceptedOrderBy,
                AcceptedSkip = acceptedSkip,
                AcceptedTake = acceptedTake,
                Properties = CreatePlanProperties(
                    resolvedRepository,
                    CreatePlanningNotes(
                        acceptedPredicate,
                        residualPredicate,
                        acceptedOrderBy,
                        residualOrderBy,
                        acceptedSkip,
                        acceptedTake,
                        residualSkip,
                        residualTake))
            },
            AcceptedColumns = acceptedColumns,
            AcceptedPredicate = acceptedPredicate,
            ResidualPredicate = residualPredicate,
            AcceptedOrderBy = acceptedOrderBy,
            ResidualOrderBy = residualOrderBy,
            AcceptedSkip = acceptedSkip,
            ResidualSkip = residualSkip,
            AcceptedTake = acceptedTake,
            ResidualTake = residualTake,
            Cardinality = cardinality,
            Diagnostics = diagnostics
        });
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        _recorder?.RowSourceCalls.Add(new GitRowSourceCall(
            name,
            GitSourceExecutionSnapshot.From(executionContext),
            parameters.ToArray(),
            typeof(T)));

        return base.GetRowSource<T>(name, executionContext, parameters);
    }

    public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
    {
        ArgumentNullException.ThrowIfNull(metadataContext);
        _recorder?.RawConstructorCalls.Add(new GitRawConstructorCall(
            null,
            GitSourceMetadataSnapshot.From(metadataContext)));

        return base.GetRawConstructors(metadataContext);
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(metadataContext);
        _recorder?.RawConstructorCalls.Add(new GitRawConstructorCall(
            methodName,
            GitSourceMetadataSnapshot.From(metadataContext)));

        return base.GetRawConstructors(metadataContext)
            .Where(constructor => string.Equals(constructor.MethodName, methodName, StringComparison.Ordinal))
            .ToArray();
    }

    private SourcePlanResult AddRecordedPlanDiagnostics(SourcePlanResult result)
    {
        if (_recorder == null ||
            (_recorder.PlanDiagnostics.Count == 0 &&
             _recorder.PlanContractDiagnostics.Count == 0))
            return result;

        return result with
        {
            Diagnostics = result.Diagnostics.Concat(_recorder.PlanDiagnostics).ToArray(),
            ContractDiagnostics = result.ContractDiagnostics.Concat(_recorder.PlanContractDiagnostics).ToArray()
        };
    }

    private CardinalityEstimate EstimateCardinality(
        string? repository,
        SourcePredicateExpression? acceptedPredicate,
        IReadOnlyList<OrderByExpression> acceptedOrderBy,
        long? acceptedSkip,
        long? acceptedTake)
    {
        var rows = _store
            .GetCommits(repository)
            .Select(commit => new GitCommitRow(commit, _store.GetStats));
        var plannedRows = GitCommitPlan.Apply(
                rows,
                SourceExecutionPlan.Empty(SourceIdentity.Empty) with
                {
                    AcceptedPredicate = acceptedPredicate,
                    AcceptedOrderBy = acceptedOrderBy,
                    AcceptedSkip = acceptedSkip,
                    AcceptedTake = acceptedTake
                })
            .LongCount();

        return CardinalityEstimate.Exact(
            plannedRows,
            "The Git example source is backed by an in-memory deterministic history.");
    }

    private static string? ResolveRepository(
        object?[] parameters,
        IReadOnlyDictionary<string, string> sourceRuntimeSettings)
    {
        if (parameters.Length > 0 && parameters[0] is string repository && !string.IsNullOrWhiteSpace(repository))
            return repository;

        return sourceRuntimeSettings.TryGetValue(RepositoryRuntimeSetting, out var runtimeRepository) &&
            !string.IsNullOrWhiteSpace(runtimeRepository)
                ? runtimeRepository
                : null;
    }

    private static IReadOnlyDictionary<string, object?> CreatePlanProperties(
        string? repository,
        string planningNotes)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [GitSourcePlanProperties.PlanningNotes] = planningNotes
        };

        if (!string.IsNullOrWhiteSpace(repository))
            properties[GitSourcePlanProperties.Repository] = repository;

        return properties;
    }

    private static string CreatePlanningNotes(
        SourcePredicateExpression? acceptedPredicate,
        SourcePredicateExpression? residualPredicate,
        IReadOnlyCollection<OrderByExpression> acceptedOrderBy,
        IReadOnlyCollection<OrderByExpression> residualOrderBy,
        long? acceptedSkip,
        long? acceptedTake,
        long? residualSkip,
        long? residualTake)
    {
        var notes = new List<string>();

        if (acceptedPredicate != null)
            notes.Add("accepted predicate");
        if (residualPredicate != null)
            notes.Add("residual predicate");
        if (acceptedOrderBy.Count > 0)
            notes.Add("accepted order");
        if (residualOrderBy.Count > 0)
            notes.Add("residual order");
        if (acceptedSkip.HasValue || acceptedTake.HasValue)
            notes.Add("accepted slice");
        if (residualSkip.HasValue || residualTake.HasValue)
            notes.Add("residual slice");

        return notes.Count == 0
            ? "no pushdown requested"
            : string.Join(", ", notes);
    }

    private static IReadOnlyList<OptimizationDiagnostic> CreatePlanningDiagnostics(
        SourcePlanRequest request,
        SourcePredicateExpression? residualPredicate,
        IReadOnlyList<OrderByExpression> residualOrderBy,
        long? residualSkip,
        long? residualTake)
    {
        var diagnostics = new List<OptimizationDiagnostic>();
        var target = FormatSourceTarget(request.Identity);

        if (residualPredicate != null && GitCommitPlan.ReferencesExpensiveColumn(residualPredicate))
        {
            diagnostics.Add(CreateOptimizationWarning(
                "GitPredicatePushdown",
                target,
                "Git stats columns are loaded lazily, so predicates over stats columns remain evaluator residual work."));
        }

        if (residualOrderBy.Any(order => GitCommitPlan.IsExpensiveColumn(order.Column.Name)))
        {
            diagnostics.Add(CreateOptimizationWarning(
                "GitOrderPushdown",
                target,
                "Git stats columns are loaded lazily, so ordering over stats columns remains evaluator residual work."));
        }

        if (residualSkip.HasValue || residualTake.HasValue)
        {
            diagnostics.Add(CreateOptimizationWarning(
                "GitSlicePushdown",
                target,
                "Git source planning cannot push down skip/take while residual predicate or ordering work can still change row membership or order."));
        }

        return diagnostics;
    }

    private static OptimizationDiagnostic CreateOptimizationWarning(
        string optimization,
        string target,
        string reason)
    {
        return OptimizationDiagnostic.Warning(reason) with
        {
            Optimization = optimization,
            Target = target,
            Reason = reason
        };
    }

    private static string FormatSourceTarget(SourceIdentity identity)
    {
        var schema = string.IsNullOrWhiteSpace(identity.SchemaName)
            ? "#git"
            : identity.SchemaName;
        var method = string.IsNullOrWhiteSpace(identity.MethodName)
            ? Commits
            : identity.MethodName;
        var alias = string.IsNullOrWhiteSpace(identity.Alias)
            ? string.Empty
            : $" as {identity.Alias}";

        return $"{schema}.{method}(){alias}";
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
