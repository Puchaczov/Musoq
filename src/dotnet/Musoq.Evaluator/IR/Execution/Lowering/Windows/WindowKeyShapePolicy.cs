using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;

namespace Musoq.Evaluator.IR.Execution.Lowering.Windows;

internal static class WindowKeyShapePolicy
{
    internal static ExecutionWindowKeyArray CreateOrderKeyArray(string variableName,
        IReadOnlyList<ExecutionWindowOrderKey> keys)
    {
        var typeName = CreateCarrierName(variableName);
        var parts = keys.Select(static key => new ExecutionWindowGeneratedKeyPart(
            key.Expression.ReturnType, key.Descending, key.NullOrdering)).ToArray();
        var rawType = WindowContextOperationBinding.ResolveRawOrderKeyType(
            keys.Select(static key => key.Expression.ReturnType.ResolveClrType()).ToArray())
            ?? throw new SourcePlanContractException("A window order key requires a concrete value shape.", TextSpan.Empty);
        var shape = new ExecutionWindowKeyShape(rawType, true, typeName, true, parts)
        {
            RawValueType = ExecutionClrBindingFactory.FromClr(rawType)
        };
        return new ExecutionWindowKeyArray(new ExecutionVariable(variableName, rawType.MakeArrayType(), $"{typeName}[]"),
            true, shape);
    }
    internal static ExecutionWindowKeyArray CreatePartitionKeyArray(string variableName,
        ExecutionExpression key)
    {
        var type = WindowRegistrationLoweringHelpers.ResolveWindowPartitionKeyElementType(key);
        if (type != typeof(object))
            return new ExecutionWindowKeyArray(new ExecutionVariable(variableName, type.MakeArrayType()),
                true, new ExecutionWindowKeyShape(type, true));
        var expressions = key switch
        {
            ExecutionValueTupleKey tuple => tuple.Parts,
            _ => new[] { key }
        };
        if (expressions.FirstOrDefault(static part => part.ReturnType.ResolveClrType() == typeof(object)) is { } unresolved)
        {
            var name = unresolved is ExecutionFieldRead field ? field.FieldName : variableName;
            throw new SourcePlanContractException($"Window partition key '{name}' requires a concrete value type.", TextSpan.Empty);
        }
        var typeName = CreateCarrierName(variableName);
        var parts = expressions.Select(static part => new ExecutionWindowGeneratedKeyPart(part.ReturnType, false)).ToArray();
        return new ExecutionWindowKeyArray(new ExecutionVariable(variableName, key.ReturnType.ResolveClrType().MakeArrayType(), $"{typeName}[]"),
            true, new ExecutionWindowKeyShape(key.ReturnType, true, typeName, false, parts));
    }

    private static string CreateCarrierName(string variableName)
    {
        var builder = new StringBuilder(variableName.Length);
        var capitalizeNext = true;
        foreach (var character in variableName)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = true;
                continue;
            }
            builder.Append(capitalizeNext ? char.ToUpperInvariant(character) : character);
            capitalizeNext = false;
        }
        if (builder.Length == 0)
            builder.Append('_');
        if (char.IsDigit(builder[0]))
            builder.Insert(0, '_');
        if (builder.Length > ExecutionSymbolicNamePolicy.MaxIdentifierLength)
            builder.Length = ExecutionSymbolicNamePolicy.MaxIdentifierLength;
        return $"Window{builder}Key";
    }

}
