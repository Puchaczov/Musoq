using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal static class QueryRowSourceShapeFactory
{
    public static GeneratedRowShape Create(
        string alias,
        SourceTransferStrategyPlan transfer)
    {
        var fields = transfer.Shape!.Fields
            .Select(field => new FieldBinding(
                field.Name,
                $"{alias}.{field.Name}",
                field.Slot,
                field.FieldType,
                field.IsNullable ? FieldNullability.Nullable : FieldNullability.NotNullable,
                new GeneratedFieldAccess(QueryRowSourceNaming.CreateFieldName(field.Slot)),
                readModifiers: field.ReadModifiers,
                sourceReadType: field.SourceReadType,
                enumType: field.EnumType))
            .ToArray();

        return new GeneratedRowShape(
            QueryRowSourceNaming.CreateCarrierTypeName(transfer.Shape.Fingerprint, transfer.Carrier!.Value),
            fields,
            [],
            supportsGeneratedFieldAccess: true,
            requiresRowBase: false)
        {
            EmitAsValueType = transfer.Carrier == SourceQueryRowCarrier.ReadonlyStruct,
            IsQueryScopedRow = true,
            SourceAlias = alias
        };
    }
}
