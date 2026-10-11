using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Converter;

public sealed class CompiledTypedProfileQuery<TOut> : IProfiledTypedRunnable<TOut>, IQueryProgressSource, ITypedQueryDiagnosticsProvider
{
    private readonly TypedProfileRunnableFactory<TOut> _factory;
    private readonly ISchemaProvider _schemaProvider;
    private readonly TypedRunState _runState;

    internal CompiledTypedProfileQuery(TypedProfileRunnableFactory<TOut> factory, ISchemaProvider schemaProvider)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
        _runState = new TypedRunState(factory.ParameterDefinitions);
    }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => _runState.AddPhaseChanged(value);
        remove => _runState.RemovePhaseChanged(value);
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => _runState.AddDataSourceProgress(value);
        remove => _runState.RemoveDataSourceProgress(value);
    }

    public event QueryProgressEventHandler QueryProgress
    {
        add => _runState.AddQueryProgress(value);
        remove => _runState.RemoveQueryProgress(value);
    }

    public TypedParameterValues TypedParameters => _runState.TypedParameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _runState.ParameterDefinitions;

    public IReadOnlyList<ScriptParameterContract> ParameterContracts => _runState.ParameterContracts;

    public IReadOnlyList<ScriptParameterDefinition> RequiredParameters => _runState.RequiredParameters;

    public TypedQueryDiagnostics Diagnostics => _factory.Diagnostics;

    public TypedQueryProfileResult<TOut> RunWithProfile()
    {
        return RunWithProfile(CancellationToken.None);
    }

    public TypedQueryProfileResult<TOut> RunWithProfile(CancellationToken token)
    {
        return RunWithProfile(_runState.CreateOptions(token));
    }

    public TypedQueryProfileResult<TOut> RunWithProfile(TypedQueryRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var compiledQuery = _factory.CreateCompiledQuery(_schemaProvider);
        if (options.TypedParameters.Count != 0)
            compiledQuery.TypedParameters.ReplaceWith(options.TypedParameters);
        if (options.PhaseChanged != null)
            compiledQuery.PhaseChanged += options.PhaseChanged;
        if (options.DataSourceProgress != null)
            compiledQuery.DataSourceProgress += options.DataSourceProgress;
        if (options.QueryProgress != null)
            compiledQuery.QueryProgress += options.QueryProgress;

        try
        {
            var profileResult = options.QueryProgressOptions is { } progressOptions
                ? compiledQuery.RunWithProfile(progressOptions, options.CancellationToken)
                : compiledQuery.RunWithProfile(options.CancellationToken);
            return CreateTypedProfileResult(profileResult);
        }
        finally
        {
            if (options.PhaseChanged != null)
                compiledQuery.PhaseChanged -= options.PhaseChanged;
            if (options.DataSourceProgress != null)
                compiledQuery.DataSourceProgress -= options.DataSourceProgress;
            if (options.QueryProgress != null)
                compiledQuery.QueryProgress -= options.QueryProgress;
        }
    }

    private TypedQueryProfileResult<TOut> CreateTypedProfileResult(QueryProfileResult profileResult)
    {
        return new TypedQueryProfileResult<TOut>(
            ProjectRows(profileResult.Result, _factory.Projector),
            profileResult.Profile,
            profileResult.ProfileText,
            isSourceExecutionComplete: true);
    }

    private static IEnumerable<TOut> ProjectRows(Table table, TableTypedRowProjector<TOut> projector)
    {
        foreach (var row in table)
            yield return projector.Project(row);
    }

}
