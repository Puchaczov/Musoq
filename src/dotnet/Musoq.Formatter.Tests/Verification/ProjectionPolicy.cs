using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Musoq.Formatter.Tests.Verification;

internal static class ProjectionPolicy
{
    internal static IEnumerable<ISymbol> Members(INamedTypeSymbol type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current != null; current = current.BaseType)
            foreach (var member in current.GetMembers())
                if (Readable(member) && seen.Add(member.Name))
                    yield return member;
    }

    internal static ITypeSymbol ValueType(ISymbol member) => member switch
    {
        IPropertySymbol property => property.Type,
        IFieldSymbol field => field.Type,
        _ => throw new InvalidOperationException("Only readable properties and fields have projection policies.")
    };

    internal static string? Exclusion(ISymbol member)
    {
        var type = ValueType(member);
        if (type.ToDisplayString() is "Musoq.Parser.TextSpan" or "Musoq.Parser.TextSpan?")
            return "Source range; independently checked by the source oracle.";
        if (member is not IPropertySymbol property)
            return null;
        while (property.OverriddenProperty != null)
            property = property.OverriddenProperty;
        var key = VerificationCompiler.Key(property.ContainingType) + "." + property.Name;
        return key switch
        {
            "Musoq.Parser.Nodes.Node.Id" => "Computed execution-node identity; semantic members are projected.",
            "Musoq.Parser.Nodes.Node.HasSpan" => "Source-range presence.",
            "Musoq.Parser.Nodes.From.AliasedFromNode.InSourcePosition" => "Source position.",
            "Musoq.Parser.Nodes.From.SchemaFromNode.QueryId" => "Generated query identity.",
            "Musoq.Parser.Nodes.DiagnosticCommandNode.CommandStart" or
            "Musoq.Parser.Nodes.DiagnosticCommandNode.InnerStart" or
            "Musoq.Parser.Nodes.DiagnosticCommandNode.InnerEnd" => "Diagnostic source positions.",
            "Musoq.Parser.Nodes.SkipNode.Value" or "Musoq.Parser.Nodes.TakeNode.Value" =>
                "Computed paging value; its Expression is projected.",
            _ => null
        };
    }

    private static bool Readable(ISymbol member) => member.DeclaredAccessibility == Accessibility.Public && !member.IsStatic &&
        member is IPropertySymbol { GetMethod.DeclaredAccessibility: Accessibility.Public } or IFieldSymbol;
}
