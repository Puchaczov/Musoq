using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    private static IEnumerable<string> EnumerateSourceFamilyFiles(string directory, string searchPattern)
    {
        const string declaredTypePrefix = "declares:";
        if (!searchPattern.StartsWith(declaredTypePrefix, StringComparison.Ordinal))
            return Directory.EnumerateFiles(directory, searchPattern);

        var typeName = searchPattern[declaredTypePrefix.Length..];
        return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file))
                .GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Any(declaration => declaration.Identifier.ValueText == typeName &&
                                    declaration.Modifiers.ToFullString()
                                        .Contains("partial", StringComparison.Ordinal)));
    }
}
