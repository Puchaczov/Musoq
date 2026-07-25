using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Target-neutral symbolic naming used by lowering and optimization. A target
/// may apply its own language escaping when rendering these stable symbols.
/// </summary>
internal static class ExecutionSymbolicNamePolicy
{
    internal const int MaxIdentifierLength = 256;

    private static readonly FrozenSet<string> ReservedMemberNames = CreateReservedMemberNames(
        "__contexts",
        "__values");

    private static readonly FrozenSet<string> ReservedIdentifiers = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach",
        "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
        "ushort", "using", "virtual", "void", "volatile", "while"
    }.ToFrozenSet(StringComparer.Ordinal);

    internal static string CreateGeneratedFieldName(
        string outputName,
        int outputIndex,
        ISet<string> usedFieldNames)
    {
        var candidate = CreateLoweringIdentifierCandidate(outputName, outputIndex);
        if (ReservedMemberNames.Contains(candidate))
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

    internal static bool IsReservedIdentifier(string identifier) =>
        ReservedIdentifiers.Contains(identifier);

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
