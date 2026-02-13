using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.Runtime;
using Musoq.Schema;
using Musoq.Schema.Api;
using SchemaFromNode = Musoq.Evaluator.Parser.SchemaFromNode;

namespace Musoq.Converter;

public static class InstanceCreator
{
    public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider,
        ILoggerResolver loggerResolver)
    {
        var items = new BuildItems
        {
            SchemaProvider = provider,
            RawQuery = script,
            AssemblyName = assemblyName,
            CreateBuildMetadataAndInferTypesVisitor = null
        };

        RuntimeLibraries.CreateReferences();

        var chain = new CreateTree(
            new CompileInterpretationSchemas(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null), loggerResolver)));

        chain.Build(items);

        return items;
    }

    public static (byte[] DllFile, byte[] PdbFile) CompileForStore(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver)
    {
        var items = CreateForAnalyze(script, assemblyName, provider, loggerResolver);

        return (items.DllFile, items.PdbFile);
    }

    public static Task<(byte[] DllFile, byte[] PdbFile)> CompileForStoreAsync(string script, string assemblyName,
        ISchemaProvider provider, ILoggerResolver loggerResolver)
    {
        return Task.Factory.StartNew(() => CompileForStore(script, assemblyName, provider, loggerResolver));
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver)
    {
        return CompileForExecution(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            () => new CreateTree(
                new CompileInterpretationSchemas(
                    new TransformTree(
                        new TurnQueryIntoRunnableCode(null), loggerResolver))),
            _ => { });
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, CompilationOptions compilationOptions)
    {
        return CompileForExecution(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            () => new CreateTree(
                new CompileInterpretationSchemas(
                    new TransformTree(
                        new TurnQueryIntoRunnableCode(null), loggerResolver))),
            buildItems => { buildItems.CompilationOptions = compilationOptions; });
    }

    public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver, Func<BuildChain> createChain, Action<BuildItems> modifyBuildItems)
    {
        var items = new BuildItems
        {
            SchemaProvider = schemaProvider,
            RawQuery = script,
            AssemblyName = assemblyName,
            CreateBuildMetadataAndInferTypesVisitor = null
        };

        modifyBuildItems(items);

        var compiled = true;

        RuntimeLibraries.CreateReferences();

        var chain =
            createChain?.Invoke() ??
            new CreateTree(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null), loggerResolver)
            );

        CompilationException compilationError = null;
        try
        {
            chain.Build(items);
        }
        catch (CompilationException ce)
        {
            compilationError = ce;
            compiled = false;
        }


        IRunnable runnable;
        if (compiled && !Debugger.IsAttached)
        {
            runnable = CreateRunnable(items);
            runnable.Logger = loggerResolver.ResolveLogger();

            return new CompiledQuery(runnable);
        }

        var tempPath = Path.Combine(Path.GetTempPath(), "Musoq");
        var tempFileName = Guid.NewGuid().ToString();
        var assemblyPath = Path.Combine(tempPath, $"{tempFileName}.dll");
        var pdbPath = Path.Combine(tempPath, $"{tempFileName}.pdb");
        var csPath = Path.Combine(tempPath, $"{tempFileName}.cs");

        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);


        var builder = new StringBuilder();
        if (items.Compilation?.SyntaxTrees != null)
            for (var i = 0; i < items.Compilation.SyntaxTrees.Count(); i++)
            {
                builder.AppendLine($"// === SYNTAX TREE {i} ===");
                using var writer = new StringWriter();
                items.Compilation.SyntaxTrees.ElementAt(i).GetRoot().WriteTo(writer);
                builder.AppendLine(writer.ToString());
                builder.AppendLine();
            }

        using (var file = new StreamWriter(File.Open(csPath, FileMode.Create)))
        {
            file.Write(builder.ToString());
        }

        if (items.DllFile is { Length: > 0 })
        {
            using var file = new BinaryWriter(File.Open(assemblyPath, FileMode.Create));
            if (items.DllFile != null)
                file.Write(items.DllFile);
        }

        if (items.PdbFile is { Length: > 0 })
        {
            using var file = new BinaryWriter(File.Open(pdbPath, FileMode.Create));
            if (items.PdbFile != null)
                file.Write(items.PdbFile);
        }

        if (!compiled && compilationError != null)
            throw compilationError;

        var assemblyLoadContext = new DebugAssemblyLoadContext();
        runnable = new RunnableDebugDecorator(
            CreateRunnableForDebug(items, () => assemblyLoadContext.LoadFromAssemblyPath(assemblyPath)),
            assemblyLoadContext,
            csPath,
            assemblyPath,
            pdbPath);

        return new CompiledQuery(runnable);
    }

    public static Task<CompiledQuery> CompileForExecutionAsync(string script, string assemblyName,
        ISchemaProvider schemaProvider, ILoggerResolver loggerResolver,
        IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables)
    {
        return Task.Factory.StartNew(() => CompileForExecution(script, assemblyName, schemaProvider, loggerResolver));
    }

    private static IRunnable CreateRunnableForDebug(BuildItems items, Func<Assembly> loadAssembly)
    {
        return CreateRunnable(items, loadAssembly);
    }

    private static IRunnable CreateRunnable(BuildItems items)
    {
        return CreateRunnable(items, () => items.PdbFile is { Length: > 0 }
            ? Assembly.Load(items.DllFile, items.PdbFile)
            : Assembly.Load(items.DllFile));
    }

    private static IRunnable CreateRunnable(BuildItems items, Func<Assembly> createAssembly)
    {
        var assembly = createAssembly();

        var type = assembly.GetType(items.AccessToClassPath);

        if (type is null)
            throw new InvalidOperationException(
                $"Type {items.AccessToClassPath} was not found in assembly {assembly.FullName}.");

        var runnable = (IRunnable)Activator.CreateInstance(type);

        if (runnable is null)
            throw new InvalidOperationException($"Could not create instance of type {type.FullName}.");

        runnable.Provider = items.SchemaProvider;
        runnable.PositionalEnvironmentVariables = items.PositionalEnvironmentVariables;

        var usedColumns = items.UsedColumns;
        var usedWhereNodes = items.UsedWhereNodes;
        var queryHintsPerSchema = items.QueryHintsPerSchema;

        if (usedColumns.Count != usedWhereNodes.Count)
            throw new InvalidOperationException(
                "Used columns and used where nodes are not equal. This must not happen.");

        runnable.QueriesInformation =
            usedColumns.Join(
                usedWhereNodes,
                f => f.Key.Id,
                f => f.Key.Id,
                (f, s) => new
                {
                    SchemaFromNode = f.Key,
                    UsedColumns = (IReadOnlyCollection<ISchemaColumn>)f.Value,
                    WhereNode = s.Value,
                    HasExternallyProvidedTypes = f.Key is SchemaFromNode { HasExternallyProvidedTypes: true },
                    QueryHints = queryHintsPerSchema.TryGetValue(f.Key, out var hints)
                        ? hints
                        : QueryHints.Empty
                }
            ).ToDictionary(
                f => f.SchemaFromNode.Id,
                f => new QuerySourceInfo(
                    f.SchemaFromNode,
                    f.UsedColumns,
                    f.WhereNode,
                    f.HasExternallyProvidedTypes,
                    f.QueryHints));

        return runnable;
    }

    private class DebugAssemblyLoadContext : AssemblyLoadContext
    {
        public DebugAssemblyLoadContext() : base(true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
