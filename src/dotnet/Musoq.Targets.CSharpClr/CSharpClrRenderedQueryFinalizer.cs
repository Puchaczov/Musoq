using System.IO;
using Microsoft.CodeAnalysis.Emit;

namespace Musoq.Targets.CSharpClr;

internal sealed class CSharpClrRenderedQueryFinalizer : IRenderedQueryFinalizer
{
    public ExecutionTargetId TargetId => ExecutionTargetIds.CSharpClr;

    public TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options)
    {
        if (artifact is not CSharpRenderedQueryArtifact csharp)
            throw new InvalidOperationException(
                $"C# CLR finalizer expected artifact type '{nameof(CSharpRenderedQueryArtifact)}' for target '{ExecutionTargetIds.CSharpClr}', but received artifact type '{artifact.GetType().Name}' for target '{artifact.TargetId}'.");

        if (options is not CSharpClrFinalizationOptions csharpOptions)
            throw new NotSupportedException(
                $"C# CLR finalizer expected options type '{nameof(CSharpClrFinalizationOptions)}', but received '{options.GetType().Name}'.");

        var emitPdb = csharpOptions.EmitPdb;
        var dllStream = new MemoryStream();
        var pdbStream = emitPdb ? new MemoryStream() : null;

        var result = emitPdb
            ? csharp.Compilation.Emit(dllStream, pdbStream,
                options: new EmitOptions(false, DebugInformationFormat.PortablePdb))
            : csharp.Compilation.Emit(dllStream);

        if (!result.Success)
        {
            pdbStream?.Dispose();
            dllStream.Dispose();
            return new CSharpClrFinalizationResult(result, null);
        }

        if (csharpOptions.Purpose == TargetFinalizationPurpose.Execution)
        {
            return new CSharpClrFinalizationResult(
                result,
                new ClrAssemblyExecutableArtifact(dllStream, pdbStream, csharp.AccessToClassPath));
        }

        try
        {
            var dllFile = ToByteArray(dllStream);
            var pdbFile = pdbStream is null ? null : ToByteArray(pdbStream);
            return new CSharpClrFinalizationResult(
                result,
                new ClrAssemblyExecutableArtifact(dllFile, pdbFile, csharp.AccessToClassPath));
        }
        finally
        {
            pdbStream?.Dispose();
            dllStream.Dispose();
        }
    }

    private static byte[] ToByteArray(MemoryStream stream)
    {
        if (stream.TryGetBuffer(out var buffer) &&
            buffer.Offset == 0 &&
            buffer.Count == buffer.Array!.Length)
        {
            return buffer.Array;
        }

        return stream.ToArray();
    }
}
