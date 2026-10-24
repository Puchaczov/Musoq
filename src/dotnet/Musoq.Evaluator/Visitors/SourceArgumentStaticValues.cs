using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors;

internal static class SourceArgumentStaticValues
{
    private static readonly IReadOnlyDictionary<string, ScriptVariableDefinition> Empty =
        new Dictionary<string, ScriptVariableDefinition>(StringComparer.Ordinal);

    internal static IReadOnlyDictionary<string, ScriptVariableDefinition> Create(
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        IReadOnlyDictionary<string, ScriptVariableDefinition>? variables)
    {
        variables ??= Empty;
        if (parameters == null || !parameters.Values.Any(static definition => definition.HasDefaultValue))
            return variables;

        var values = new Dictionary<string, ScriptVariableDefinition>(variables, StringComparer.Ordinal);
        foreach (var definition in parameters.Values)
        {
            if (!definition.HasDefaultValue)
                continue;
            values.Add(definition.Name, new ScriptVariableDefinition(
                definition.Name, definition.ParameterType, definition.DefaultValue, false));
        }
        return values;
    }
}
