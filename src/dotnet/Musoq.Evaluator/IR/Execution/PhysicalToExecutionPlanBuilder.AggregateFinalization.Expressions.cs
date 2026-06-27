using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<ExecutionExpression> ConvertAggregateFinalProjectionExpression(
        IrExpression expression,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(expression, context);
        if (groupKeyRead != null)
            return BuildResult<ExecutionExpression>.Success(groupKeyRead);

        var rewrittenExpression = AggregateRefRewriter.Rewrite(expression, context.BindingsByIdentifier);
        return ConvertAggregateFinalExpression(rewrittenExpression, context);
    }

    private static AggregateFinalizationContext CreateAggregateFinalizationContext(
        ExecutionVariable group,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateCapturedValue> capturedValues,
        IReadOnlyDictionary<string, AggregateAccumulatorField> typedAccumulators,
        AggregateGroupShape groupShape,
        string aggregateKind)
    {
        return new AggregateFinalizationContext(
            group,
            groupKeys,
            bindings,
            CreateAggregateBindingsMap(bindings),
            capturedValues,
            typedAccumulators,
            groupShape,
            aggregateKind);
    }

    private BuildResult<ExecutionBlock> CreateAggregateFinalBlock(
        IrExpression? havingPredicate,
        AggregateFinalizationContext context,
        ExecutionAppendRow appendRow)
    {
        var appendBlock = CreateAppendBlock(appendRow);
        if (havingPredicate == null)
            return BuildResult<ExecutionBlock>.Success(appendBlock);

        var rewrittenPredicate = AggregateRefRewriter.Rewrite(havingPredicate, context.BindingsByIdentifier);
        var condition = ConvertAggregateFinalExpression(rewrittenPredicate, context);

        if (!condition.Supported)
            return BuildResult<ExecutionBlock>.Unsupported(condition.UnsupportedReason);

        if (condition.Value.ReturnType != typeof(bool))
        {
            return BuildResult<ExecutionBlock>.Unsupported(
                $"Execution IR {context.AggregateKind} HAVING lowering requires a boolean predicate. Found {condition.Value.ReturnType.Name}.");
        }

        return BuildResult<ExecutionBlock>.Success(new ExecutionBlock([new ExecutionIf(condition.Value, appendBlock)]));
    }

    private static BuildResult<ExecutionExpression> ConvertAggregateFinalExpression(
        IrExpression expression,
        AggregateFinalizationContext context)
    {
        switch (expression)
        {
            case Literal literal:
                return BuildResult<ExecutionExpression>.Success(new ExecutionLiteral(literal.Value, literal.ReturnType));
            case WildcardLiteral:
                return BuildResult<ExecutionExpression>.Success(new ExecutionLiteral("*", typeof(string)));
            case ScriptParameterRef parameter:
                return BuildResult<ExecutionExpression>.Success(new ExecutionScriptParameterRead(
                    parameter.Name,
                    parameter.ReturnType));
            case ScriptVariableRef variable:
                return BuildResult<ExecutionExpression>.Success(new ExecutionScriptVariableRead(
                    variable.Name,
                    variable.ReturnType));
            case BinaryOp binary:
                return ConvertAggregateFinalBinaryExpression(binary, context);
            case UnaryOp unary:
                return ConvertAggregateFinalUnaryExpression(unary, context);
            case AggregateRef aggregateRef:
                return CreateAggregateFinalCall(aggregateRef.Identifier, context);
            case ColumnRef columnRef:
                return ConvertAggregateFinalColumnRef(columnRef, context);
            case MethodCall methodCall:
                return ConvertAggregateFinalMethodCall(methodCall, context);
            case StrictCast strictCast:
                return ConvertAggregateFinalStrictCast(strictCast, context);
            default:
                return BuildResult<ExecutionExpression>.Unsupported(
                    $"Execution IR {context.AggregateKind} final expression lowering cannot convert expression {IrExpressionPrinter.Print(expression)}.");
        }
    }

    private static BuildResult<ExecutionExpression> ConvertAggregateFinalBinaryExpression(
        BinaryOp binary,
        AggregateFinalizationContext context)
    {
        var left = ConvertAggregateFinalExpression(binary.Left, context);
        if (!left.Supported)
            return left;

        var right = ConvertAggregateFinalExpression(binary.Right, context);
        if (!right.Supported)
            return right;

        return BuildResult<ExecutionExpression>.Success(new ExecutionBinary(
            binary.Kind,
            left.Value,
            right.Value,
            binary.ReturnType));
    }

    private static BuildResult<ExecutionExpression> ConvertAggregateFinalUnaryExpression(
        UnaryOp unary,
        AggregateFinalizationContext context)
    {
        var operand = ConvertAggregateFinalExpression(unary.Operand, context);
        if (!operand.Supported)
            return operand;

        return BuildResult<ExecutionExpression>.Success(new ExecutionUnary(unary.Kind, operand.Value, unary.ReturnType));
    }

    private static BuildResult<ExecutionExpression> ConvertAggregateFinalStrictCast(
        StrictCast strictCast,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(strictCast, context);
        if (groupKeyRead != null)
            return BuildResult<ExecutionExpression>.Success(groupKeyRead);

        var expression = ConvertAggregateFinalExpression(strictCast.Expression, context);
        if (!expression.Supported)
            return expression;

        return BuildResult<ExecutionExpression>.Success(new ExecutionStrictCast(
            expression.Value,
            strictCast.TargetTypeName,
            strictCast.ReturnType));
    }

    private static BuildResult<ExecutionExpression> ConvertAggregateFinalColumnRef(
        ColumnRef columnRef,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(columnRef, context);
        if (groupKeyRead != null)
            return BuildResult<ExecutionExpression>.Success(groupKeyRead);

        var identifier = string.IsNullOrWhiteSpace(columnRef.Alias)
            ? columnRef.ColumnName
            : $"{columnRef.Alias}.{columnRef.ColumnName}";
        if (TryResolveAggregateBinding(identifier, context.BindingsByIdentifier, out var binding))
            return CreateAggregateFinalCall(binding, context);

        if (TryCreateAggregateCapturedValueRead(identifier, columnRef.ReturnType, context, out var capturedValueRead))
            return BuildResult<ExecutionExpression>.Success(capturedValueRead);

        return BuildResult<ExecutionExpression>.Unsupported(
            $"Execution IR {context.AggregateKind} final expression lowering cannot bind aggregate reference or captured aggregate value '{identifier}'.");
    }

    private static ExecutionGroupKeyRead? TryCreateGroupKeyRead(
        IrExpression expression,
        AggregateFinalizationContext context)
    {
        var groupKeyIndex = TryGetGroupKeyExpressionIndex(expression, context.GroupKeys);
        if (groupKeyIndex == null)
            return null;

        return new ExecutionGroupKeyRead(
            context.Group,
            context.GroupKeys.Names[groupKeyIndex.Value],
            context.GroupKeys.Types[groupKeyIndex.Value],
            context.GroupShape.Keys[groupKeyIndex.Value]);
    }

    private static BuildResult<ExecutionExpression> ConvertAggregateFinalMethodCall(
        MethodCall methodCall,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(methodCall, context);
        if (groupKeyRead != null)
            return BuildResult<ExecutionExpression>.Success(groupKeyRead);

        if (TryResolveProjectedAggregate(methodCall, context.Bindings, context.BindingsByIdentifier, out var binding))
            return CreateAggregateFinalCall(binding, context);

        if (RequiresSourceInjection(methodCall.Method))
        {
            return BuildResult<ExecutionExpression>.Unsupported(
                $"Execution IR {context.AggregateKind} final expression lowering cannot render source-scoped method call {IrExpressionPrinter.Print(methodCall)} after aggregate finalization.");
        }

        var arguments = new List<ExecutionExpression>(methodCall.Arguments.Count);
        foreach (var argument in methodCall.Arguments)
        {
            var convertedArgument = ConvertAggregateFinalExpression(argument, context);
            if (!convertedArgument.Supported)
                return convertedArgument;

            arguments.Add(convertedArgument.Value);
        }

        return BuildResult<ExecutionExpression>.Success(new ExecutionMethodCall(
            methodCall.Method,
            arguments,
            methodCall.Alias,
            methodCall.ReturnType));
    }

    private static bool TryCreateAggregateCapturedValueRead(
        string identifier,
        Type returnType,
        AggregateFinalizationContext context,
        out ExecutionAggregateCapturedValueRead capturedValueRead)
    {
        var normalizedIdentifier = AggregateRefRewriter.NormalizeIdentifier(identifier);
        if (context.CapturedValues.TryGetValue(identifier, out var capturedValue) ||
            (!string.IsNullOrWhiteSpace(normalizedIdentifier) &&
             context.CapturedValues.TryGetValue(normalizedIdentifier, out capturedValue)))
        {
            var capturedField = TryResolveAggregateCapturedField(context.GroupShape, capturedValue.ValueName);
            if (capturedField == null)
            {
                capturedValueRead = null!;
                return false;
            }

            capturedValueRead = new ExecutionAggregateCapturedValueRead(
                context.Group,
                capturedValue.ValueName,
                returnType,
                capturedField);
            return true;
        }

        capturedValueRead = null!;
        return false;
    }

    private static bool RequiresSourceInjection(MethodInfo method)
    {
        return method.GetParameters()
            .Any(static parameter => parameter.GetCustomAttributes(true)
                .OfType<InjectTypeAttribute>()
                .Any(static attribute => attribute.GetType().Name is nameof(InjectSpecificSourceAttribute) or "InjectSourceAttribute"));
    }
}
