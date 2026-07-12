namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanUseScalarOffsetArguments(ExecutionComputeOffsetWindow offset)
    {
        return offset.Offset is ExecutionLiteral literal &&
               literal.Value.TryGetInt32(out _) &&
               offset.DefaultValue is ExecutionLiteral;
    }

    private static Type CreateWindowOffsetValueElementType(Type valueType)
    {
        return valueType == typeof(object)
            ? typeof(object)
            : valueType;
    }

}
