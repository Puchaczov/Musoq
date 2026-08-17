using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Musoq.Targets.CSharpClr;

internal sealed class CSharpRenderedQueryInspector : IRenderedQueryInspector
{
    public ExecutionTargetId TargetId => ExecutionTargetIds.CSharpClr;

    public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact)
    {
        if (artifact is not CSharpRenderedQueryArtifact csharp)
            throw new InvalidOperationException(
                $"Generated C# inspection requires a C# rendered artifact, but got '{artifact.TargetId}'.");

        return new RenderedQueryInspection(
            TargetId,
            FormatGeneratedCode(csharp),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["language"] = "csharp",
                ["runnableType"] = csharp.AccessToClassPath
            });
    }

    private static string FormatGeneratedCode(CSharpRenderedQueryArtifact csharp)
    {
        var syntaxTrees = csharp.Compilation.SyntaxTrees.ToArray();

        if (syntaxTrees.Length == 0)
            return string.Empty;

        if (syntaxTrees.Length == 1)
            return FormatSyntaxTree(syntaxTrees[0]);

        var builder = new StringBuilder();

        for (var index = 0; index < syntaxTrees.Length; index++)
        {
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
