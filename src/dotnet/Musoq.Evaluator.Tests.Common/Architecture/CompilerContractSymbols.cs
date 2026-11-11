using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Musoq.Evaluator.Tests.Architecture;

/// <summary>Reads compiled contracts through Roslyn symbols, including private metadata, without runtime discovery or invocation.</summary>
internal static class CompilerContractSymbols
{
    private static readonly Lazy<CSharpCompilation> Compilation = new(CreateCompilation);

    internal static INamedTypeSymbol For(Type declaredType) => Compilation.Value.GetTypeByMetadataName(declaredType.FullName!)
        ?? throw new InvalidOperationException("Missing declared contract: " + declaredType.FullName);

    private static CSharpCompilation CreateCompilation()
    {
        string[] modules =
        [
            "Musoq.Parser", "Musoq.Schema", "Musoq.Plugins", "Musoq.Evaluator", "Musoq.Converter",
            "Musoq.Targets.Abstractions", "Musoq.Targets.Execution", "Musoq.Targets.Execution.Analysis",
            "Musoq.Targets.CSharpClr"
        ];
        var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?? throw new InvalidOperationException("Runtime metadata inventory is missing.");
        var paths = platformAssemblies.Split(Path.PathSeparator)
            .Concat(modules.Select(name => Path.Combine(AppContext.BaseDirectory, name + ".dll")))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var references = paths.Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create("CompiledArchitectureContracts", references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                metadataImportOptions: MetadataImportOptions.All));
    }

    private static IEnumerable<INamedTypeSymbol> Hierarchy(ITypeSymbol type)
    {
        for (var current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            yield return current;
    }

    private static bool Matches(ISymbol symbol, BindingFlags flags, bool inherited)
    {
        if (inherited && symbol.IsStatic && (flags & BindingFlags.FlattenHierarchy) == 0)
            return false;
        if (inherited && symbol.DeclaredAccessibility == Accessibility.Private)
            return false;
        if (symbol.IsStatic && (flags & BindingFlags.Static) == 0 || !symbol.IsStatic && (flags & BindingFlags.Instance) == 0)
            return false;
        return symbol.DeclaredAccessibility == Accessibility.Public
            ? (flags & BindingFlags.Public) != 0
            : (flags & BindingFlags.NonPublic) != 0;
    }

    private static IEnumerable<T> Members<T>(ITypeSymbol type, BindingFlags flags) where T : ISymbol =>
        Hierarchy(type).Take((flags & BindingFlags.DeclaredOnly) != 0 ? 1 : int.MaxValue)
            .SelectMany((current, index) => current.GetMembers().OfType<T>()
                .Where(member => Matches(member, flags, index != 0)));

    internal static bool IsAssignable(ITypeSymbol target, ITypeSymbol source) =>
        Hierarchy(source).Any(type => SymbolEqualityComparer.Default.Equals(type, target)) ||
        source is INamedTypeSymbol named && named.AllInterfaces.Any(type => SymbolEqualityComparer.Default.Equals(type, target));

    extension(ITypeSymbol type)
    {
        internal string FullName => type.ContainingType is null
            ? type.ContainingNamespace.ToDisplayString() + "." + type.MetadataName
            : type.ContainingType.FullName + "+" + type.MetadataName;
        internal string? Namespace => type.ContainingNamespace?.ToDisplayString();
        internal bool IsNested => type.ContainingType != null;
        internal bool IsPublic => type.ContainingType is null && type.DeclaredAccessibility == Accessibility.Public;
        internal bool IsNestedPublic => type.ContainingType is not null && type.DeclaredAccessibility == Accessibility.Public;
        internal bool IsClass => type.TypeKind == TypeKind.Class;
        internal bool IsInterface => type.TypeKind == TypeKind.Interface;
        internal bool IsArray => type is IArrayTypeSymbol;
        internal bool IsPointer => type is IPointerTypeSymbol;
        internal bool IsGenericType => type is INamedTypeSymbol { IsGenericType: true };
        internal bool IsGenericTypeDefinition => type is INamedTypeSymbol { IsGenericType: true } named && SymbolEqualityComparer.Default.Equals(named, named.OriginalDefinition);
        internal IAssemblySymbol Assembly => type.ContainingAssembly;
        internal ITypeSymbol GetGenericTypeDefinition() => type.OriginalDefinition;
        internal ITypeSymbol? GetElementType() => type switch { IArrayTypeSymbol array => array.ElementType, IPointerTypeSymbol pointer => pointer.PointedAtType, _ => null };
        internal ImmutableArray<ITypeSymbol> GetGenericArguments() => ((INamedTypeSymbol)type).TypeArguments;
        internal bool IsAssignableFrom(ITypeSymbol other) => IsAssignable(type, other);
        internal bool IsAssignableTo(ITypeSymbol other) => IsAssignable(other, type);
        internal IFieldSymbol[] GetFields(BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) => Members<IFieldSymbol>(type, flags).ToArray();
        internal IPropertySymbol[] GetProperties(BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) => Members<IPropertySymbol>(type, flags).ToArray();
        internal IMethodSymbol[] GetMethods(BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) => Members<IMethodSymbol>(type, flags)
            .Where(method => method.MethodKind is not (MethodKind.Constructor or MethodKind.StaticConstructor)).ToArray();
        internal IMethodSymbol[] GetConstructors(BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) => ((INamedTypeSymbol)type).InstanceConstructors.Where(method => Matches(method, flags, false)).ToArray();
        internal INamedTypeSymbol[] GetNestedTypes(BindingFlags flags = BindingFlags.Public) => ((INamedTypeSymbol)type).GetTypeMembers()
            .Where(nested => nested.DeclaredAccessibility == Accessibility.Public ? (flags & BindingFlags.Public) != 0 : (flags & BindingFlags.NonPublic) != 0).ToArray();
        internal IFieldSymbol? GetField(string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) => type.GetFields(flags).SingleOrDefault(field => field.Name == name);
        internal IPropertySymbol? GetProperty(string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) => type.GetProperties(flags).SingleOrDefault(property => property.Name == name);
        internal IMethodSymbol? GetMethod(string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) => type.GetMethods(flags).SingleOrDefault(method => method.Name == name);
        internal ISymbol[] GetMembers(BindingFlags flags) => Members<ISymbol>(type, flags).ToArray();
    }

    extension(IMethodSymbol method)
    {
        internal ImmutableArray<IParameterSymbol> GetParameters() => method.Parameters;
        internal bool IsPublic => method.DeclaredAccessibility == Accessibility.Public;
        internal bool IsSpecialName => method.MethodKind != MethodKind.Ordinary;
        internal INamedTypeSymbol DeclaringType => method.ContainingType;
    }

    extension(IFieldSymbol fieldSymbol)
    {
        internal ITypeSymbol FieldType => fieldSymbol.Type;
        internal bool IsInitOnly => fieldSymbol.IsReadOnly;
        internal bool IsLiteral => fieldSymbol.IsConst;
    }

    extension(IPropertySymbol property)
    {
        internal ITypeSymbol PropertyType => property.Type;
    }

    extension(IParameterSymbol parameter)
    {
        internal ITypeSymbol ParameterType => parameter.Type;
    }

    extension(IAssemblySymbol assembly)
    {
        internal IEnumerable<INamedTypeSymbol> GetTypes() => NamespaceTypes(assembly.GlobalNamespace);
        internal IEnumerable<INamedTypeSymbol> GetExportedTypes() => assembly.GetTypes().Where(IsExported);
        internal INamedTypeSymbol? GetType(string name) => assembly.GetTypeByMetadataName(name);
        internal AssemblyIdentity GetName() => assembly.Identity;
    }

    private static bool IsExported(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;
        return true;
    }

    private static IEnumerable<INamedTypeSymbol> NamespaceTypes(INamespaceSymbol space) =>
        space.GetTypeMembers().SelectMany(NestedTypes).Concat(space.GetNamespaceMembers().SelectMany(NamespaceTypes));

    private static IEnumerable<INamedTypeSymbol> NestedTypes(INamedTypeSymbol type) =>
        new[] { type }.Concat(type.GetTypeMembers().SelectMany(NestedTypes));
}
