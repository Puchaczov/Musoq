using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Execution;

internal static class StructuralSourcePreparation
{
    internal static PreparedStructuralArguments PrepareStructuralSourceArguments(
        IReadOnlyList<ExecutionExpression> arguments,
        IReadOnlyList<ExecutionStructuralLimitPlan?>? structuralArgumentLimits,
        string sourceAlias,
        string? scope = null,
        string? sourceContextId = null)
    {
        var state = new StructuralPreparationState(sourceAlias, scope);
        var preparedArguments = new ExecutionExpression[arguments.Count];
        for (var index = 0; index < arguments.Count; index++)
        {
            var original = arguments[index];
            var prepared = state.Prepare(original);
            preparedArguments[index] = structuralArgumentLimits is { Count: > 0 } &&
                                       index < structuralArgumentLimits.Count &&
                                       structuralArgumentLimits[index] is { } limits &&
                                       IsStructured(prepared)
                ? AttachLimitBinding(
                    prepared,
                    limits,
                    DetermineOrigin(original),
                    DeterminePath(original, sourceAlias, index),
                    sourceContextId)
                : prepared;
        }

        return new PreparedStructuralArguments(preparedArguments, state.Preparations);
    }

    internal sealed record PreparedStructuralArguments(
        IReadOnlyList<ExecutionExpression> Arguments,
        IReadOnlyList<ExecutionNode> Preparations);

    private sealed class StructuralPreparationState
    {
        private readonly string _prefix;
        private int _ordinal;

        public StructuralPreparationState(string sourceAlias, string? scope)
        {
            _prefix = CreateStructuralPreparationPrefix(sourceAlias, scope);
        }

        public List<ExecutionNode> Preparations { get; } = [];

        public ExecutionExpression Prepare(ExecutionExpression expression)
        {
            return expression switch
            {
                ExecutionStructuralRecord record => PrepareRecord(record),
                ExecutionStructuralArray array => PrepareArray(array),
                ExecutionStructuralConversion conversion => PrepareConversion(conversion),
                _ => expression
            };
        }

        private ExecutionExpression PrepareRecord(ExecutionStructuralRecord record)
        {
            var fields = new ExecutionStructuralField[record.Fields.Count];
            for (var index = 0; index < record.Fields.Count; index++)
            {
                var field = record.Fields[index];
                var value = Prepare(field.Value);

                // Structural children already materialize themselves (and return
                // a variable read).  Scalar expressions, however, must be
                // evaluated in authored order before the receiver constructor
                // is rendered in constructor order.  Retaining every scalar
                // field here gives the renderer typed locals to consume and
                // prevents C# argument evaluation from reordering user code.
                if (field.Value is not (ExecutionStructuralRecord or ExecutionStructuralArray or ExecutionStructuralConversion))
                    value = Retain(value);

                fields[index] = new ExecutionStructuralField(field.Name, value, field.IsPresent);
            }

            var preparedRecord = new ExecutionStructuralRecord(record.ReturnType, fields, record.ConstructionPlan);
            if (record.ConstructionPlan is { } plan)
            {
                var target = new ExecutionVariable(
                    $"{_prefix}_{_ordinal++}",
                    plan.TargetType);
                Preparations.Add(new ExecutionPrepareStructuralInput(target, preparedRecord, plan));
                return new ExecutionVariableRead(target);
            }

            return Retain(preparedRecord);
        }

        private ExecutionExpression PrepareArray(ExecutionStructuralArray array)
        {
            var elements = new ExecutionExpression[array.Elements.Count];
            for (var index = 0; index < array.Elements.Count; index++)
                elements[index] = Prepare(array.Elements[index]);

            return Retain(new ExecutionStructuralArray(array.ReturnType, array.ElementType, elements));
        }

        private ExecutionExpression PrepareConversion(ExecutionStructuralConversion conversion)
        {
            var input = Prepare(conversion.Input);
            return Retain(new ExecutionStructuralConversion(input, conversion.TargetType));
        }

        private ExecutionExpression Retain(ExecutionExpression expression)
        {
            var variable = new ExecutionVariable(
                $"{_prefix}_{_ordinal++}",
                expression.ReturnType);
            Preparations.Add(new ExecutionLet(variable, expression, ExecutionLetCacheMode.SuppressMethodCache));
            return new ExecutionVariableRead(variable);
        }
    }

    private static bool IsStructured(ExecutionExpression expression)
    {
        if (expression is ExecutionStructuralRecord or ExecutionStructuralArray or ExecutionCteCollectionInput)
            return true;

        try
        {
            var type = expression.ReturnType.ResolveClrType();
            return StructuralTypeDescriptor.FromClrType(type).Kind != StructuralTypeKind.Scalar;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static ExecutionStructuralInputOrigin DetermineOrigin(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionCteCollectionInput => ExecutionStructuralInputOrigin.Cte,
            ExecutionScriptParameterRead => ExecutionStructuralInputOrigin.Parameter,
            ExecutionScriptVariableRead => ExecutionStructuralInputOrigin.Let,
            ExecutionStructuralConversion conversion => DetermineOrigin(conversion.Input),
            _ => ExecutionStructuralInputOrigin.Inline
        };
    }

    private static string DeterminePath(
        ExecutionExpression expression,
        string sourceAlias,
        int argumentIndex)
    {
        return expression switch
        {
            ExecutionScriptParameterRead parameter => "$" + parameter.Name,
            ExecutionScriptVariableRead variable => "$" + variable.Name,
            ExecutionStructuralConversion conversion => DeterminePath(conversion.Input, sourceAlias, argumentIndex),
            _ => $"{sourceAlias}.argument[{argumentIndex}]"
        };
    }

    private static ExecutionExpression AttachLimitBinding(
        ExecutionExpression expression,
        ExecutionStructuralLimitPlan limits,
        ExecutionStructuralInputOrigin origin,
        string path,
        string? sourceContextId)
    {
        var binding = new ExecutionStructuralLimitBinding(limits, origin, path, sourceContextId);

        if (expression is ExecutionCteCollectionInput cte)
        {
            // CTE adaptation owns its allocation boundary.  Keep the binding
            // on the relation so the generated helper can measure typed rows
            // before allocating the receiving collection.
            return cte with { LimitBinding = binding };
        }

        // The preparation pass retains the root in a typed local.  Wrapping
        // that local in a conversion-shaped guard lets the renderer insert a
        // single demand check without changing the expression's static type.
        return new ExecutionStructuralConversion(
            expression,
            expression.ReturnType)
        {
            LimitBinding = binding
        };
    }

    private static string CreateStructuralPreparationPrefix(string sourceAlias, string? scope)
    {
        var aliasChars = sourceAlias.Select(static character =>
            char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray();
        if (string.IsNullOrWhiteSpace(scope))
            return $"__musoqStructural_{new string(aliasChars)}";

        var scopeChars = scope.Select(static character =>
            char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray();
        return $"__musoqStructural_{new string(scopeChars)}_{new string(aliasChars)}";
    }
}
