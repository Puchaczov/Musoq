using System;
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
}