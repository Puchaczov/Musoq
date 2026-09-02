namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionColumnMetadataFields
{
    public static ExecutionColumnMetadataField FromFieldBinding(FieldBinding field)
    {
        return new ExecutionColumnMetadataField(
            field.Name,
            field.OutputIndex,
            field.ColumnType,
            field.ReadModifiers,
            field.SourceReadType,
            field.EnumType);
    }

    public static Type RequireClrTypeForLegacyCodeGeneration(ExecutionColumnMetadataField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Type.ResolveClrType();
    }
}
