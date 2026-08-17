using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator;

public class RunnableDebugDecorator(
    ITableRunnable runnable,
    AssemblyLoadContext assemblyLoadContext,
    params string[] filesToDelete)
    : ITableRunnable
{
    public ISchemaProvider Provider
    {
        get => runnable.Provider;
        set => runnable.Provider = value;
    }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId
    {
        get => runnable.SourceRuntimeSettingsBySourceContextId;
        set => runnable.SourceRuntimeSettingsBySourceContextId = value;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId
    {
        get => runnable.SourceRuntimeSettingDescriptionsBySourceContextId;
        set => runnable.SourceRuntimeSettingDescriptionsBySourceContextId = value;
    }

    public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans
    {
        get => runnable.SourceExecutionPlans;
        set => runnable.SourceExecutionPlans = value;
    }

    public ILogger Logger
    {
        get => runnable.Logger;
        set => runnable.Logger = value;
    }

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => runnable.PhaseChanged += value;
        remove => runnable.PhaseChanged -= value;
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => runnable.DataSourceProgress += value;
        remove => runnable.DataSourceProgress -= value;
    }

    public Table Run(CancellationToken token)
    {
        var table = runnable.Run(token);

        assemblyLoadContext.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foreach (var path in filesToDelete)
        {
            var file = new FileInfo(path);

            try
            {
                if (file.Exists)
                    file.Delete();
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine("File is in use. Cannot delete it.");
            }
        }

        return table;
    }
}
