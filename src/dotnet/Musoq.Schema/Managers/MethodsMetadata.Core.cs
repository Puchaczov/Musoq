using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private readonly Dictionary<(string Name, MethodInfo Method), int> _methodIndexCache = new();

    private readonly Dictionary<string, List<MethodInfo>> _methods;
    private readonly Dictionary<string, string> _normalizedToOriginalMethodNames;

    private readonly ConcurrentDictionary<MethodInfo, ParameterMetadataInfo> _parameterMetadataCache = new();

    private readonly ConcurrentDictionary<MethodResolutionKey, (bool Found, int Index, string? ActualName)>
        _annotatedMethodCache = new();

    private readonly ConcurrentDictionary<MethodResolutionKey, (bool Found, int Index, string? ActualName)>
        _aggregationMethodCache = new();

    /// <summary>
    ///     Initialize object.
    /// </summary>
    public MethodsMetadata()
    {
        _methods = new Dictionary<string, List<MethodInfo>>();
        _normalizedToOriginalMethodNames = new Dictionary<string, string>();
    }

    private ParameterMetadataInfo GetCachedParameterMetadata(MethodInfo method)
    {
        return _parameterMetadataCache.GetOrAdd(method, static m =>
        {
            var parameters = m.GetParameters();
            return new ParameterMetadataInfo(parameters);
        });
    }

    private ParameterInfo[] GetCachedParameters(MethodInfo method)
    {
        return GetCachedParameterMetadata(method).Parameters;
    }

    private int GetCachedMethodIndex(string name, MethodInfo method)
    {
        return _methodIndexCache.TryGetValue((name, method), out var index)
            ? index
            : _methods[name].IndexOf(method);
    }
}
