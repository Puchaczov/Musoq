using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Musoq.Converter.Build;
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
                new TranformTree(
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

            var chain = new CreateTree(
                new TranformTree(
                    new TurnQueryIntoRunnableCode(null)));

            chain.Build(items);

            if (!Debugger.IsAttached) return new CompiledQuery(items.CompiledQuery);

            var tempPath = Path.GetTempPath();
            var tempFileName = $"{Guid.NewGuid().ToString()}";
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

            using (var file = new BinaryWriter(File.OpenWrite(assemblyPath)))
            {
                if(items.DllFile != null)
                    file.Write(items.DllFile);
            }

            using (var file = new BinaryWriter(File.OpenWrite(pdbPath)))
            {
                if (items.PdbFile != null)
                    file.Write(items.PdbFile);
            }

            var runnable = new RunnableDebugDecorator(items.CompiledQuery, csPath, assemblyPath, pdbPath);

            return new CompiledQuery(runnable);
        }
    }
}