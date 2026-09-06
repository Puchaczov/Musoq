using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Musoq.Targets.CSharpClr;

internal sealed class CSharpRenderedQueryInspector : IRenderedQueryInspector, ICancellableRenderedQueryInspector
{
    public ExecutionTargetId TargetId => ExecutionTargetIds.CSharpClr;

    public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact)
    {
        return Inspect(artifact, CancellationToken.None);
    }

    public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (artifact is not CSharpRenderedQueryArtifact csharp)
            throw new InvalidOperationException(
                $"Generated C# inspection requires a C# rendered artifact, but got '{artifact.TargetId}'.");

        var generatedCode = FormatGeneratedCode(csharp, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return new RenderedQueryInspection(
            TargetId,
            generatedCode,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["language"] = "csharp",
                ["runnableType"] = csharp.AccessToClassPath
            });
    }

    private static string FormatGeneratedCode(CSharpRenderedQueryArtifact csharp, CancellationToken cancellationToken)
    {
        var syntaxTrees = csharp.Compilation.SyntaxTrees.ToArray();

        if (syntaxTrees.Length == 0)
            return string.Empty;

        if (syntaxTrees.Length == 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var formatted = FormatSyntaxTree(syntaxTrees[0]);
            cancellationToken.ThrowIfCancellationRequested();
            return formatted;
        }

        var builder = new StringBuilder();

        for (var index = 0; index < syntaxTrees.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index > 0)
                builder.AppendLine();

            builder.AppendLine(CultureInfo.InvariantCulture, $"// === SYNTAX TREE {index} ===");
            builder.AppendLine(FormatSyntaxTree(syntaxTrees[index]));
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatSyntaxTree(SyntaxTree syntaxTree)
    {
        return syntaxTree.GetRoot().ToFullString();
    }
}
