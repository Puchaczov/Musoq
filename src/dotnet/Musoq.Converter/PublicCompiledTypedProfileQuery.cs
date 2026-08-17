using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter;

internal sealed class PublicCompiledTypedProfileQuery<TOut> : ICompiledTypedProfileQuery<TOut>, ITypedQueryDiagnosticsProvider
{
    private readonly TypedProfileRunnableFactory<TOut> _factory;
    private readonly InMemorySourceBinding _sourceBinding;
    private readonly TypedRunState _runState;

    public PublicCompiledTypedProfileQuery(
        string query,
        InMemorySourceBinding sourceBinding,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions)
    {
        _sourceBinding = sourceBinding ?? throw new ArgumentNullException(nameof(sourceBinding));

        _factory = InstanceCreator.CompileForTypedProfileFactory<TOut>(
            query,
            $"MusoqTypedProfile_{Guid.NewGuid():N}",
            _sourceBinding.CreateMetadataProvider(),
            loggerResolver ?? throw new ArgumentNullException(nameof(loggerResolver)),
            compilationOptions ?? throw new ArgumentNullException(nameof(compilationOptions)),
            _sourceBinding.AdditionalReferenceTypes);
        _runState = new TypedRunState(_factory.ParameterDefinitions);
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

    public TypedQueryProfileResult<TOut> RunWithProfile(CancellationToken token, params MusoqSourceRows[] sources)
    {
        return RunWithProfile(_runState.CreateOptions(token), sources);
    }

    public TypedQueryProfileResult<TOut> RunWithProfile(TypedQueryRunOptions options, params MusoqSourceRows[] sources)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sources);

        var provider = _sourceBinding.CreateRuntimeProvider(sources);
        var compiled = _factory.Create(provider);
        return compiled.RunWithProfile(options);
    }
}
