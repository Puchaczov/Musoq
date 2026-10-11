using System.Reflection;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    protected void ClearRegistrations()
    {
        if (_sharedRegistrations)
        {
            _methods = new();
            _methodIndexCache = new();
            _normalizedToOriginalMethodNames = new();
            _sharedRegistrations = false;
        }
        else
        {
            _methods.Clear();
            _methodIndexCache.Clear();
            _normalizedToOriginalMethodNames.Clear();
        }
        _annotatedMethodCache.Clear();
        _aggregationMethodCache.Clear();
        _functions = default;
    }

    protected void RegisterMethod(string name, MethodInfo methodInfo)
    {
        RegisterMethodCore(name, methodInfo);
        _annotatedMethodCache.Clear();
        _aggregationMethodCache.Clear();
    }

    protected void RegisterMethodCore(string name, MethodInfo methodInfo)
    {
        EnsureWritableRegistrations();
        int index;
        if (_methods.TryGetValue(name, out var method))
        {
            if (method.Contains(methodInfo))
                return;
            index = method.Count;
            method.Add(methodInfo);
        }
        else
        {
            index = 0;
            _methods.Add(name, [methodInfo]);
        }

        _methodIndexCache[(name, methodInfo)] = index;
        _functions = default;

        var normalizedName = MethodNameNormalizer.Normalize(name);
        if (normalizedName != name)
            _normalizedToOriginalMethodNames.TryAdd(normalizedName, name);
    }
}
