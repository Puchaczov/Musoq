using System.Reflection;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private void RegisterMethod(string name, MethodInfo methodInfo)
    {
        int index;
        if (_methods.TryGetValue(name, out var method))
        {
            index = method.Count;
            method.Add(methodInfo);
        }
        else
        {
            index = 0;
            _methods.Add(name, [methodInfo]);
        }

        _methodIndexCache[(name, methodInfo)] = index;

        var normalizedName = MethodNameNormalizer.Normalize(name);
        if (normalizedName != name)
            _normalizedToOriginalMethodNames.TryAdd(normalizedName, name);
    }
}
