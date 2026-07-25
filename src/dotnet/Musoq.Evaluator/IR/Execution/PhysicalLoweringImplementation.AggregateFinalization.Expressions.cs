using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<ExecutionExpression> ConvertAggregateFinalProjectionExpression(
        IrExpression expression,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(expression, context);
        if (groupKeyRead != null)
            return LoweringAttempt<ExecutionExpression>.Built(groupKeyRead);

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

    private LoweringAttempt<ExecutionBlock> CreateAggregateFinalBlock(
        IrExpression? havingPredicate,
        AggregateFinalizationContext context,
        ExecutionAppendRow appendRow)
    {
        var appendBlock = CreateAppendBlock(appendRow);
        if (havingPredicate == null)
            return LoweringAttempt<ExecutionBlock>.Built(appendBlock);

        var rewrittenPredicate = AggregateRefRewriter.Rewrite(havingPredicate, context.BindingsByIdentifier);
        var condition = ConvertAggregateFinalExpression(rewrittenPredicate, context);

        if (!condition.IsBuilt)
            return LoweringAttempt<ExecutionBlock>.Unsupported(condition.UnsupportedReason);

        if (condition.Value.ReturnType.ResolveClrType() != typeof(bool))
        {
            return LoweringAttempt<ExecutionBlock>.Unsupported(
                $"Execution IR {context.AggregateKind} HAVING lowering requires a boolean predicate. Found {condition.Value.ReturnType.ResolveClrType().Name}.");
        }

        return LoweringAttempt<ExecutionBlock>.Built(new ExecutionBlock([new ExecutionIf(condition.Value, appendBlock)]));
    }

    private static LoweringAttempt<ExecutionExpression> ConvertAggregateFinalExpression(
        IrExpression expression,
        AggregateFinalizationContext context)
    {
        switch (expression)
        {
            case Literal literal:
                return LoweringAttempt<ExecutionExpression>.Built(new ExecutionLiteral(literal.Value, literal.ReturnType));
            case WildcardLiteral:
                return LoweringAttempt<ExecutionExpression>.Built(new ExecutionLiteral("*", typeof(string)));
            case ScriptParameterRef parameter:
                return LoweringAttempt<ExecutionExpression>.Built(new ExecutionScriptParameterRead(
                    parameter.Name,
                    parameter.ReturnType));
            case ScriptVariableRef variable:
                return LoweringAttempt<ExecutionExpression>.Built(new ExecutionScriptVariableRead(
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
                return LoweringAttempt<ExecutionExpression>.Unsupported(
                    $"Execution IR {context.AggregateKind} final expression lowering cannot convert expression {IrExpressionPrinter.Print(expression)}.");
        }
    }

    private static LoweringAttempt<ExecutionExpression> ConvertAggregateFinalBinaryExpression(
        BinaryOp binary,
        AggregateFinalizationContext context)
    {
        var left = ConvertAggregateFinalExpression(binary.Left, context);
        if (!left.IsBuilt)
            return left;

        var right = ConvertAggregateFinalExpression(binary.Right, context);
        if (!right.IsBuilt)
            return right;

        return LoweringAttempt<ExecutionExpression>.Built(new ExecutionBinary(
            binary.Kind,
            left.Value,
            right.Value,
            binary.ReturnType));
    }

    private static LoweringAttempt<ExecutionExpression> ConvertAggregateFinalUnaryExpression(
        UnaryOp unary,
        AggregateFinalizationContext context)
    {
        var operand = ConvertAggregateFinalExpression(unary.Operand, context);
        if (!operand.IsBuilt)
            return operand;

        return LoweringAttempt<ExecutionExpression>.Built(new ExecutionUnary(unary.Kind, operand.Value, unary.ReturnType));
    }

    private static LoweringAttempt<ExecutionExpression> ConvertAggregateFinalStrictCast(
        StrictCast strictCast,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(strictCast, context);
        if (groupKeyRead != null)
            return LoweringAttempt<ExecutionExpression>.Built(groupKeyRead);

        var expression = ConvertAggregateFinalExpression(strictCast.Expression, context);
        if (!expression.IsBuilt)
            return expression;

        return LoweringAttempt<ExecutionExpression>.Built(new ExecutionStrictCast(
            expression.Value,
            strictCast.TargetTypeName,
            strictCast.ReturnType));
    }

    private static LoweringAttempt<ExecutionExpression> ConvertAggregateFinalColumnRef(
        ColumnRef columnRef,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(columnRef, context);
        if (groupKeyRead != null)
            return LoweringAttempt<ExecutionExpression>.Built(groupKeyRead);

        var identifier = string.IsNullOrWhiteSpace(columnRef.Alias)
            ? columnRef.ColumnName
            : $"{columnRef.Alias}.{columnRef.ColumnName}";
        if (TryResolveAggregateBinding(identifier, context.BindingsByIdentifier, out var binding))
            return CreateAggregateFinalCall(binding, context);

        if (TryCreateAggregateCapturedValueRead(identifier, columnRef.ReturnType, context, out var capturedValueRead))
            return LoweringAttempt<ExecutionExpression>.Built(capturedValueRead);

        return LoweringAttempt<ExecutionExpression>.Unsupported(
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

    private static LoweringAttempt<ExecutionExpression> ConvertAggregateFinalMethodCall(
        MethodCall methodCall,
        AggregateFinalizationContext context)
    {
        var groupKeyRead = TryCreateGroupKeyRead(methodCall, context);
        if (groupKeyRead != null)
            return LoweringAttempt<ExecutionExpression>.Built(groupKeyRead);

        if (TryResolveProjectedAggregate(methodCall, context.Bindings, context.BindingsByIdentifier, out var binding))
            return CreateAggregateFinalCall(binding, context);

        if (RequiresSourceInjection(methodCall.Method))
        {
            return LoweringAttempt<ExecutionExpression>.Unsupported(
                $"Execution IR {context.AggregateKind} final expression lowering cannot render source-scoped method call {IrExpressionPrinter.Print(methodCall)} after aggregate finalization.");
        }

        var arguments = new List<ExecutionExpression>(methodCall.Arguments.Count);
        foreach (var argument in methodCall.Arguments)
        {
            var convertedArgument = ConvertAggregateFinalExpression(argument, context);
            if (!convertedArgument.IsBuilt)
                return convertedArgument;

            arguments.Add(convertedArgument.Value);
        }

        return LoweringAttempt<ExecutionExpression>.Built(new ExecutionMethodCall(
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
