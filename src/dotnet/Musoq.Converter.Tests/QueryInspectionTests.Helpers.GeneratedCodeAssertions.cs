using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static void AssertGeneratedCSharpDoesNotUseMemberAccessOnAliases(
        string generatedCSharpCode,
        params string[] aliases)
    {
        var aliasSet = aliases.ToHashSet(StringComparer.Ordinal);
        var root = CSharpSyntaxTree.ParseText(generatedCSharpCode).GetRoot();
        var invalidAccesses = root.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(access => access.Expression is IdentifierNameSyntax identifier &&
                             aliasSet.Contains(identifier.Identifier.ValueText))
            .Select(access => access.ToString())
            .ToArray();

        Assert.IsFalse(
            invalidAccesses.Length > 0,
            $"Generated code contains member access on a positional-row alias: {string.Join(", ", invalidAccesses)}");
    }
}
