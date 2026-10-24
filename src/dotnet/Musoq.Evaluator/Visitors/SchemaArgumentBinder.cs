using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Parser.Nodes;
using System.Globalization;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Visitors;

internal static class SchemaArgumentBinder
{
    public static object?[] BindStaticArguments(
        ArgsListNode args,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? scriptParameters = null,
        IReadOnlyDictionary<string, ScriptVariableDefinition>? scriptVariables = null,
        BoundSchemaInvocation? invocation = null,
        bool useConstantFolding = true)
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

        var evaluation = new StaticArgumentEvaluation(
            SourceArgumentStaticValues.Create(scriptParameters, scriptVariables), useConstantFolding);
        var values = new List<object?>(invocation?.Arguments.Length ?? args.Args.Length);

        if (invocation == null)
        {
            foreach (var arg in args.Args)
            {
                if (!TryBindStaticArgument(arg, evaluation, out var value))
                    break;

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
            if (!TryBindStaticArgument(arg, evaluation, out var value))
            {
                // Metadata/planning APIs only accept a positional object array. Once a
                // required slot is dynamic, appending a later static slot would make it
                // look like the dynamic slot and silently reorder the invocation. Keep
                // the materialized values as the known canonical prefix instead.
                break;
            }

            values.Add(ConvertStaticArgument(value, invocation.Signature.Parameters[boundArgument.ParameterIndex].ParameterType));
        }

        return values.ToArray();
    }

    private static object? ConvertStaticArgument(object? value, Type parameterType)
    {
        var targetType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        if (value is null || targetType.IsInstanceOfType(value))
            return value;
        if (value is IConvertible && SchemaConversionClassifier.TryGetCost(value.GetType(), parameterType, out _))
            return Convert.ChangeType(value is char character ? (ushort)character : value, targetType, CultureInfo.InvariantCulture);
        return value;
    }

    private static bool TryBindStaticArgument(
        Node arg,
        StaticArgumentEvaluation evaluation,
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
                when evaluation.Values.TryGetValue(parameterReferenceNode.Name, out var definition):
                value = definition.Value;
                return true;
            case ScriptVariableReferenceNode variableReferenceNode
                when evaluation.Values.TryGetValue(variableReferenceNode.Name, out var definition):
                value = definition.Value;
                return true;
            default:
                return TryBindStaticExpression(arg, evaluation, out value);
        }
    }

    private static bool TryBindStaticExpression(
        Node arg,
        StaticArgumentEvaluation evaluation,
        [MaybeNullWhen(false)] out object? value)
    {
        var result = ScriptVariableInitializerEvaluator.EvaluateStaticExpression(arg, evaluation.Values);
        value = result.Value;
        if (!result.Success || arg is RecordLiteralNode or ArrayLiteralNode)
            return result.Success;

        var expression = new ExpressionConverter().Convert(arg);
        if (evaluation.UseConstantFolding)
            expression = new LogicalConstantExpressionFolder(null).Visit(expression);
        if (new LogicalConstantExpressionFolder(null, evaluation.Values).Visit(expression) is not Literal literal)
            return false;
        value = literal.Value;
        return true;
    }

    private readonly record struct StaticArgumentEvaluation(
        IReadOnlyDictionary<string, ScriptVariableDefinition> Values,
        bool UseConstantFolding);
}
