namespace Musoq.Evaluator.IR.Execution;

internal static class StrictCastLibraryConversionFacts
{
    public static Type GetCastTargetType(Type returnType)
    {
        return Nullable.GetUnderlyingType(returnType) ?? returnType;
    }

    public static bool NeedsLibraryTarget(Type sourceReturnType, Type castReturnType)
    {
        return sourceReturnType != typeof(DBNull) &&
               !IsPassThrough(sourceReturnType, castReturnType) &&
               CanUseLibraryConversion(sourceReturnType, castReturnType);
    }

    public static bool IsPassThrough(Type sourceReturnType, Type castReturnType)
    {
        var sourceType = Nullable.GetUnderlyingType(sourceReturnType) ?? sourceReturnType;
        return sourceType == GetCastTargetType(castReturnType);
    }

    public static bool CanUseLibraryConversion(Type sourceReturnType, Type castReturnType)
    {
        var sourceType = Nullable.GetUnderlyingType(sourceReturnType) ?? sourceReturnType;
        var targetType = GetCastTargetType(castReturnType);

        if (sourceReturnType == typeof(DBNull) || IsPassThrough(sourceReturnType, castReturnType))
            return false;

        if (IsNumericOrBooleanTarget(targetType))
            return sourceType == typeof(string) ||
                   sourceType == typeof(object) ||
                   IsNumericOrBooleanSource(sourceType) ||
                   sourceType == typeof(char);

        if (targetType == typeof(string))
        {
            return sourceType == typeof(object) ||
                   sourceType == typeof(string) ||
                   sourceType == typeof(char) ||
                   sourceType == typeof(DateTime) ||
                   sourceType == typeof(DateTimeOffset) ||
                   sourceType == typeof(TimeSpan) ||
                   sourceType == typeof(Guid) ||
                   IsNumericOrBooleanSource(sourceType);
        }

        if (targetType == typeof(char))
        {
            return sourceType == typeof(string) ||
                   sourceType == typeof(object) ||
                   sourceType == typeof(char) ||
                   IsNumericOrBooleanSource(sourceType);
        }

        if (targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset))
        {
            return sourceType == typeof(string) ||
                   sourceType == typeof(object) ||
                   sourceType == typeof(DateTime) ||
                   sourceType == typeof(DateTimeOffset);
        }

        if (targetType == typeof(TimeSpan))
            return sourceType == typeof(string) || sourceType == typeof(object) || sourceType == typeof(TimeSpan);

        if (targetType == typeof(Guid))
            return sourceType == typeof(string) || sourceType == typeof(object) || sourceType == typeof(Guid);

        return false;
    }

    private static bool IsNumericOrBooleanTarget(Type type)
    {
        return IsNumericOrBooleanSource(type) || type == typeof(decimal);
    }

    private static bool IsNumericOrBooleanSource(Type type)
    {
        return type == typeof(bool) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }
}
