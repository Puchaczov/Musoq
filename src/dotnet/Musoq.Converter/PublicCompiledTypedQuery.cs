using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

internal sealed class PublicCompiledTypedQuery<TOut> : ICompiledTypedQuery<TOut>, IQueryProgressSource
{
    private readonly TypedRunnableFactory<TOut> _factory;
    private readonly InMemorySourceBinding _sourceBinding;
    private readonly TypedRunState _runState;

    public PublicCompiledTypedQuery(
        TypedRunnableFactory<TOut> factory,
        InMemorySourceBinding sourceBinding)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _sourceBinding = sourceBinding ?? throw new ArgumentNullException(nameof(sourceBinding));
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

    public IDictionary<string, object?> Parameters => _runState.Parameters;

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions => _runState.ParameterDefinitions;

    public IReadOnlyList<ScriptParameterContract> ParameterContracts => _runState.ParameterContracts;

    public IReadOnlyList<ScriptParameterDefinition> RequiredParameters => _runState.RequiredParameters;

    public TypedQueryDiagnostics Diagnostics => _factory.Diagnostics;

    public IEnumerable<TOut> Run(CancellationToken token, params MusoqSourceRows[] sources)
    {
        return Run(_runState.CreateOptions(token), sources);
    }

    public IEnumerable<TOut> Run(TypedQueryRunOptions options, params MusoqSourceRows[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(options);

        var provider = _sourceBinding.CreateRuntimeProvider(sources);
        var query = _factory.Create(provider);

        return query.Run(options);
    }
}
