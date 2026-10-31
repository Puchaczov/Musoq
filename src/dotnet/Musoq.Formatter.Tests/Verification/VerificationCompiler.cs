using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Musoq.Formatter.Tests.Verification;

internal sealed class VerificationCompiler
{
    private static readonly Lazy<VerificationCompiler> Model = new(() => new VerificationCompiler());
    internal static VerificationCompiler Current => Model.Value;

    private VerificationCompiler()
    {
        Tests = CompilerInputs.Read("Tests").Compile("FormatterVerificationTests");
        Formatter = CompilerInputs.Read("Formatter").Compile("FormatterVerificationLibrary");
        Parser = CompilerInputs.Read("Parser").Compile("FormatterVerificationParser");
        RequireValid(Tests);
        RequireValid(Formatter);
        RequireValid(Parser);
    }

    internal CSharpCompilation Tests { get; }
    internal CSharpCompilation Formatter { get; }
    internal CSharpCompilation Parser { get; }

    internal string[] ParserDeclarations() => ParserDeclarations(Parser);

    internal static string[] ParserDeclarations(CSharpCompilation parser) => Types(parser.Assembly.GlobalNamespace)
        .Where(type => type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract && !type.IsStatic && IsPublic(type) &&
            type.ContainingNamespace.ToDisplayString().StartsWith("Musoq.Parser.Nodes", StringComparison.Ordinal))
        .Select(Key).Order(StringComparer.Ordinal).ToArray();

    internal string[] References(string project)
    {
        var input = CompilerInputs.Read(project);
        var image = MetadataReference.CreateFromFile(input.Target);
        var compilation = CSharpCompilation.Create("References", references: new[] { image });
        var assembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(image)!;
        return assembly.Modules.SelectMany(module => module.ReferencedAssemblies).Select(identity => identity.Name)
            .Where(name => name.StartsWith("Musoq.", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    internal string[] SyntaxKinds() => Parser.GetTypeByMetadataName("Musoq.Parser.Syntax.SyntaxKind")!.GetMembers()
        .OfType<IFieldSymbol>().Where(field => field.HasConstantValue).Select(field => field.Name).ToArray();

    internal int SyntaxKindValue(string name)
    {
        var field = Parser.GetTypeByMetadataName("Musoq.Parser.Syntax.SyntaxKind")!.GetMembers(name).OfType<IFieldSymbol>().Single();
        return (int)field.ConstantValue!;
    }

    internal static string Key(INamedTypeSymbol type)
    {
        if (type.ContainingType != null)
            return Key(type.ContainingType) + "+" + type.MetadataName;
        return type.ContainingNamespace.ToDisplayString() + "." + type.MetadataName;
    }

    private static bool IsPublic(INamedTypeSymbol type) => type.DeclaredAccessibility == Accessibility.Public &&
        (type.ContainingType == null || IsPublic(type.ContainingType));

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol parent)
    {
        foreach (var member in parent.GetMembers())
        {
            if (member is INamedTypeSymbol type)
                yield return type;
            if (member is INamespaceOrTypeSymbol nested)
                foreach (var child in Types(nested))
                    yield return child;
        }
    }

    private static void RequireValid(CSharpCompilation compilation)
    {
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
            throw new InvalidOperationException("Evaluated verification compilation is invalid: " + string.Join("\n", errors.Select(error => error.ToString())));
    }
}
