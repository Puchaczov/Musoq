using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionExpression ConvertWindowInputExpression(
        IrExpression expression,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return ResolveWindowAggregateSourceRead(expression, sourceLookup) ??
               ExecutionExpressionConverter.Convert(expression, sourceLookup);
    }

    private static PluginWindowArgumentsBuildResult CreatePluginWindowArguments(
        WindowRegistration registration,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var value = ConvertWindowInputExpression(registration.ValueArguments[0], sourceLookup);
        if (value is ExecutionRawExpression)
        {
            return PluginWindowArgumentsBuildResult.Unsupported(
                $"Execution IR value window lowering cannot convert value argument {IrExpressionPrinter.Print(registration.ValueArguments[0])}.");
        }

        var arguments = new List<ExecutionExpression>(Math.Max(0, registration.ValueArguments.Length - 1));
        var rowScopedArguments = new List<bool>(Math.Max(0, registration.ValueArguments.Length - 1));
        for (var index = 1; index < registration.ValueArguments.Length; index++)
        {
            var argument = ConvertWindowInputExpression(registration.ValueArguments[index], sourceLookup);
            if (argument is ExecutionRawExpression)
            {
                return PluginWindowArgumentsBuildResult.Unsupported(
                    $"Execution IR value window lowering cannot convert argument {IrExpressionPrinter.Print(registration.ValueArguments[index])}.");
            }

            arguments.Add(argument);
            rowScopedArguments.Add(ContainsRowScopedRead(argument));
        }

        return PluginWindowArgumentsBuildResult.Success(value, arguments, rowScopedArguments, []);
    }

    private static ExecutionWindowFrame? CreatePluginWindowFrame(WindowRegistration registration)
    {
        return IsNtileWindowFunction(registration.FunctionName)
            ? null
            : CreateWindowFrame(registration.Frame);
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
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var value = ConvertWindowInputExpression(registration.ValueArguments[0], sourceLookup);
        if (value is ExecutionRawExpression)
        {
            return OffsetWindowArgumentsBuildResult.Unsupported(
                $"Execution IR offset window lowering cannot convert value argument {IrExpressionPrinter.Print(registration.ValueArguments[0])}.");
        }

        var offset = registration.ValueArguments.Length > 1
            ? ConvertWindowInputExpression(registration.ValueArguments[1], sourceLookup)
            : new ExecutionLiteral(1, typeof(int));

        if (offset is ExecutionRawExpression)
        {
            return OffsetWindowArgumentsBuildResult.Unsupported(
                $"Execution IR offset window lowering cannot convert offset argument {IrExpressionPrinter.Print(registration.ValueArguments[1])}.");
        }

        if (offset.ReturnType != typeof(int))
        {
            return OffsetWindowArgumentsBuildResult.Unsupported(
                $"Execution IR offset window lowering requires an int offset. Found {offset.ReturnType.Name}.");
        }

        var defaultValue = registration.ValueArguments.Length > 2
            ? ConvertWindowInputExpression(registration.ValueArguments[2], sourceLookup)
            : new ExecutionLiteral(null, typeof(object));

        if (defaultValue is ExecutionRawExpression)
        {
            return OffsetWindowArgumentsBuildResult.Unsupported(
                $"Execution IR offset window lowering cannot convert default argument {IrExpressionPrinter.Print(registration.ValueArguments[2])}.");
        }

        return OffsetWindowArgumentsBuildResult.Success(value, offset, defaultValue);
    }

}
