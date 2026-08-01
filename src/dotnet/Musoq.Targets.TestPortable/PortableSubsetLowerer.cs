using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Targets.TestPortable;

internal static class PortableSubsetLowerer
{
    public static PortableSubsetProgram Lower(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = new LoweringState();
        var body = LowerBlock(plan.Body, state);
        state.ThrowIfUnresolvedObjectTargets();
        return new PortableSubsetProgram(
            plan.Identifier,
            plan.SemanticsContract,
            body);
    }

    private static PortableBlock LowerBlock(ExecutionBlock block, LoweringState state)
    {
        var instructions = new List<PortableInstruction>();
        foreach (var node in block.Nodes)
        {
            if (node is ExecutionScopedBlock scoped)
            {
                instructions.AddRange(LowerBlock(scoped.Body, state).Instructions);
                continue;
            }

            if (node is ExecutionCreateObject createObject)
            {
                state.DeclareObjectTarget(createObject.Target.Name);
                continue;
            }

            instructions.Add(LowerNode(node, state));
        }

        return new PortableBlock(instructions);
    }

    private static PortableInstruction LowerNode(ExecutionNode node, LoweringState state) => node switch
    {
        ExecutionSourceScan source => new PortableLoadSourceInstruction(
            source.Rows.Name,
            source.Binding.RuntimeContextId),
        ExecutionCreateTable table => new PortableCreateTableInstruction(table.Table.Name),
        ExecutionCreateValuesRows values => new PortableCreateValuesInstruction(
            values.Rows.Name,
            Array.AsReadOnly(values.Values
                .Select(row => (IReadOnlyList<PortableRowValue>)Array.AsReadOnly(
                    row.Select(value => LowerRowValue(value, state)).ToArray()))
                .ToArray())),
        ExecutionForEach loop => new PortableForEachInstruction(
            loop.Item.Name,
            ResolveRowsVariable(loop.Source),
            LowerBlock(loop.Body, state)),
        ExecutionLet let => new PortableLetInstruction(let.Variable.Name, LowerExpression(let.Value, state)),
        ExecutionAssign assign => new PortableLetInstruction(assign.Variable.Name, LowerExpression(assign.Value, state)),
        ExecutionIf branch => new PortableIfInstruction(LowerExpression(branch.Condition, state), LowerBlock(branch.Body, state)),
        ExecutionContinue => new PortableContinueInstruction(),
        ExecutionContinueIf condition => new PortableContinueIfInstruction(LowerExpression(condition.Condition, state)),
        ExecutionAppendRow append => new PortableAppendRowInstruction(
            append.Table.Name,
            Array.AsReadOnly(append.Values.Select(value => LowerRowValue(value, state)).ToArray())),
        ExecutionCreateGeneratedRow row => new PortableCreateRowInstruction(
            row.Row.Name,
            Array.AsReadOnly(row.Values.Select(value => LowerRowValue(value, state)).ToArray())),
        ExecutionSortTable sort => LowerOrderSlice(sort.Source.Name, sort.Target.Name, sort.Keys, 0, null),
        ExecutionTopOffsetTable top => LowerOrderSlice(
            top.Source.Name,
            top.Target.Name,
            top.Keys,
            top.SkipCount,
            top.TakeCount),
        ExecutionSkipTable skip => new PortableOrderSliceInstruction(
            skip.Source.Name,
            skip.Target.Name,
            [],
            skip.Count,
            null),
        ExecutionTakeTable take => new PortableOrderSliceInstruction(
            take.Source.Name,
            take.Target.Name,
            [],
            0,
            take.Count),
        ExecutionSliceTable slice => new PortableOrderSliceInstruction(
            slice.Source.Name,
            slice.Target.Name,
            [],
            slice.SkipCount,
            slice.TakeCount),
        ExecutionReturnTable result => new PortableReturnInstruction(result.Table.Name),
        _ => throw new InvalidOperationException(
            $"Operation capability admitted execution node '{node.GetType().Name}', but the portable subset lowerer has no implementation.")
    };

    private static PortableOrderSliceInstruction LowerOrderSlice(
        string source,
        string target,
        IReadOnlyList<ExecutionOrderField> keys,
        int skip,
        int? take)
    {
        return new PortableOrderSliceInstruction(
            source,
            target,
            Array.AsReadOnly(keys.Select(static key => new PortableOrderField(key.FieldName, key.Descending)).ToArray()),
            skip,
            take);
    }

    private static PortableRowValue LowerRowValue(ExecutionRowValue value, LoweringState state) =>
        new(value.FieldName, LowerExpression(value.Value, state));

    private static string ResolveRowsVariable(ExecutionExpression source) => source switch
    {
        ExecutionRowStream stream => stream.Variable.Name,
        ExecutionVariableRead variable => variable.Variable.Name,
        _ => throw new InvalidOperationException(
            $"Portable foreach source '{source.GetType().Name}' is not a named row stream.")
    };

    private static PortableExpression LowerExpression(ExecutionExpression expression, LoweringState state) => expression switch
    {
        ExecutionLiteral literal => new PortableLiteralExpression(LowerConstant(literal.Value)),
        ExecutionFieldRead field => new PortableFieldExpression(field.Alias, field.FieldName),
        ExecutionMemberRead member => throw new PortableSubsetLoweringException(
            $"Portable subset does not support runtime member '{member.MemberName}'."),
        ExecutionScriptParameterRead parameter => new PortableParameterExpression(parameter.Name),
        ExecutionScriptVariableRead variable => new PortableScriptVariableExpression(variable.Name),
        ExecutionVariableRead variable => new PortableVariableExpression(variable.Variable.Name),
        ExecutionBinary binary => new PortableBinaryExpression(
            LowerBinaryOperation(binary.Kind),
            LowerExpression(binary.Left, state),
            LowerExpression(binary.Right, state)),
        ExecutionUnary unary => new PortableUnaryExpression(
            LowerUnaryOperation(unary.Kind),
            LowerExpression(unary.Operand, state)),
        ExecutionMethodCall { Method.Descriptor.IntrinsicKind: ExecutionIntrinsicCallableKind.Coalesce } call =>
            LowerIntrinsicCoalesce(call, state),
        ExecutionMethodCall call => throw new PortableSubsetLoweringException(
            $"Portable subset does not support callable '{call.Method.StableId}'."),
        ExecutionIsNullCheck nullCheck => new PortableNullCheckExpression(
            LowerExpression(nullCheck.Expression, state),
            nullCheck.IsNegated),
        ExecutionCoalesce coalesce => new PortableCoalesceExpression(
            Array.AsReadOnly(coalesce.Expressions.Select(value => LowerExpression(value, state)).ToArray())),
        ExecutionCaseWhen @case => new PortableCaseExpression(
            Array.AsReadOnly(@case.Branches.Select(branch => new PortableCaseBranch(
                LowerExpression(branch.Condition, state),
                LowerExpression(branch.Result, state))).ToArray()),
            @case.ElseExpression is null ? null : LowerExpression(@case.ElseExpression, state)),
        ExecutionInCheck @in => new PortableInExpression(
            LowerExpression(@in.Expression, state),
            Array.AsReadOnly(@in.Values.Select(value => LowerExpression(value, state)).ToArray())),
        ExecutionStrictCast cast => new PortableStrictCastExpression(
            LowerExpression(cast.Expression, state),
            cast.TargetTypeName),
        _ => throw new InvalidOperationException(
            $"Operation capability admitted execution expression '{expression.GetType().Name}', but the portable subset lowerer has no implementation.")
    };

    private static PortableExpression LowerIntrinsicCoalesce(
        ExecutionMethodCall call,
        LoweringState state)
    {
        if (call.Target is { } target)
            state.ConsumeObjectTarget(target.Name);

        return new PortableCoalesceExpression(
            Array.AsReadOnly(call.Arguments.Select(argument => LowerExpression(argument, state)).ToArray()));
    }

    private static PortableValue LowerConstant(ExecutionConstantValue value) => value.Kind switch
    {
        ExecutionConstantKind.Null => PortableValue.Null,
        ExecutionConstantKind.Boolean => PortableValue.FromBoolean(value.UnsignedInteger != 0),
        ExecutionConstantKind.Character => PortableValue.FromCharacter(checked((char)value.UnsignedInteger)),
        ExecutionConstantKind.SignedInteger => PortableValue.FromSigned(value.SignedInteger, value.BitWidth),
        ExecutionConstantKind.UnsignedInteger => PortableValue.FromUnsigned(value.UnsignedInteger, value.BitWidth),
        ExecutionConstantKind.FloatingPoint when value.BitWidth is 32 or 64 =>
            PortableValue.FromFloatingPointBits(value.BitWidth, value.FloatingPointBits),
        ExecutionConstantKind.Decimal when value.DecimalBits.Count == 4 => PortableValue.FromDecimal(
            new decimal(value.DecimalBits.ToArray())),
        ExecutionConstantKind.String => PortableValue.FromString(
            new string(value.Utf16CodeUnits.Select(static unit => (char)unit).ToArray())),
        ExecutionConstantKind.DateTime => PortableValue.FromDateTime(value.Ticks, value.DateTimeKind),
        ExecutionConstantKind.DateTimeOffset => PortableValue.FromDateTimeOffset(value.Ticks, value.OffsetMinutes),
        ExecutionConstantKind.Guid => PortableValue.FromGuid(value.GuidBytes),
        ExecutionConstantKind.TimeSpan => PortableValue.FromTimeSpan(value.Ticks),
        ExecutionConstantKind.Enum when value.EnumType is { } enumType && value.EnumUnderlyingValue is { } underlying =>
            PortableValue.FromEnum(enumType.Descriptor.StableName, LowerConstant(underlying)),
        _ => throw new InvalidOperationException(
            $"Portable subset does not support constant kind '{value.Kind}'.")
    };

    private static PortableBinaryOperation LowerBinaryOperation(BinaryOpKind operation) => operation switch
    {
        BinaryOpKind.Add => PortableBinaryOperation.Add,
        BinaryOpKind.Subtract => PortableBinaryOperation.Subtract,
        BinaryOpKind.Multiply => PortableBinaryOperation.Multiply,
        BinaryOpKind.Divide => PortableBinaryOperation.Divide,
        BinaryOpKind.Modulo => PortableBinaryOperation.Modulo,
        BinaryOpKind.And => PortableBinaryOperation.And,
        BinaryOpKind.Or => PortableBinaryOperation.Or,
        BinaryOpKind.Equal => PortableBinaryOperation.Equal,
        BinaryOpKind.NotEqual => PortableBinaryOperation.NotEqual,
        BinaryOpKind.IsDistinctFrom => PortableBinaryOperation.IsDistinctFrom,
        BinaryOpKind.IsNotDistinctFrom => PortableBinaryOperation.IsNotDistinctFrom,
        BinaryOpKind.GreaterThan => PortableBinaryOperation.GreaterThan,
        BinaryOpKind.LessThan => PortableBinaryOperation.LessThan,
        BinaryOpKind.GreaterOrEqual => PortableBinaryOperation.GreaterOrEqual,
        BinaryOpKind.LessOrEqual => PortableBinaryOperation.LessOrEqual,
        BinaryOpKind.StringConcatenate => PortableBinaryOperation.StringConcatenate,
        _ => throw new InvalidOperationException($"Portable subset does not support binary operation '{operation}'.")
    };

    private static PortableUnaryOperation LowerUnaryOperation(UnaryOpKind operation) => operation switch
    {
        UnaryOpKind.Not => PortableUnaryOperation.Not,
        UnaryOpKind.Negate => PortableUnaryOperation.Negate,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };

    private sealed class LoweringState
    {
        private readonly HashSet<string> _declaredObjectTargets = new(StringComparer.Ordinal);
        private readonly HashSet<string> _consumedObjectTargets = new(StringComparer.Ordinal);

        public void DeclareObjectTarget(string targetName)
        {
            _declaredObjectTargets.Add(targetName);
        }

        public void ConsumeObjectTarget(string targetName)
        {
            if (_declaredObjectTargets.Contains(targetName))
                _consumedObjectTargets.Add(targetName);
        }

        public void ThrowIfUnresolvedObjectTargets()
        {
            var unresolved = _declaredObjectTargets
                .Except(_consumedObjectTargets, StringComparer.Ordinal)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();
            if (unresolved.Length == 0)
                return;

            throw new PortableSubsetLoweringException(
                $"Portable subset cannot lower CLR object construction for target(s): {string.Join(", ", unresolved)}.");
        }
    }
}

internal sealed class PortableSubsetLoweringException(string message) : Exception(message);
