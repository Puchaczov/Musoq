using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Converter.Exceptions;

namespace Musoq.Converter.Build;

public class TurnQueryIntoRunnableCode(BuildChain? successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var artifacts = Compile(items.RenderingArtifacts, items.EmitPdb);
        items.CompilationArtifacts = artifacts;

        if (!artifacts.EmitResult.Success)
            throw new CompilationException(CreateCompilationErrorText(artifacts.EmitResult));

        Successor?.Build(items);
    }

    private static CompilationBuildArtifacts Compile(RenderingBuildArtifacts rendering, bool emitPdb)
    {
        using var dllStream = new MemoryStream();
        using var pdbStream = emitPdb ? new MemoryStream() : null;

        var result = emitPdb
            ? rendering.Compilation.Emit(dllStream, pdbStream,
                options: new EmitOptions(false, DebugInformationFormat.PortablePdb))
            : rendering.Compilation.Emit(dllStream);

        if (!result.Success)
            return new CompilationBuildArtifacts(result, null, null);

        byte[]? pdbFile;
        if (emitPdb)
        {
            if (!pdbStream!.TryGetBuffer(out var pdbBuffer))
                pdbBuffer = new ArraySegment<byte>(pdbStream.ToArray());

            pdbFile = pdbBuffer.Count == pdbBuffer.Array!.Length ? pdbBuffer.Array : pdbBuffer.ToArray();
        }
        else
        {
            pdbFile = null;
        }

        if (!dllStream.TryGetBuffer(out var dllBuffer))
            dllBuffer = new ArraySegment<byte>(dllStream.ToArray());

        var dllFile = dllBuffer.Count == dllBuffer.Array!.Length ? dllBuffer.Array : dllBuffer.ToArray();

        return new CompilationBuildArtifacts(result, dllFile, pdbFile);
    }

    private static string CreateCompilationErrorText(EmitResult result)
    {
        var all = new StringBuilder();

        foreach (var diagnostic in result.Diagnostics)
        {
            all.AppendLine(diagnostic.ToString());
            AppendDiagnosticSourceSnippet(all, diagnostic);
        }

        return all.ToString();
    }

    private static void AppendDiagnosticSourceSnippet(StringBuilder builder, Microsoft.CodeAnalysis.Diagnostic diagnostic)
    {
        if (!diagnostic.Location.IsInSource)
            return;

        var sourceTree = diagnostic.Location.SourceTree;
        if (sourceTree is null)
            return;

        var sourceText = sourceTree.GetText();
        var lineSpan = diagnostic.Location.GetLineSpan();
        var lineIndex = lineSpan.StartLinePosition.Line;

        if (lineIndex < 0 || lineIndex >= sourceText.Lines.Count)
            return;

        var start = Math.Max(0, lineIndex - 1);
        var end = Math.Min(sourceText.Lines.Count - 1, lineIndex + 1);

        builder.AppendLine("-- source snippet --");

        for (var index = start; index <= end; index++)
        {
            var textLine = sourceText.Lines[index].ToString().TrimEnd();
            builder.Append(index + 1);
            builder.Append(": ");
            builder.AppendLine(textLine);
        }

        var caretColumn = Math.Max(0, lineSpan.StartLinePosition.Character);
        builder.Append("   ");
        builder.Append(' ', Math.Min(caretColumn, 120));
        builder.AppendLine("^");
        builder.AppendLine("-- end snippet --");
    }
}
