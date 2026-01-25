using System;
using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Schema.Managers;

public class MethodsAggregator(MethodsManager methodsManager)
{
    public bool TryResolveMethod(string name, Type[] types, Type entityType, out MethodInfo method)
    {
        return methodsManager.TryGetMethod(name, types, entityType, out method);
    }

    public bool TryResolveRawMethod(string name, Type[] types, out MethodInfo method)
    {
        return methodsManager.TryGetRawMethod(name, types, out method);
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
