using System;
using System.IO;
using System.Reflection;
using System.Text;
using Musoq.Evaluator;

namespace Musoq.Converter.Build
{
    public class TurnQueryIntoRunnableCode : BuildChain {
        public TurnQueryIntoRunnableCode(BuildChain successor) 
            : base(successor)
        {
        }

        public override void Build(BuildItems items)
        {
            using (var dllStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    var result = items.Compilation.Emit(dllStream, pdbStream);

                    items.EmitResult = result;
                    if (!result.Success)
                    {
                        var all = new StringBuilder();

                        foreach (var diagnostic in result.Diagnostics)
                            all.Append(diagnostic);

                        throw new NotSupportedException(all.ToString());
                    }

                    var dllBytesArray = dllStream.ToArray();
                    var pdbBytesArray = pdbStream.ToArray();

                    var assembly = Assembly.Load(dllBytesArray, pdbBytesArray);

                    var type = assembly.GetType(items.AccessToClassPath);

                    var runnable = (IRunnable)Activator.CreateInstance(type);
                    runnable.Provider = items.SchemaProvider;

                    items.CompiledQuery = runnable;
                    items.DllFile = dllBytesArray;
                    items.PdbFile = pdbBytesArray;
                }
            }

            Successor?.Build(items);
        }
    }
}