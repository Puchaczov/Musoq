using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class NativeInlineRecordBindings(int firstDefinitionIndex) : ExecutionIrRewriter
{
    private readonly Dictionary<string, ExecutionStructuralCarrier> _carriers = new(StringComparer.Ordinal);
    private readonly Dictionary<int, StructuralTypeDescriptor> _recordShapes = new();
    internal IEnumerable<ExecutionStructuralCarrier> Carriers => _carriers.Values;
    internal IReadOnlyDictionary<int, StructuralTypeDescriptor> RecordShapes => _recordShapes;

    internal ExecutionExpression Bind(ExecutionExpression expression)
        => expression is ExecutionStructuralRecord or ExecutionStructuralArray or ExecutionMethodCall
            ? RewriteExpression(expression) : expression;

    protected override ExecutionExpression RewriteStructuralRecord(ExecutionStructuralRecord record)
    {
        if (record.ConstructionPlan is not null)
            return record;
        var descriptor = Describe(record);
        var shape = ExecutionStructuralShapeFactory.Create(descriptor);
        if (!_carriers.TryGetValue(shape.CanonicalType, out var carrier))
        {
            carrier = new ExecutionStructuralCarrier(firstDefinitionIndex + _carriers.Count, shape);
            _carriers.Add(shape.CanonicalType, carrier);
            _recordShapes.Add(carrier.DefinitionIndex, descriptor);
        }
        return BindValue(record, descriptor, carrier.DefinitionIndex, []);
    }

    protected override ExecutionExpression RewriteStructuralArray(ExecutionStructuralArray array)
    {
        var rewritten = (ExecutionStructuralArray)base.RewriteStructuralArray(array);
        return rewritten.GeneratedElementTypeName is null &&
               rewritten.Elements.FirstOrDefault() is ExecutionStructuralRecord { GeneratedTypeName: { } name }
            ? rewritten with { GeneratedElementTypeName = name } : rewritten;
    }

    private static ExecutionExpression BindValue(ExecutionExpression value, StructuralTypeDescriptor shape,
        int index, int[] path)
    {
        if (value is ExecutionStructuralRecord record)
            return new ExecutionStructuralRecord(record.ReturnType,
                record.Fields.Select((field, fieldIndex) => new ExecutionStructuralField(field.Name,
                    BindValue(field.Value, shape.Fields[fieldIndex].Type, index, [.. path, fieldIndex]), field.IsPresent)))
            {
                GeneratedTypeName = StructuralCarrierNaming.Create(index, path),
                LimitBinding = record.LimitBinding
            };
        if (value is ExecutionStructuralArray array)
            return new ExecutionStructuralArray(array.ReturnType, array.ElementType,
                array.Elements.Select(element => BindValue(element, shape.ElementType!, index, [.. path, -1])))
            {
                GeneratedElementTypeName = shape.ElementType?.Kind == StructuralTypeKind.Record
                    ? StructuralCarrierNaming.Create(index, [.. path, -1]) : null,
                LimitBinding = array.LimitBinding
            };
        return value;
    }

    private static StructuralTypeDescriptor Describe(ExecutionExpression expression)
    {
        if (expression is ExecutionStructuralRecord record)
            return StructuralTypeDescriptor.Record(typeof(StructuralValue), record.Fields.Select(field =>
                new StructuralFieldDescriptor(field.Name, Describe(field.Value), field.IsPresent)));
        if (expression is ExecutionStructuralArray array)
        {
            var element = array.Elements.Count == 0
                ? StructuralTypeDescriptor.Scalar(array.ElementType.ResolveClrType())
                : Describe(array.Elements[0]);
            return StructuralTypeDescriptor.Collection(array.ReturnType.ResolveClrType(), element);
        }
        var type = expression.ReturnType.ResolveClrType();
        var unresolved = ConcreteValueShape.FindUnresolvedMember(type);
        if (unresolved is not null)
            throw new SourcePlanContractException($"Structural field has an unresolved object-valued shape at '{unresolved}'.", TextSpan.Empty);
        var underlying = Nullable.GetUnderlyingType(type);
        return StructuralTypeDescriptor.Scalar(underlying ?? type,
            underlying is not null || !type.IsValueType);
    }
}
