using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Musoq.Targets.TestPortable;

internal sealed record PortableExecutionContext
{
    public PortableExecutionContext(
        IReadOnlyDictionary<string, PortableValue>? parameters = null,
        IReadOnlyDictionary<string, PortableValue>? scriptVariables = null,
        IReadOnlyDictionary<string, IReadOnlyList<PortableRow>>? sources = null)
    {
        Parameters = FreezeDictionary(parameters);
        ScriptVariables = FreezeDictionary(scriptVariables);
        Sources = new ReadOnlyDictionary<string, IReadOnlyList<PortableRow>>(
            (sources ?? new Dictionary<string, IReadOnlyList<PortableRow>>(StringComparer.Ordinal))
            .ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyList<PortableRow>)Array.AsReadOnly(pair.Value.ToArray()),
                StringComparer.Ordinal));
    }

    public IReadOnlyDictionary<string, PortableValue> Parameters { get; }

    public IReadOnlyDictionary<string, PortableValue> ScriptVariables { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<PortableRow>> Sources { get; }

    public static PortableExecutionContext Empty { get; } = new();

    private static IReadOnlyDictionary<string, PortableValue> FreezeDictionary(
        IReadOnlyDictionary<string, PortableValue>? values)
    {
        return new ReadOnlyDictionary<string, PortableValue>(
            values is null
                ? new Dictionary<string, PortableValue>(StringComparer.Ordinal)
                : new Dictionary<string, PortableValue>(values, StringComparer.Ordinal));
    }
}

internal sealed record PortableTable
{
    public PortableTable(IEnumerable<PortableRow>? rows)
    {
        Rows = Array.AsReadOnly((rows ?? []).ToArray());
    }

    public IReadOnlyList<PortableRow> Rows { get; }
}

internal static class PortableSubsetInterpreter
{
    public static PortableTable Execute(
        PortableSubsetRenderedArtifact artifact,
        PortableExecutionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        var executionContext = context ?? PortableExecutionContext.Empty;
        ValidateHostBindings(artifact.HostAbiInventory, executionContext);
        return Execute(artifact.Program, executionContext);
    }

    public static PortableTable Execute(
        PortableSubsetProgram program,
        PortableExecutionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (!program.SemanticsContract.IsEquivalentTo(ExecutionSemanticsContract.Version1))
        {
            throw new NotSupportedException(
                $"Portable subset interpreter supports semantics fingerprint '{ExecutionSemanticsContract.Version1.Fingerprint}', not '{program.SemanticsFingerprint}'.");
        }

        var state = new InterpreterState(context ?? PortableExecutionContext.Empty);
        var signal = ExecuteBlock(program.Body, state);
        return signal.Result ?? throw new InvalidOperationException("Portable program did not return a table.");
    }

    private static void ValidateHostBindings(
        TargetHostAbiInventory inventory,
        PortableExecutionContext context)
    {
        inventory.ValidateRuntimeServices(
            inventory.CreateServiceRequirements(TargetRuntimeServiceFulfillmentKind.HostImport));

        var missingSourceContexts = inventory.Imports
            .Where(static import => import.Details is TargetSourceAccessAbiDetails)
            .Select(static import => ((TargetSourceAccessAbiDetails)import.Details).SourceContextId)
            .Where(sourceContextId => !context.Sources.ContainsKey(sourceContextId))
            .OrderBy(static sourceContextId => sourceContextId, StringComparer.Ordinal)
            .ToArray();
        if (missingSourceContexts.Length > 0)
        {
            throw new InvalidOperationException(
                $"Portable host bindings are missing required source context(s): {string.Join(", ", missingSourceContexts)}.");
        }
    }

    private static ExecutionSignal ExecuteBlock(PortableBlock block, InterpreterState state)
    {
        foreach (var instruction in block.Instructions)
        {
            var signal = ExecuteInstruction(instruction, state);
            if (signal.Kind != ExecutionSignalKind.None)
                return signal;
        }

        return ExecutionSignal.None;
    }

    private static ExecutionSignal ExecuteInstruction(
        PortableInstruction instruction,
        InterpreterState state)
    {
        switch (instruction)
        {
            case PortableLoadSourceInstruction source:
                if (!state.Context.Sources.TryGetValue(source.SourceContextId, out var rows))
                {
                    throw new KeyNotFoundException(
                        $"Portable source '{source.SourceContextId}' was not supplied.");
                }

                state.Tables[source.RowsVariable] = rows.ToList();
                return ExecutionSignal.None;
            case PortableCreateTableInstruction table:
                state.Tables[table.TableVariable] = [];
                return ExecutionSignal.None;
            case PortableCreateValuesInstruction values:
                state.Tables[values.RowsVariable] = values.Rows
                    .Select(row => CreateRow(row, state))
                    .ToList();
                return ExecutionSignal.None;
            case PortableForEachInstruction loop:
                foreach (var row in GetTable(state, loop.RowsVariable))
                {
                    state.Rows[loop.ItemVariable] = row;
                    var signal = ExecuteBlock(loop.Body, state);
                    if (signal.Kind == ExecutionSignalKind.Return)
                        return signal;
                    if (signal.Kind == ExecutionSignalKind.Continue)
                        continue;
                }

                state.Rows.Remove(loop.ItemVariable);
                return ExecutionSignal.None;
            case PortableLetInstruction let:
                state.Values[let.Variable] = Evaluate(let.Value, state);
                return ExecutionSignal.None;
            case PortableIfInstruction branch:
                return IsTrue(Evaluate(branch.Condition, state))
                    ? ExecuteBlock(branch.Body, state)
                    : ExecutionSignal.None;
            case PortableContinueInstruction:
                return ExecutionSignal.Continue;
            case PortableContinueIfInstruction condition:
                return IsTrue(Evaluate(condition.Condition, state))
                    ? ExecutionSignal.Continue
                    : ExecutionSignal.None;
            case PortableAppendRowInstruction append:
                GetTable(state, append.TableVariable).Add(CreateRow(append.Values, state));
                return ExecutionSignal.None;
            case PortableCreateRowInstruction row:
                state.Rows[row.RowVariable] = CreateRow(row.Values, state);
                return ExecutionSignal.None;
            case PortableOrderSliceInstruction order:
                var ordered = order.Order.Count == 0
                    ? GetTable(state, order.SourceVariable).ToArray()
                    : GetTable(state, order.SourceVariable)
                        .OrderBy(static row => row, new PortableRowComparer(order.Order))
                        .ToArray();
                state.Tables[order.TargetVariable] = ordered
                    .Skip(order.Skip)
                    .Take(order.Take ?? int.MaxValue)
                    .ToList();
                return ExecutionSignal.None;
            case PortableReturnInstruction result:
                return ExecutionSignal.Return(new PortableTable(GetTable(state, result.TableVariable)));
            default:
                throw new InvalidOperationException(
                    $"Portable interpreter does not implement instruction '{instruction.GetType().Name}'.");
        }
    }

    private static PortableRow CreateRow(
        IEnumerable<PortableRowValue> values,
        InterpreterState state)
    {
        return new PortableRow(values.Select(value =>
            new KeyValuePair<string, PortableValue>(value.FieldName, Evaluate(value.Value, state))));
    }

    private static PortableValue Evaluate(PortableExpression expression, InterpreterState state) => expression switch
    {
        PortableLiteralExpression literal => literal.Value,
        PortableFieldExpression field => ResolveField(field, state),
        PortableParameterExpression parameter => GetRequired(state.Context.Parameters, parameter.Name, "parameter"),
        PortableScriptVariableExpression variable => GetRequired(state.Context.ScriptVariables, variable.Name, "script variable"),
        PortableVariableExpression variable => GetRequired(state.Values, variable.Name, "variable"),
        PortableBinaryExpression binary => EvaluateBinary(
            binary.Operation,
            Evaluate(binary.Left, state),
            Evaluate(binary.Right, state)),
        PortableUnaryExpression unary => EvaluateUnary(unary.Operation, Evaluate(unary.Operand, state)),
        PortableNullCheckExpression nullCheck => PortableValue.FromBoolean(
            Evaluate(nullCheck.Expression, state).IsNull != nullCheck.IsNegated),
        PortableCoalesceExpression coalesce => coalesce.Expressions
            .Select(expressionValue => Evaluate(expressionValue, state))
            .FirstOrDefault(static value => !value.IsNull, PortableValue.Null),
        PortableCaseExpression @case => EvaluateCase(@case, state),
        PortableInExpression @in => EvaluateIn(@in, state),
        PortableStrictCastExpression cast => EvaluateStrictCast(
            Evaluate(cast.Expression, state),
            cast.TargetTypeName),
        _ => throw new InvalidOperationException(
            $"Portable interpreter does not implement expression '{expression.GetType().Name}'.")
    };

    private static PortableValue ResolveField(PortableFieldExpression field, InterpreterState state)
    {
        if (field.Alias != null && state.Rows.TryGetValue(field.Alias, out var aliasedRow))
            return aliasedRow[field.FieldName];

        var matches = state.Rows.Values
            .SelectMany(static row => row.Fields)
            .Where(pair => string.Equals(pair.Key, field.FieldName, StringComparison.Ordinal))
            .Select(static pair => pair.Value)
            .ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new KeyNotFoundException($"Portable field '{field.FieldName}' was not found."),
            _ => throw new InvalidOperationException($"Portable field '{field.FieldName}' is ambiguous.")
        };
    }

    private static PortableValue EvaluateCase(PortableCaseExpression expression, InterpreterState state)
    {
        foreach (var branch in expression.Branches)
        {
            if (IsTrue(Evaluate(branch.Condition, state)))
                return Evaluate(branch.Result, state);
        }

        return expression.ElseExpression is null
            ? PortableValue.Null
            : Evaluate(expression.ElseExpression, state);
    }

    private static PortableValue EvaluateIn(PortableInExpression expression, InterpreterState state)
    {
        var candidate = Evaluate(expression.Expression, state);
        if (candidate.IsNull)
            return PortableValue.Null;

        var sawNull = false;
        foreach (var valueExpression in expression.Values)
        {
            var value = Evaluate(valueExpression, state);
            if (value.IsNull)
            {
                sawNull = true;
                continue;
            }

            if (ValuesEqual(candidate, value))
                return PortableValue.FromBoolean(true);
        }

        return sawNull ? PortableValue.Null : PortableValue.FromBoolean(false);
    }

    private static PortableValue EvaluateUnary(PortableUnaryOperation operation, PortableValue operand)
    {
        if (operand.IsNull)
            return PortableValue.Null;

        return operation switch
        {
            PortableUnaryOperation.Not when operand.Kind == PortableValueKind.Boolean => PortableValue.FromBoolean(!operand.Boolean),
            PortableUnaryOperation.Negate when operand.Kind == PortableValueKind.SignedInteger =>
                CreateSignedWrapped(unchecked(-operand.SignedInteger), operand.BitWidth),
            PortableUnaryOperation.Negate when operand.Kind == PortableValueKind.Decimal =>
                PortableValue.FromDecimal(checked(-operand.AsDecimal())),
            PortableUnaryOperation.Negate when operand.Kind == PortableValueKind.FloatingPoint =>
                PortableValue.FromDouble(-operand.AsDouble()),
            _ => throw new InvalidOperationException(
                $"Portable unary operation '{operation}' does not support '{operand.Kind}'.")
        };
    }

    private static PortableValue EvaluateBinary(
        PortableBinaryOperation operation,
        PortableValue left,
        PortableValue right)
    {
        if (operation == PortableBinaryOperation.IsDistinctFrom)
            return PortableValue.FromBoolean(left.IsNull != right.IsNull || !left.IsNull && !ValuesEqual(left, right));
        if (operation == PortableBinaryOperation.IsNotDistinctFrom)
            return PortableValue.FromBoolean(left.IsNull == right.IsNull && (left.IsNull || ValuesEqual(left, right)));
        if (operation is PortableBinaryOperation.And or PortableBinaryOperation.Or)
            return EvaluateThreeValuedBoolean(operation, left, right);
        if (left.IsNull || right.IsNull)
            return PortableValue.Null;

        if (operation is PortableBinaryOperation.Equal or PortableBinaryOperation.NotEqual)
        {
            var equal = ValuesEqual(left, right);
            return PortableValue.FromBoolean(operation == PortableBinaryOperation.Equal ? equal : !equal);
        }

        if (operation is PortableBinaryOperation.GreaterThan or PortableBinaryOperation.LessThan or
            PortableBinaryOperation.GreaterOrEqual or PortableBinaryOperation.LessOrEqual)
        {
            var comparison = Compare(left, right);
            return PortableValue.FromBoolean(operation switch
            {
                PortableBinaryOperation.GreaterThan => comparison > 0,
                PortableBinaryOperation.LessThan => comparison < 0,
                PortableBinaryOperation.GreaterOrEqual => comparison >= 0,
                _ => comparison <= 0
            });
        }

        if (operation == PortableBinaryOperation.StringConcatenate &&
            left.Kind == PortableValueKind.String &&
            right.Kind == PortableValueKind.String)
        {
            return PortableValue.FromString(left.Text + right.Text);
        }

        return EvaluateArithmetic(operation, left, right);
    }

    private static PortableValue EvaluateThreeValuedBoolean(
        PortableBinaryOperation operation,
        PortableValue left,
        PortableValue right)
    {
        bool? leftValue = left.IsNull ? null : left.Boolean;
        bool? rightValue = right.IsNull ? null : right.Boolean;
        bool? result = operation == PortableBinaryOperation.And
            ? leftValue == false || rightValue == false
                ? false
                : leftValue == true && rightValue == true ? true : null
            : leftValue == true || rightValue == true
                ? true
                : leftValue == false && rightValue == false ? false : null;
        return result.HasValue ? PortableValue.FromBoolean(result.Value) : PortableValue.Null;
    }

    private static PortableValue EvaluateArithmetic(
        PortableBinaryOperation operation,
        PortableValue left,
        PortableValue right)
    {
        if (left.Kind == PortableValueKind.SignedInteger && right.Kind == PortableValueKind.SignedInteger)
        {
            var bitWidth = Math.Max(left.BitWidth, right.BitWidth);
            if (operation is PortableBinaryOperation.Add or PortableBinaryOperation.Subtract or PortableBinaryOperation.Multiply)
            {
                var value = operation switch
                {
                    PortableBinaryOperation.Add => unchecked(left.SignedInteger + right.SignedInteger),
                    PortableBinaryOperation.Subtract => unchecked(left.SignedInteger - right.SignedInteger),
                    _ => unchecked(left.SignedInteger * right.SignedInteger)
                };
                return CreateSignedWrapped(value, bitWidth);
            }

            if (bitWidth == 32 &&
                left.SignedInteger == int.MinValue &&
                right.SignedInteger == -1 &&
                operation is PortableBinaryOperation.Divide or PortableBinaryOperation.Modulo)
            {
                throw new OverflowException("Integer division overflowed the portable Int32 range.");
            }

            return CreateSigned(operation switch
            {
                PortableBinaryOperation.Divide => left.SignedInteger / right.SignedInteger,
                PortableBinaryOperation.Modulo => left.SignedInteger % right.SignedInteger,
                _ => throw UnsupportedArithmetic(operation, left.Kind)
            }, bitWidth);
        }

        if (left.Kind == PortableValueKind.Decimal && right.Kind == PortableValueKind.Decimal)
        {
            var leftDecimal = left.AsDecimal();
            var rightDecimal = right.AsDecimal();
            return PortableValue.FromDecimal(operation switch
            {
                PortableBinaryOperation.Add => checked(leftDecimal + rightDecimal),
                PortableBinaryOperation.Subtract => checked(leftDecimal - rightDecimal),
                PortableBinaryOperation.Multiply => checked(leftDecimal * rightDecimal),
                PortableBinaryOperation.Divide => leftDecimal / rightDecimal,
                PortableBinaryOperation.Modulo => leftDecimal % rightDecimal,
                _ => throw UnsupportedArithmetic(operation, left.Kind)
            });
        }

        if (left.Kind == PortableValueKind.FloatingPoint && right.Kind == PortableValueKind.FloatingPoint)
        {
            var leftDouble = left.AsDouble();
            var rightDouble = right.AsDouble();
            return PortableValue.FromDouble(operation switch
            {
                PortableBinaryOperation.Add => leftDouble + rightDouble,
                PortableBinaryOperation.Subtract => leftDouble - rightDouble,
                PortableBinaryOperation.Multiply => leftDouble * rightDouble,
                PortableBinaryOperation.Divide => leftDouble / rightDouble,
                PortableBinaryOperation.Modulo => leftDouble % rightDouble,
                _ => throw UnsupportedArithmetic(operation, left.Kind)
            });
        }

        throw new InvalidOperationException(
            $"Portable arithmetic requires matching numeric kinds, but got '{left.Kind}' and '{right.Kind}'.");
    }

    private static PortableValue EvaluateStrictCast(PortableValue value, string targetTypeName)
    {
        if (value.IsNull)
            return PortableValue.Null;

        return targetTypeName.ToLowerInvariant() switch
        {
            "string" => PortableValue.FromString(value.ToManifestValue()),
            "int" or "int32" when value.Kind == PortableValueKind.String =>
                PortableValue.FromSigned(int.Parse(value.Text, NumberStyles.Integer, CultureInfo.InvariantCulture), 32),
            "long" or "int64" when value.Kind == PortableValueKind.String =>
                PortableValue.FromSigned(long.Parse(value.Text, NumberStyles.Integer, CultureInfo.InvariantCulture)),
            "decimal" when value.Kind == PortableValueKind.String =>
                PortableValue.FromDecimal(decimal.Parse(value.Text, NumberStyles.Number, CultureInfo.InvariantCulture)),
            _ => value
        };
    }

    private static int Compare(PortableValue left, PortableValue right)
    {
        if (left.Kind != right.Kind)
            throw new InvalidOperationException($"Cannot compare '{left.Kind}' and '{right.Kind}'.");

        return left.Kind switch
        {
            PortableValueKind.Boolean => left.Boolean.CompareTo(right.Boolean),
            PortableValueKind.SignedInteger => left.SignedInteger.CompareTo(right.SignedInteger),
            PortableValueKind.UnsignedInteger => left.UnsignedInteger.CompareTo(right.UnsignedInteger),
            PortableValueKind.FloatingPoint => left.AsDouble().CompareTo(right.AsDouble()),
            PortableValueKind.Decimal => left.AsDecimal().CompareTo(right.AsDecimal()),
            PortableValueKind.String => string.CompareOrdinal(left.Text, right.Text),
            _ => throw new InvalidOperationException($"Portable value kind '{left.Kind}' is not orderable.")
        };
    }

    private static bool ValuesEqual(PortableValue left, PortableValue right)
    {
        if (left.Kind != right.Kind)
            return false;

        return left.Kind switch
        {
            PortableValueKind.Null => true,
            PortableValueKind.Boolean => left.Boolean == right.Boolean,
            PortableValueKind.SignedInteger => left.SignedInteger == right.SignedInteger,
            PortableValueKind.UnsignedInteger => left.UnsignedInteger == right.UnsignedInteger,
            PortableValueKind.FloatingPoint => left.FloatingPointBits == right.FloatingPointBits,
            PortableValueKind.Decimal => left.AsDecimal() == right.AsDecimal(),
            PortableValueKind.String => string.Equals(left.Text, right.Text, StringComparison.Ordinal),
            _ => false
        };
    }

    private static PortableValue CreateSigned(long value, int bitWidth)
    {
        _ = bitWidth switch
        {
            8 => checked((sbyte)value),
            16 => checked((short)value),
            32 => checked((int)value),
            64 => value,
            _ => throw new InvalidOperationException($"Unsupported signed integer width '{bitWidth}'.")
        };
        return PortableValue.FromSigned(value, bitWidth);
    }

    private static PortableValue CreateSignedWrapped(long value, int bitWidth)
    {
        return bitWidth switch
        {
            8 => PortableValue.FromSigned(unchecked((sbyte)value), bitWidth),
            16 => PortableValue.FromSigned(unchecked((short)value), bitWidth),
            32 => PortableValue.FromSigned(unchecked((int)value), bitWidth),
            64 => PortableValue.FromSigned(value, bitWidth),
            _ => throw new InvalidOperationException($"Unsupported signed integer width '{bitWidth}'.")
        };
    }

    private static bool IsTrue(PortableValue value) =>
        value.Kind == PortableValueKind.Boolean && value.Boolean;

    private static PortableValue GetRequired(
        IReadOnlyDictionary<string, PortableValue> values,
        string name,
        string kind)
    {
        return values.TryGetValue(name, out var value)
            ? value
            : throw new KeyNotFoundException($"Portable {kind} '{name}' was not supplied.");
    }

    private static List<PortableRow> GetTable(InterpreterState state, string name) =>
        state.Tables.TryGetValue(name, out var table)
            ? table
            : throw new KeyNotFoundException($"Portable table '{name}' does not exist.");

    private static InvalidOperationException UnsupportedArithmetic(
        PortableBinaryOperation operation,
        PortableValueKind kind) =>
        new($"Portable arithmetic operation '{operation}' does not support '{kind}'.");

    private sealed class InterpreterState(PortableExecutionContext context)
    {
        public PortableExecutionContext Context { get; } = context;

        public Dictionary<string, PortableValue> Values { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, PortableRow> Rows { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, List<PortableRow>> Tables { get; } = new(StringComparer.Ordinal);
    }

    private enum ExecutionSignalKind
    {
        None,
        Continue,
        Return
    }

    private readonly record struct ExecutionSignal(ExecutionSignalKind Kind, PortableTable? Result)
    {
        public static ExecutionSignal None { get; } = new(ExecutionSignalKind.None, null);

        public static ExecutionSignal Continue { get; } = new(ExecutionSignalKind.Continue, null);

        public static ExecutionSignal Return(PortableTable result) => new(ExecutionSignalKind.Return, result);
    }

    private sealed class PortableRowComparer(IReadOnlyList<PortableOrderField> order) : IComparer<PortableRow>
    {
        public int Compare(PortableRow? left, PortableRow? right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            foreach (var field in order)
            {
                var leftValue = left[field.FieldName];
                var rightValue = right[field.FieldName];
                var comparison = CompareValues(leftValue, rightValue);
                if (comparison != 0)
                    return field.Descending ? -comparison : comparison;
            }

            return 0;
        }

        private static int CompareValues(PortableValue left, PortableValue right)
        {
            if (left.IsNull)
                return right.IsNull ? 0 : -1;
            if (right.IsNull)
                return 1;
            return PortableSubsetInterpreter.Compare(left, right);
        }
    }
}
