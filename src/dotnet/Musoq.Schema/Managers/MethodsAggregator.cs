using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Musoq.Schema.Managers;

public class MethodsAggregator(MethodsManager methodsManager)
{
    public bool TryResolveMethod(string name, Type[] types, Type? entityType, [NotNullWhen(true)] out MethodInfo? method)
    {
        return methodsManager.TryGetMethod(name, types, entityType, out method);
    }

    public bool TryResolveAggregationMethod(string name, Type[] types, Type? entityType, [NotNullWhen(true)] out MethodInfo? method)
    {
        return methodsManager.TryGetAggregationMethod(name, types, entityType, out method);
    }

    public bool TryResolveAggregationMethod(
        string name,
        Type[] types,
        Type? entityType,
        Func<MethodInfo, bool> methodFilter,
        [NotNullWhen(true)] out MethodInfo? method)
    {
        return methodsManager.TryGetAggregationMethod(name, types, entityType, methodFilter, out method);
    }

    public bool TryResolveRawMethod(string name, Type[] types, [NotNullWhen(true)] out MethodInfo? method)
    {
        return methodsManager.TryGetRawMethod(name, types, out method);
    }

    public bool TryResolveWindowFunction(string sqlName, [NotNullWhen(true)] out MethodInfo? method)
    {
        return methodsManager.TryGetWindowFunction(sqlName, out method);
    }

    /// <summary>
    ///     Gets all registered methods with their metadata.
    /// </summary>
    /// <returns>Dictionary of method names to their MethodInfo list.</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllMethods()
    {
        return methodsManager.GetAllMethods();
    }
}
