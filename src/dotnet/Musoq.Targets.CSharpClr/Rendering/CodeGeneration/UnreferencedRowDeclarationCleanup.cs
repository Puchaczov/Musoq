using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class UnreferencedRowDeclarationCleanup
{
    public static CompilationUnitSyntax Apply(CompilationUnitSyntax unit)
    {
        var candidates = unit.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(static type => type.Modifiers.Any(SyntaxKind.PrivateKeyword) &&
                type.BaseList?.Types.Any(static parent => parent.Type is IdentifierNameSyntax { Identifier.ValueText: "Row" }) == true)
            .GroupBy(static type => type.Identifier.ValueText, StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);
        if (candidates.Count == 0)
            return unit;

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in unit.DescendantTokens())
        {
            foreach (var candidate in candidates)
            {
                if (token.ValueText.Contains(candidate.Key, StringComparison.Ordinal) &&
                    token.Parent?.FirstAncestorOrSelf<ClassDeclarationSyntax>() != candidate.Value)
                    used.Add(candidate.Key);
            }
        }
        var unused = candidates.Where(pair => !used.Contains(pair.Key)).Select(static pair => pair.Value).ToArray();
        return unused.Length == 0 ? unit : unit.RemoveNodes(unused, SyntaxRemoveOptions.KeepNoTrivia)!;
    }
}
