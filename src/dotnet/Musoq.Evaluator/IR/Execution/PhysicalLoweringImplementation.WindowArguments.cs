using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionExpression ConvertWindowInputExpression(
        IrExpression expression,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields)
    {
        return ResolveWindowAggregateSourceRead(expression, sourceLookup, aggregateSourceFields) ??
               ExecutionExpressionConverter.Convert(expression, sourceLookup);
    }

    private static PluginWindowArgumentsBuildResult CreatePluginWindowArguments(
        WindowRegistration registration,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields)
    {
        ExecutionExpression value = new ExecutionLiteral(1, typeof(int));
        if (registration.ValueArguments.Length > 0)
            value = ConvertWindowInputExpression(registration.ValueArguments[0], sourceLookup, aggregateSourceFields);
        var arguments = new List<ExecutionExpression>(Math.Max(0, registration.ValueArguments.Length - 1));
        var rowScopedArguments = new List<bool>(Math.Max(0, registration.ValueArguments.Length - 1));
        for (var index = 1; index < registration.ValueArguments.Length; index++)
        {
            var argument = ConvertWindowInputExpression(registration.ValueArguments[index], sourceLookup, aggregateSourceFields);
            arguments.Add(argument);
            rowScopedArguments.Add(ContainsRowScopedRead(argument));
        }

        if (registration.Function?.GetCustomAttribute<WindowFunctionAttribute>()?.ContextType is not null)
        {
            var parameters = registration.Function.GetParameters();
            for (var index = arguments.Count + 1; index < parameters.Length; index++)
            {
                if (!parameters[index].HasDefaultValue)
                    throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
                        $"Window '{registration.FunctionName}' is missing required argument '{parameters[index].Name}'.",
                        Musoq.Parser.TextSpan.Empty);
                arguments.Add(new ExecutionLiteral(parameters[index].DefaultValue, parameters[index].ParameterType));
                rowScopedArguments.Add(false);
            }
        }

        return PluginWindowArgumentsBuildResult.Success(value, arguments, rowScopedArguments, []);
    }

    private static ExecutionWindowFrame? CreatePluginWindowFrame(WindowRegistration registration)
    {
        if (registration.Function?.GetCustomAttribute<WindowFunctionAttribute>()?.IgnoresFrame == true)
            return null;

        return CreateAggregateWindowFrame(registration);
    }

    private static ExecutionWindowFrame? CreateWindowFrame(WindowFrameNode? frame)
    {
        return frame == null
            ? null
            : new ExecutionWindowFrame(
                CreateWindowFrameKind(frame.FrameType),
                CreateWindowFrameBound(frame.Start),
                CreateWindowFrameBound(frame.End));
    }

    private static ExecutionWindowFrameKind CreateWindowFrameKind(WindowFrameType frameType)
    {
        return frameType switch
        {
            WindowFrameType.Rows => ExecutionWindowFrameKind.Rows,
            WindowFrameType.Range => ExecutionWindowFrameKind.Range,
            _ => throw new ArgumentOutOfRangeException(nameof(frameType), frameType, null)
        };
    }

    private static ExecutionWindowFrameBound CreateWindowFrameBound(WindowFrameBoundNode bound)
    {
        return new ExecutionWindowFrameBound(CreateWindowFrameBoundKind(bound.BoundType), bound.Offset);
    }

    private static ExecutionWindowFrameBoundKind CreateWindowFrameBoundKind(WindowFrameBoundType boundType)
    {
        return boundType switch
        {
            WindowFrameBoundType.UnboundedPreceding => ExecutionWindowFrameBoundKind.UnboundedPreceding,
            WindowFrameBoundType.UnboundedFollowing => ExecutionWindowFrameBoundKind.UnboundedFollowing,
            WindowFrameBoundType.CurrentRow => ExecutionWindowFrameBoundKind.CurrentRow,
            WindowFrameBoundType.OffsetPreceding => ExecutionWindowFrameBoundKind.OffsetPreceding,
            WindowFrameBoundType.OffsetFollowing => ExecutionWindowFrameBoundKind.OffsetFollowing,
            _ => throw new ArgumentOutOfRangeException(nameof(boundType), boundType, null)
        };
    }

    private static OffsetWindowArgumentsBuildResult CreateOffsetWindowArguments(
        WindowRegistration registration,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string> aggregateSourceFields)
    {
        var value = ConvertWindowInputExpression(registration.ValueArguments[0], sourceLookup, aggregateSourceFields);
        var offset = registration.ValueArguments.Length > 1
            ? ConvertWindowInputExpression(registration.ValueArguments[1], sourceLookup, aggregateSourceFields)
            : new ExecutionLiteral(1, typeof(int));

        if (offset.ReturnType.ResolveClrType() != typeof(int))
        {
            return OffsetWindowArgumentsBuildResult.Unsupported(
                $"Execution IR offset window lowering requires an int offset. Found {offset.ReturnType.ResolveClrType().Name}.");
        }

        var defaultValue = registration.ValueArguments.Length > 2
            ? ConvertWindowInputExpression(registration.ValueArguments[2], sourceLookup, aggregateSourceFields)
            : new ExecutionLiteral(null, typeof(object));

        return OffsetWindowArgumentsBuildResult.Success(value, offset, defaultValue);
    }

}
