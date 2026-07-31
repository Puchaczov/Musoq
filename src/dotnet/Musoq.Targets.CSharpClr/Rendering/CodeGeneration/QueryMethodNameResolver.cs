using Musoq.Evaluator.Resources;

namespace Musoq.Targets.CSharpClr;

internal static class QueryMethodNameResolver
{
    public static string Resolve(RenderContext context, string queryIdentifier)
    {
        return context.ResultMode == QueryResultMode.Table && context.TableViaRowsResult == null
            ? ResolveTable(context, queryIdentifier)
            : ResolveRows(context, queryIdentifier);
    }

    public static string ResolveTable(RenderContext context, string queryIdentifier)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (TryResolveScopeMethodName(context, out var methodName))
            return methodName;

        return $"{ResolveDefaultBaseName(queryIdentifier)}_0";
    }

    public static string ResolveRows(RenderContext context, string queryIdentifier)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (TryResolveScopeMethodName(context, out var methodName))
            return methodName.StartsWith("ComputeTable_", StringComparison.Ordinal)
                ? "ComputeRows_" + methodName["ComputeTable_".Length..]
                : methodName;

        return $"{ResolveRowsBaseName(queryIdentifier)}_0";
    }

    public static string ResolveShapeRows(RenderContext context, string queryIdentifier)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (TryResolveScopeMethodName(context, out var methodName))
            return methodName.StartsWith("ComputeTable_", StringComparison.Ordinal)
                ? "ComputeShapeRows_" + methodName["ComputeTable_".Length..]
                : $"{methodName}_Shapes";

        return $"{ResolveShapeRowsBaseName(queryIdentifier)}_0";
    }

    public static string ResolveIndexed(RenderContext context, string queryIdentifier, int methodIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfNegative(methodIndex);

        var baseName = TryResolveScopeMethodName(context, out var scopeMethodName)
            ? scopeMethodName
            : ResolveDefaultBaseName(queryIdentifier);

        return $"{baseName}_{methodIndex}";
    }

    public static string ResolveProfiled(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        return $"{methodName}_Profiled";
    }

    private static bool TryResolveScopeMethodName(RenderContext context, out string methodName)
    {
        methodName = string.Empty;

        if (context.Scope == null || !context.Scope.ContainsAttribute(MetaAttributes.MethodName))
            return false;

        methodName = context.Scope[MetaAttributes.MethodName];
        return true;
    }

    private static string ResolveDefaultBaseName(string queryIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        return $"ComputeTable_{queryIdentifier}";
    }

    private static string ResolveRowsBaseName(string queryIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        return $"ComputeRows_{queryIdentifier}";
    }

    private static string ResolveShapeRowsBaseName(string queryIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        return $"ComputeShapeRows_{queryIdentifier}";
    }
}
