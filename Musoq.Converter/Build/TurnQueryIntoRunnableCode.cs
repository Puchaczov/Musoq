using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Converter.Exceptions;

namespace Musoq.Converter.Build;

public class TurnQueryIntoRunnableCode(BuildChain successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        using (var dllStream = new MemoryStream())
        {
            using (var pdbStream = new MemoryStream())
            {
                var result = items.Compilation.Emit(
                    dllStream, 
                    pdbStream, 
                    options: new EmitOptions(false, DebugInformationFormat.PortablePdb));

                items.PdbFile = pdbStream.ToArray();
                items.EmitResult = result;
                
                if (!result.Success)
                {
                    var all = new StringBuilder();

                    foreach (var diagnostic in result.Diagnostics)
                        all.Append(diagnostic);

                    items.DllFile = null;
                    items.PdbFile = null;

                    // Dump source code
                    foreach (var tree in items.Compilation.SyntaxTrees)
                    {
                        // File.WriteAllText("d:\\repos\\Musoq-2\\generated_code_error.cs", tree.ToString());
                    }

                    throw new CompilationException(all.ToString());
                }
            }

            items.DllFile = dllStream.ToArray();
        }

        Successor?.Build(items);
    }
}