using System.Collections.Frozen;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

internal static class GeneratedRowNamingPolicy
{
    internal const int MaxIdentifierLength = 256;

    private static readonly FrozenSet<string> RendererReservedMemberNames = CreateReservedMemberNames(
        "__ContextKind",
        "__contextKind",
        "__cachedContexts",
        "__contexts",
        "__values",
        "__leftContext",
        "__rightContext",
        "__leftContexts",
        "__rightContexts",
        "__leftContextsRow",
        "__rightContextsRow");

    internal static string GetGeneratedFieldName(FieldBinding field)
    {
        return field.AccessStrategy is GeneratedFieldAccess generated
            ? generated.FieldName
            : field.Name;
    }

    internal static bool CanRenderIdentifier(string identifier)
    {
        return identifier.Length <= MaxIdentifierLength &&
               SyntaxFacts.IsValidIdentifier(identifier);
    }

    internal static string CreateRendererIdentifierCandidate(string value, int disambiguator)
    {
        var builder = new System.Text.StringBuilder(value.Length + 8);

        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');

        if (builder.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(builder[0]))
            builder.Insert(0, '_');

        if (disambiguator > 0)
            builder.Append(disambiguator.ToString(CultureInfo.InvariantCulture));

        var candidate = builder.Length <= MaxIdentifierLength
            ? builder.ToString()
            : builder.ToString(0, MaxIdentifierLength);

        return SyntaxFacts.IsValidIdentifier(candidate)
            ? candidate
            : $"_{candidate}";
    }

    internal static bool IsRendererReservedMemberName(string memberName)
    {
        return RendererReservedMemberNames.Contains(memberName);
    }

    private static FrozenSet<string> CreateReservedMemberNames(params string[] additionalNames)
    {
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal)
        {
            nameof(Row.Contexts),
            nameof(Row.Values),
            nameof(Row.Count),
            nameof(Row.HasColumn),
            nameof(Row.AssignValue),
            nameof(Row.FitsTheIndex),
            nameof(Row.CheckWithKey),
            nameof(Row.Equals),
            nameof(Row.GetHashCode)
        };

        foreach (var name in additionalNames)
            names.Add(name);

        return names.ToFrozenSet(StringComparer.Ordinal);
    }
}
