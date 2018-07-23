using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Schema;

namespace Musoq.Converter
{
    public static class InstanceCreator
    {
        public static (byte[] DllFile, byte[] PdbFile) CompileForStore(string script, ISchemaProvider provider)
        {
            var items = new BuildItems
            {
                SchemaProvider = provider,
                RawQuery = script
            };

            var chain = new CreateTree(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null)));

            chain.Build(items);

            return (items.DllFile, items.PdbFile);
        }

        public static CompiledQuery CompileForExecution(string script, ISchemaProvider schemaProvider)
        {
            var items = new BuildItems
            {
                SchemaProvider = schemaProvider,
                RawQuery = script
            };

            var compiled = true;

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
            var tempFileName = $"InMemoryAssembly";
            var assemblyPath = Path.Combine(tempPath, $"{tempFileName}.dll");
            var pdbPath = Path.Combine(tempPath, $"{tempFileName}.pdb");
            var csPath = Path.Combine(tempPath, $"{tempFileName}.cs");

            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                items.Compilation?.SyntaxTrees.ElementAt(0).GetRoot().WriteTo(writer);
            }

            using (var file = new StreamWriter(File.OpenWrite(csPath)))
            {
                file.Write(builder.ToString());
            }

            if (items.DllFile != null && items.DllFile.Length > 0)
            {
                using (var file = new BinaryWriter(File.OpenWrite(assemblyPath)))
                {
                    if (items.DllFile != null)
                        file.Write(items.DllFile);
                }
            }

            if (items.PdbFile != null && items.PdbFile.Length > 0)
            {
                using (var file = new BinaryWriter(File.OpenWrite(pdbPath)))
                {
                    if (items.PdbFile != null)
                        file.Write(items.PdbFile);
                }
            }

            if (!compiled && compilationError != null)
                throw compilationError;

            var runnable = new RunnableDebugDecorator(CreateRunnable(items), csPath, assemblyPath, pdbPath);

            return new CompiledQuery(runnable);
        }

        private static IRunnable CreateRunnable(BuildItems items, string assemblyPath)
        {
            return CreateRunnable(items, () => {
               return Assembly.LoadFrom(assemblyPath);
            });
        }

        private static IRunnable CreateRunnable(BuildItems items)
        {
            return CreateRunnable(items, () => Assembly.Load(items.DllFile, items.PdbFile));
        }

        private static IRunnable CreateRunnable(BuildItems items, Func<Assembly> createAssembly)
        {
            var assembly = createAssembly();

            var type = assembly.GetType(items.AccessToClassPath);

            var runnable = (IRunnable)Activator.CreateInstance(type);
            runnable.Provider = items.SchemaProvider;

            return runnable;
        }
    }
}