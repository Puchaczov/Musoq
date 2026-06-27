using System.Collections.Generic;

namespace Musoq.Evaluator.Visitors.Helpers;

internal static class InterpretationSchemaTypeDependencyExtractor
{
    public static IEnumerable<string> Extract(string typeName, ISet<string> typeParameters)
    {
        var trimmedTypeName = typeName.Trim();
        var openIndex = trimmedTypeName.IndexOf('<', StringComparison.Ordinal);
        if (openIndex < 0 || !trimmedTypeName.EndsWith('>'))
        {
            if (!typeParameters.Contains(trimmedTypeName))
                yield return trimmedTypeName;

            yield break;
        }

        var schemaName = trimmedTypeName[..openIndex].Trim();
        if (!typeParameters.Contains(schemaName))
            yield return schemaName;

        var argumentText = trimmedTypeName[(openIndex + 1)..^1];
        foreach (var typeArgument in SplitTypeArguments(argumentText))
            foreach (var dependency in Extract(typeArgument, typeParameters))
                yield return dependency;
    }

    private static string[] SplitTypeArguments(string argumentText)
    {
        var arguments = new List<string>();
        var depth = 0;
        var start = 0;

        for (var index = 0; index < argumentText.Length; index++)
        {
            var character = argumentText[index];
            if (character == '<')
            {
                depth++;
                continue;
            }

            if (character == '>')
            {
                depth--;
                continue;
            }

            if (character != ',' || depth != 0)
                continue;

            arguments.Add(argumentText[start..index].Trim());
            start = index + 1;
        }

        arguments.Add(argumentText[start..].Trim());
        return arguments.ToArray();
    }
}
