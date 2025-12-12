using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator;

public class RunnableDebugDecorator(
    IRunnable runnable,
    AssemblyLoadContext assemblyLoadContext,
    params string[] filesToDelete)
    : IRunnable
{
    public ISchemaProvider Provider
    {
        get => runnable.Provider;
        set => runnable.Provider = value;
    }

    public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables
    {
        get => runnable.PositionalEnvironmentVariables;
        set => runnable.PositionalEnvironmentVariables = value;
    }

    public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation
    {
        get => runnable.QueriesInformation;
        set => runnable.QueriesInformation = value;
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