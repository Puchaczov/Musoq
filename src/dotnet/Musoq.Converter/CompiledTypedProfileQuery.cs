using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Converter;

public sealed class CompiledTypedProfileQuery<TOut> : IProfiledTypedRunnable<TOut>, ITypedQueryDiagnosticsProvider
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

    public IDictionary<string, object?> Parameters => _runState.Parameters;

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
        ApplyParameters(compiledQuery, options.Parameters);
        if (options.PhaseChanged != null)
            compiledQuery.PhaseChanged += options.PhaseChanged;
        if (options.DataSourceProgress != null)
            compiledQuery.DataSourceProgress += options.DataSourceProgress;

        try
        {
            var profileResult = compiledQuery.RunWithProfile(options.CancellationToken);
            return CreateTypedProfileResult(profileResult);
        }
        finally
        {
            if (options.PhaseChanged != null)
                compiledQuery.PhaseChanged -= options.PhaseChanged;
            if (options.DataSourceProgress != null)
                compiledQuery.DataSourceProgress -= options.DataSourceProgress;
        }
    }

    private static TypedQueryProfileResult<TOut> CreateTypedProfileResult(QueryProfileResult profileResult)
    {
        var projector = TableTypedRowProjector<TOut>.Create(profileResult.Result);
        return new TypedQueryProfileResult<TOut>(
            ProjectRows(profileResult.Result, projector),
            profileResult.Profile,
            profileResult.ProfileText,
            isSourceExecutionComplete: true);
    }

    private static IEnumerable<TOut> ProjectRows(Table table, TableTypedRowProjector<TOut> projector)
    {
        foreach (var row in table)
            yield return projector.Project(row);
    }

    private static void ApplyParameters(CompiledQuery compiledQuery, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parameters != null)
        {
            foreach (var parameter in parameters)
                compiledQuery.Parameters[parameter.Key] = parameter.Value;
        }
    }

}
