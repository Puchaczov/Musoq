using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SchemaArgumentBinder
{
    public static object?[] BindStaticArguments(
        ArgsListNode args,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? scriptParameters = null,
        IReadOnlyDictionary<string, ScriptVariableDefinition>? scriptVariables = null)
    {
        if (args == null || args.Args.Length == 0)
            return [];

        var values = new List<object?>(args.Args.Length);

        foreach (var arg in args.Args)
        {
            if (TryBindStaticArgument(arg, scriptParameters, scriptVariables, out var value))
                values.Add(value);
        }

        return values.ToArray();
    }

    private static bool TryBindStaticArgument(
        Node arg,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? scriptParameters,
        IReadOnlyDictionary<string, ScriptVariableDefinition>? scriptVariables,
        [MaybeNullWhen(false)] out object? value)
    {
        switch (arg)
        {
            case StringNode stringNode:
                value = stringNode.Value;
                return true;
            case WordNode wordNode:
                value = wordNode.Value;
                return true;
            case DecimalNode decimalNode:
                value = decimalNode.Value;
                return true;
            case IntegerNode integerNode:
                value = integerNode.ObjValue;
                return true;
            case HexIntegerNode hexIntegerNode:
                value = hexIntegerNode.ObjValue;
                return true;
            case BinaryIntegerNode binaryIntegerNode:
                value = binaryIntegerNode.ObjValue;
                return true;
            case OctalIntegerNode octalIntegerNode:
                value = octalIntegerNode.ObjValue;
                return true;
            case BooleanNode booleanNode:
                value = booleanNode.Value;
                return true;
            case ParameterReferenceNode parameterReferenceNode
                when scriptParameters != null &&
                     scriptParameters.TryGetValue(parameterReferenceNode.Name, out var definition) &&
                     definition.HasDefaultValue:
                value = definition.DefaultValue;
                return true;
            case ScriptVariableReferenceNode variableReferenceNode
                when scriptVariables != null &&
                     scriptVariables.TryGetValue(variableReferenceNode.Name, out var definition):
                value = definition.Value;
                return true;
            default:
                return TryBindStaticExpression(arg, scriptVariables, out value);
        }
    }

    private static bool TryBindStaticExpression(
        Node arg,
        IReadOnlyDictionary<string, ScriptVariableDefinition>? scriptVariables,
        [MaybeNullWhen(false)] out object? value)
    {
        if (scriptVariables == null)
        {
            value = null;
            return false;
        }

        var result = ScriptVariableInitializerEvaluator.EvaluateStaticExpression(arg, scriptVariables);
        value = result.Value;
        return result.Success;
    }
}
