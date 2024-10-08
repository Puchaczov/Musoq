﻿using System.IO;
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
#if DEBUG
                var result = items.Compilation.Emit(
                    dllStream, 
                    pdbStream, 
                    options: new EmitOptions(false, DebugInformationFormat.PortablePdb));
#else
                    var result = items.Compilation.Emit(dllStream, pdbStream);
#endif

                items.EmitResult = result;
                if (!result.Success)
                {
                    var all = new StringBuilder();

                    foreach (var diagnostic in result.Diagnostics)
                        all.Append(diagnostic);

                    items.DllFile = null;
                    items.PdbFile = null;

                    throw new CompilationException(all.ToString());
                }

                items.DllFile = dllStream.ToArray();
                items.PdbFile = pdbStream.ToArray();
            }
        }

        Successor?.Build(items);
    }
}