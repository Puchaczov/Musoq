using System.Collections.Generic;
using System.Text;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

internal static class ScriptParameterLocalNameResolver
{
    public static IReadOnlyDictionary<string, string> CreateLocalNameMap(
        IReadOnlyList<ScriptParameterDefinition> definitions)
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

    private static string CreateBaseLocalName(string parameterName)
    {
        var builder = new StringBuilder("param");
        var capitalizeNext = true;

        foreach (var character in parameterName)
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

        return builder.Length == "param".Length
            ? "paramValue"
            : builder.ToString();
    }
}
