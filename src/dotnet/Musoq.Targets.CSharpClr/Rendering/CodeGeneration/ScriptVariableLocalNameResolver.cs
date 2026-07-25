using System.Collections.Generic;
using System.Text;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

internal static class ScriptVariableLocalNameResolver
{
    private const string LocalNamePrefix = "let";

    public static IReadOnlyDictionary<string, string> CreateLocalNameMap(
        IReadOnlyList<ScriptVariableDefinition> definitions)
    {
        if (definitions == null || definitions.Count == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in definitions)
        {
            var candidate = CreateBaseLocalName(definition.Name);
            var localName = candidate;
            var suffix = 1;

            while (!used.Add(localName))
                localName = $"{candidate}{suffix++}";

            names.Add(definition.Name, localName);
        }

        return names;
    }

    private static string CreateBaseLocalName(string variableName)
    {
        var builder = new StringBuilder(LocalNamePrefix);
        var capitalizeNext = true;

        foreach (var character in variableName)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = true;
                continue;
            }

            builder.Append(capitalizeNext
                ? char.ToUpperInvariant(character)
                : character);
            capitalizeNext = false;
        }

        return builder.Length == LocalNamePrefix.Length
            ? "letValue"
            : builder.ToString();
    }
}
