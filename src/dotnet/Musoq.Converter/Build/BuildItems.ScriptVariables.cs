using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    public IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions
    {
        get => GetListOrEmpty<ScriptVariableDefinition>(BuildItemKeys.ScriptVariableDefinitions);
        set => SetRequired(BuildItemKeys.ScriptVariableDefinitions, value);
    }
}