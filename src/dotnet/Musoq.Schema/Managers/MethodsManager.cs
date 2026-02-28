using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Managers;

public class MethodsManager : MethodsMetadata
{
    private static readonly ConcurrentDictionary<Type, MethodInfo[]> BindableMethodsCache = new();

    public void RegisterLibraries(LibraryBase library)
    {
        var type = library.GetType();

        var methods = BindableMethodsCache.GetOrAdd(type, static t =>
        {
            var methods = new List<MethodInfo>();
            var currentType = t;

            while (currentType != null && typeof(LibraryBase).IsAssignableFrom(currentType))
            {
                methods.AddRange(currentType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                    .Where(f => f.GetCustomAttribute<BindableMethodAttribute>() != null));

                currentType = currentType.BaseType;
            }

            return methods.ToArray();
        });

        foreach (var methodInfo in methods)
            RegisterMethod(methodInfo);
    }
}
