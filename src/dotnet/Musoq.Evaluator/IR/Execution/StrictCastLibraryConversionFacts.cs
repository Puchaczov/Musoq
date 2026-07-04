namespace Musoq.Evaluator.IR.Execution;

internal static class StrictCastLibraryConversionFacts
{
    public static Type GetCastTargetType(Type returnType)
    {
        return Nullable.GetUnderlyingType(returnType) ?? returnType;
    }

    public static bool IsPassThrough(Type sourceReturnType, Type castReturnType)
    {
        var sourceType = Nullable.GetUnderlyingType(sourceReturnType) ?? sourceReturnType;
        return sourceType == GetCastTargetType(castReturnType);
    }
}
