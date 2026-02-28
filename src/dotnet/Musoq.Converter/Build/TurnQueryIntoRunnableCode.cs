using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Converter.Exceptions;

namespace Musoq.Converter.Build;

public class TurnQueryIntoRunnableCode(BuildChain successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        using var dllStream = new MemoryStream();
        using var pdbStream = new MemoryStream();

        var result = items.Compilation.Emit(
            dllStream,
            pdbStream,
            options: new EmitOptions(false, DebugInformationFormat.PortablePdb));

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

        if (!pdbStream.TryGetBuffer(out var pdbBuffer))
            pdbBuffer = new ArraySegment<byte>(pdbStream.ToArray());
        
        if (!dllStream.TryGetBuffer(out var dllBuffer))
            dllBuffer = new ArraySegment<byte>(dllStream.ToArray());

        items.PdbFile = pdbBuffer.Count == pdbBuffer.Array!.Length ? pdbBuffer.Array : pdbBuffer.ToArray();
        items.DllFile = dllBuffer.Count == dllBuffer.Array!.Length ? dllBuffer.Array : dllBuffer.ToArray();

        Successor?.Build(items);
    }
}
