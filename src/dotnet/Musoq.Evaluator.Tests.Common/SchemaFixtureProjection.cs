using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Tests;

/// <summary>Compiles explicit property reads; it never inspects generated objects at runtime.</summary>
internal sealed class SchemaFixtureProjection
{
    private const string SchemaNamespace = "Musoq.Generated.Interpreters";
    private readonly Dictionary<ITypeSymbol, int> _types = new(SymbolEqualityComparer.Default);

    internal static string Create(CSharpCompilation compilation, string schemaName, string[] typeArguments)
    {
        var metadataName = SchemaNamespace + "." + schemaName;
        if (typeArguments.Length > 0)
            metadataName += "`" + typeArguments.Length;
        var root = compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException("Missing generated schema: " + metadataName);
        if (typeArguments.Length > 0)
            root = root.Construct(typeArguments.Select(name => compilation.GetTypeByMetadataName(SchemaNamespace + "." + name)
                ?? throw new InvalidOperationException("Missing schema argument: " + name)).ToArray());

        return Discover(compilation, root).Render(root);
    }

    internal static string CreateText(CSharpCompilation compilation, string schemaName)
    {
        var root = compilation.GetTypeByMetadataName(SchemaNamespace + "." + schemaName)
            ?? throw new InvalidOperationException("Missing generated text schema: " + schemaName);
        return Discover(compilation, root).RenderAdapter(root, "TextFixtureAdapter", "ICompiledTextFixture",
            "public object Parse(string text) => Snapshot0(_interpreter.Parse(text));");
    }

    private static SchemaFixtureProjection Discover(CSharpCompilation compilation, INamedTypeSymbol root)
    {
        var projection = new SchemaFixtureProjection();
        projection.Register(root);
        foreach (var declaration in compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()))
        {
            var type = compilation.GetSemanticModel(declaration.SyntaxTree).GetDeclaredSymbol(declaration)!;
            if (type.ContainingNamespace.ToDisplayString() == SchemaNamespace && !type.IsGenericType)
                projection.Register(type);
        }
        return projection;
    }

    private void Register(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            Register(array.ElementType);
            return;
        }
        if (type is not INamedTypeSymbol named || named.ContainingNamespace.ToDisplayString() != SchemaNamespace || _types.ContainsKey(type))
            return;
        _types.Add(type, _types.Count);
        foreach (var property in Properties(named))
            Register(property.Type);
    }

    private static IEnumerable<IPropertySymbol> Properties(INamedTypeSymbol type)
    {
        for (var current = type; current != null && current.ContainingNamespace.ToDisplayString() == SchemaNamespace; current = current.BaseType)
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
                if (!property.IsStatic && !property.IsIndexer && property.DeclaredAccessibility == Accessibility.Public)
                    yield return property;
    }

    private string Value(ITypeSymbol type, string expression)
    {
        if (_types.TryGetValue(type, out var index))
            return $"Snapshot{index}({expression})";
        if (type is IArrayTypeSymbol array && _types.ContainsKey(array.ElementType))
            return $"{expression} == null ? null : global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select({expression}, item => {Value(array.ElementType, "item")}))";
        if (type.SpecialType == SpecialType.System_Object)
            return $"SnapshotObject({expression})";
        return expression;
    }

    private string Render(INamedTypeSymbol root)
    {
        const string entryPoints = """
                    public int BytesConsumed => _interpreter.BytesConsumed;
                    public object Interpret(byte[] data) => Snapshot0(_interpreter.Interpret(data));
                    public (bool Success, object Result) TryInterpret(byte[] data)
                    {
                        var success = _interpreter.TryInterpret(data, out var parsed);
                        return (success, parsed == null ? null : Snapshot0(parsed));
                    }
                    public object PartialInterpret(byte[] data)
                    {
                        var partial = _interpreter.PartialInterpret(data);
                        return new global::System.Collections.Generic.Dictionary<string, object>
                        {
                            ["IsSuccess"] = partial.IsSuccess,
                            ["Result"] = partial.Result == null ? null : Snapshot0(partial.Result),
                            ["ParsedFields"] = partial.ParsedFields == null ? null : Snapshot0(partial.ParsedFields),
                            ["CompletedFields"] = partial.CompletedFields,
                            ["BytesConsumed"] = partial.BytesConsumed,
                            ["BitOffset"] = partial.BitOffset,
                            ["ErrorField"] = partial.ErrorField,
                            ["ErrorMessage"] = partial.ErrorMessage
                        };
                    }
            """;
        return RenderAdapter(root, "BinaryFixtureAdapter", "ICompiledBinaryFixture", entryPoints);
    }

    private string RenderAdapter(INamedTypeSymbol root, string adapterName, string contract, string entryPoints)
    {
        var typeName = root.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var result = new StringBuilder($$"""

            namespace Musoq.Generated.Interpreters
            {
                public sealed class {{adapterName}} : global::Musoq.Evaluator.Tests.{{contract}}
                {
                    private readonly {{typeName}} _interpreter = new();
                    {{entryPoints}}
            """);
        foreach (var pair in _types.OrderBy(pair => pair.Value))
        {
            var name = pair.Key.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            result.AppendLine($"private static object Snapshot{pair.Value}({name} value) => value == null ? null : new global::System.Collections.Generic.Dictionary<string, object> {{");
            foreach (var property in Properties((INamedTypeSymbol)pair.Key))
                result.AppendLine($"[\"{property.Name}\"] = {Value(property.Type, "value.@" + property.Name)},");
            result.AppendLine("};");
        }
        result.AppendLine("private static object SnapshotObject(object value) => value switch {");
        foreach (var pair in _types)
            result.AppendLine($"{pair.Key.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} item => Snapshot{pair.Value}(item),");
        result.AppendLine("_ => value }; } }");
        return result.ToString();
    }
}
