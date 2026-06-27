namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanUseScalarOffsetArguments(ExecutionComputeOffsetWindow offset)
    {
        return offset is { Offset: ExecutionLiteral { Value: int }, DefaultValue: ExecutionLiteral };
    }

    private static Type CreateWindowOffsetValueElementType(Type valueType)
    {
        return valueType == typeof(object)
            ? typeof(object)
            : valueType;
    }

}
