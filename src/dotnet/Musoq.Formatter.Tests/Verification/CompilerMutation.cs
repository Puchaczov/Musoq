using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Musoq.Formatter.Tests.Verification;

internal static class CompilerMutation
{
    internal static CSharpCompilation Replace(CSharpCompilation compilation, string file, string original, string replacement)
    {
        var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath.EndsWith(file, StringComparison.Ordinal));
        var text = tree.GetText().ToString();
        if (!text.Contains(original, StringComparison.Ordinal))
            throw new InvalidOperationException("Mutation anchor is absent: " + original);
        return compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(text.Replace(original, replacement, StringComparison.Ordinal),
            (CSharpParseOptions)tree.Options, tree.FilePath));
    }

    internal static CSharpCompilation Fixture(string source) => CSharpCompilation.Create("AuditMutation",
        new[] { CSharpSyntaxTree.ParseText(source) }, VerificationCompiler.Current.Tests.References,
        new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
}
