using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

internal sealed class TypedRunnableFactoryCore<TRunnable>
    where TRunnable : class, IQueryRunnable
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> _sourceRuntimeSettingDescriptionsBySourceContextId;
    private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
    private readonly ILoggerResolver _loggerResolver;
    private readonly Func<TRunnable> _createRunnable;

    public TypedRunnableFactoryCore(
        Type runnableType,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions,
        TypedQueryDiagnostics diagnostics,
        ILoggerResolver loggerResolver)
    {
        RunnableType = runnableType ?? throw new ArgumentNullException(nameof(runnableType));
        _createRunnable = RunnableActivator.Create<TRunnable>(RunnableType);
        _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId ??
                                                  throw new ArgumentNullException(nameof(sourceRuntimeSettingsBySourceContextId));
        _sourceRuntimeSettingDescriptionsBySourceContextId = sourceRuntimeSettingDescriptionsBySourceContextId ??
                                                             throw new ArgumentNullException(nameof(sourceRuntimeSettingDescriptionsBySourceContextId));
        _sourceExecutionPlans = sourceExecutionPlans ?? throw new ArgumentNullException(nameof(sourceExecutionPlans));
        ParameterDefinitions = parameterDefinitions ?? throw new ArgumentNullException(nameof(parameterDefinitions));
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _loggerResolver = loggerResolver ?? throw new ArgumentNullException(nameof(loggerResolver));
    }

    public Type RunnableType { get; }

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; }

    public TypedQueryDiagnostics Diagnostics { get; }

    public TRunnable CreateRunnable(ISchemaProvider provider)
    {
        var runnable = _createRunnable();

        runnable.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        runnable.SourceRuntimeSettingsBySourceContextId = _sourceRuntimeSettingsBySourceContextId;
        runnable.SourceRuntimeSettingDescriptionsBySourceContextId = _sourceRuntimeSettingDescriptionsBySourceContextId;
        runnable.SourceExecutionPlans = _sourceExecutionPlans;
        runnable.Logger = _loggerResolver.ResolveLogger();

        return runnable;
    }
}
