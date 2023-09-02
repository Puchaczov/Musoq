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
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Converter
{
    public static class InstanceCreator
    {
        public static BuildItems CreateForAnalyze(string script, string assemblyName, ISchemaProvider provider)
        {
            var items = new BuildItems
            {
                SchemaProvider = provider,
                RawQuery = script,
                AssemblyName = assemblyName,
                PositionalEnvironmentVariables = new Dictionary<uint, IReadOnlyDictionary<string, string>>()
            };

            RuntimeLibraries.CreateReferences();

            var chain = new CreateTree(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null)));

            chain.Build(items);

            return items;
        }
        
        public static (byte[] DllFile, byte[] PdbFile) CompileForStore(string script, string assemblyName, ISchemaProvider provider)
        {
            var items = CreateForAnalyze(script, assemblyName, provider);

            return (items.DllFile, items.PdbFile);
        }

        public static Task<(byte[] DllFile, byte[] PdbFile)> CompileForStoreAsync(string script, string assemblyName, ISchemaProvider provider)
        {
            return Task.Factory.StartNew(() => CompileForStore(script, assemblyName, provider));
        }

        public static CompiledQuery CompileForExecution(string script, string assemblyName, ISchemaProvider schemaProvider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables)
        {
            var items = new BuildItems
            {
                SchemaProvider = schemaProvider,
                RawQuery = script,
                AssemblyName = assemblyName,
                PositionalEnvironmentVariables = positionalEnvironmentVariables
            };

            var compiled = true;

            RuntimeLibraries.CreateReferences();

            BuildChain chain = 
                new CreateTree(
                    new TransformTree(
                        new TurnQueryIntoRunnableCode(null)));

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

            if (compiled && !Debugger.IsAttached) return new CompiledQuery(CreateRunnable(items));

            var tempPath = Path.Combine(Path.GetTempPath(), "Musoq");
            var tempFileName = Guid.NewGuid().ToString();
            var assemblyPath = Path.Combine(tempPath, $"{tempFileName}.dll");
            var pdbPath = Path.Combine(tempPath, $"{tempFileName}.pdb");
            var csPath = Path.Combine(tempPath, $"{tempFileName}.cs");

            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                items.Compilation?.SyntaxTrees.ElementAt(0).GetRoot().WriteTo(writer);
            }

            using (var file = new StreamWriter(File.Open(csPath, FileMode.Create)))
            {
                file.Write(builder.ToString());
            }

            if (items.DllFile is {Length: > 0})
            {
                using var file = new BinaryWriter(File.Open(assemblyPath, FileMode.Create));
                if (items.DllFile != null)
                    file.Write(items.DllFile);
            }

            if (items.PdbFile is {Length: > 0})
            {
                using var file = new BinaryWriter(File.Open(pdbPath, FileMode.Create));
                if (items.PdbFile != null)
                    file.Write(items.PdbFile);
            }

            if (!compiled && compilationError != null)
                throw compilationError;

            var assemblyLoadContext = new DebugAssemblyLoadContext();
            var runnable = new RunnableDebugDecorator(
                CreateRunnableForDebug(items, () => assemblyLoadContext.LoadFromAssemblyPath(assemblyPath)),
                assemblyLoadContext,
                csPath, 
                assemblyPath, 
                pdbPath);

            return new CompiledQuery(runnable);
        }

        public static Task<CompiledQuery> CompileForExecutionAsync(string script, string assemblyName, ISchemaProvider schemaProvider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables)
        {
            return Task.Factory.StartNew(() => CompileForExecution(script, assemblyName, schemaProvider, positionalEnvironmentVariables));
        }

        private static IRunnable CreateRunnableForDebug(BuildItems items, Func<Assembly> loadAssembly)
        {
            return CreateRunnable(items, loadAssembly);
        }

        private static IRunnable CreateRunnable(BuildItems items)
        {
            return CreateRunnable(items, () => Assembly.Load(items.DllFile, items.PdbFile));
        }

        private static IRunnable CreateRunnable(BuildItems items, Func<Assembly> createAssembly)
        {
            var assembly = createAssembly();

            var type = assembly.GetType(items.AccessToClassPath);
            
            if  (type is null)
                throw new InvalidOperationException($"Type {items.AccessToClassPath} was not found in assembly {assembly.FullName}.");

            var runnable = (IRunnable)Activator.CreateInstance(type);
            
            if (runnable is null)
                throw new InvalidOperationException($"Could not create instance of type {type.FullName}.");
            
            runnable.Provider = items.SchemaProvider;
            runnable.PositionalEnvironmentVariables = items.PositionalEnvironmentVariables;
            
            var usedColumns = items.UsedColumns;
            var usedWhereNodes = items.UsedWhereNodes;

            if (usedColumns.Count != usedWhereNodes.Count)
            {
                throw new InvalidOperationException("Used columns and used where nodes are not equal. This must not happen.");
            }
            
            runnable.QueriesInformation =
                usedColumns.Join(
                    usedWhereNodes, 
                    f => f.Key.Id, 
                    f => f.Key.Id,
                    (f, s) => (SchemaFromNode: f.Key, UsedColumns: (IReadOnlyCollection<ISchemaColumn>)f.Value, UsedValues:s.Value)
                ).ToDictionary(f => f.SchemaFromNode.Id, f => ((SchemaFromNode)f.SchemaFromNode, f.UsedColumns, f.UsedValues));

            return runnable;
        }
        
        private class DebugAssemblyLoadContext : AssemblyLoadContext
        {
            public DebugAssemblyLoadContext() : base(true)
            {
            }
        }
    }
}