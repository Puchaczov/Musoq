namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static TableRowShape CreateMaterializedTransitionTableRowShape(
        string alias,
        GeneratedRowShape rowShape)
    {
        return CreateMaterializedTransitionTableRowShape(
            alias,
            rowShape,
            field => CreateStoredGeneratedRowAccess(rowShape, field));
    }

    private static TableRowShape CreateMaterializedTransitionTableRowShape(
        string alias,
        GeneratedRowShape rowShape,
        Func<FieldBinding, FieldAccessStrategy> createAccess,
        bool useTypedContextAccess = false)
    {
        return new TableRowShape(
            alias,
            FieldBindingRebinder.Rebind(rowShape.Fields, createAccess),
            useTypedContextAccess && rowShape.SupportsGeneratedFieldAccess
                ? CreateTypedStoredGeneratedRowContextBindings(rowShape)
                : rowShape.Contexts,
            rowShape.TypeName);
    }

    private static TableRowShape CreateTypedMaterializedTransitionTableRowShape(
        string alias,
        GeneratedRowShape rowShape)
    {
        return CreateMaterializedTransitionTableRowShape(
            alias,
            rowShape,
            field => rowShape.SupportsGeneratedFieldAccess ? field.AccessStrategy : new PositionalAccess(field.OutputIndex),
            useTypedContextAccess: true);
    }

    private static FieldAccessStrategy CreateStoredGeneratedRowAccess(
        GeneratedRowShape rowShape,
        FieldBinding field)
    {
        if (!rowShape.SupportsGeneratedFieldAccess)
            return new PositionalAccess(field.OutputIndex);

        return field.AccessStrategy is GeneratedFieldAccess generated
            ? new GeneratedRowTypeAccess(rowShape.TypeName, generated.FieldName)
            : new GeneratedRowTypeAccess(rowShape.TypeName, field.Name);
    }

    private static FieldAccessStrategy CreateTypedStoredGeneratedRowAccess(
        GeneratedRowShape rowShape,
        FieldBinding field)
    {
        if (!rowShape.SupportsGeneratedFieldAccess)
            return new PositionalAccess(field.OutputIndex);

        return field.AccessStrategy is GeneratedFieldAccess generated
            ? generated
            : new GeneratedFieldAccess(field.Name);
    }

}
