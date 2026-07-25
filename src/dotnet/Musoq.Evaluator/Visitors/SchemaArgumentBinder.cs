using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SchemaArgumentBinder
{
    public static object?[] BindStaticArguments(
        ArgsListNode args,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? scriptParameters = null,
        IReadOnlyDictionary<string, ScriptVariableDefinition>? scriptVariables = null,
        BoundSchemaInvocation? invocation = null)
    {
        if (args == null || args.Args.Length == 0)
        {
            if (invocation == null)
                return [];

            return invocation.Arguments
                .Where(static argument => argument.UsesDefault)
                .Select(argument => argument.DefaultValue)
                .ToArray();
        }

        var values = new List<object?>(invocation?.Arguments.Length ?? args.Args.Length);

        if (invocation == null)
        {
            foreach (var arg in args.Args)
            {
                if (TryBindStaticArgument(arg, scriptParameters, scriptVariables, out var value))
                    values.Add(value);
            }

            return values.ToArray();
        }

        foreach (var boundArgument in invocation.Arguments)
        {
            if (boundArgument.UsesDefault)
            {
                values.Add(boundArgument.DefaultValue);
                continue;
            }

            var arg = args.Args[boundArgument.SourceArgumentIndex!.Value];
            if (!TryBindStaticArgument(arg, scriptParameters, scriptVariables, out var value))
            {
                // Metadata/planning APIs only accept a positional object array. Once a
                // required slot is dynamic, appending a later static slot would make it
                // look like the dynamic slot and silently reorder the invocation. Keep
                // the materialized values as the known canonical prefix instead.
                break;
            }

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
