using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal static class GeneratedRowNamingPolicy
{
    internal const int MaxIdentifierLength = 256;

    private static readonly FrozenSet<string> LoweringReservedMemberNames = CreateReservedMemberNames(
        "__contexts",
        "__values");

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

    internal static string CreateGeneratedFieldName(
        string outputName,
        int outputIndex,
        ISet<string> usedFieldNames)
    {
        var candidate = CreateLoweringIdentifierCandidate(outputName, outputIndex);
        if (SyntaxFacts.GetKeywordKind(candidate) != SyntaxKind.None ||
            LoweringReservedMemberNames.Contains(candidate))
            candidate += "_";

        candidate = TrimIdentifier(candidate, 0);
        var fieldName = candidate;
        var suffix = 1;
        while (!usedFieldNames.Add(fieldName))
        {
            var suffixText = $"_{suffix.ToString(CultureInfo.InvariantCulture)}";
            fieldName = $"{TrimIdentifier(candidate, suffixText.Length)}{suffixText}";
            suffix++;
        }

        return fieldName;
    }

    internal static string CreateValuesRowTypeName(string alias, uint shapeHash)
    {
        var hash = shapeHash.ToString("X8", CultureInfo.InvariantCulture);
        return TrimIdentifier(
            CreateLoweringIdentifierCandidate($"{alias}Values{hash}Row0", 0),
            0);
    }

    internal static string GetGeneratedFieldName(FieldBinding field)
    {
        return field.AccessStrategy is GeneratedFieldAccess generated
            ? generated.FieldName
            : field.Name;
    }

    internal static string TrimIdentifier(string identifier, int reservedSuffixLength)
    {
        var maxLength = MaxIdentifierLength - reservedSuffixLength;
        if (identifier.Length <= maxLength)
            return identifier;

        return identifier[..maxLength];
    }

    internal static string CreateLoweringIdentifierCandidate(string outputName, int outputIndex)
    {
        if (string.IsNullOrWhiteSpace(outputName))
            return $"Field{outputIndex.ToString(CultureInfo.InvariantCulture)}";

        var characters = outputName
            .Select(character => char.IsLetterOrDigit(character) || character == '_' ? character : '_')
            .ToArray();
        var candidate = new string(characters);

        if (char.IsLetter(candidate[0]) || candidate[0] == '_')
            return candidate;

        return $"_{candidate}";
    }

    internal static bool CanRenderIdentifier(string identifier)
    {
        return identifier.Length <= MaxIdentifierLength &&
               SyntaxFacts.IsValidIdentifier(identifier);
    }

    internal static string CreateRendererIdentifierCandidate(string value, int disambiguator)
    {
        var builder = new StringBuilder(value.Length + 8);

        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

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
        var names = new HashSet<string>(StringComparer.Ordinal)
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
