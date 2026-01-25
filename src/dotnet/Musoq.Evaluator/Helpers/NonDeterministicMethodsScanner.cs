#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Helpers;

/// <summary>
///     Scans assemblies for methods marked with specific attributes to support CSE optimization.
/// </summary>
public static class NonDeterministicMethodsScanner
{
    /// <summary>
    ///     Cache for scanned assembly results to avoid repeated reflection.
    ///     Key is the assembly's full name, value is the set of non-deterministic method names.
    /// </summary>
    private static readonly ConcurrentDictionary<string, HashSet<string>> AssemblyCache = new();

    /// <summary>
    ///     Scans the provided assemblies for methods marked with <see cref="NonDeterministicAttribute" />
    ///     and returns a set of their names.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for non-deterministic methods.</param>
    /// <returns>A case-insensitive hash set of method names that are non-deterministic.</returns>
    public static HashSet<string> ScanForNonDeterministicMethods(IEnumerable<Assembly>? assemblies)
    {
        var nonDeterministicMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (assemblies == null)
            return nonDeterministicMethods;

        foreach (var assembly in assemblies)
        {
            var assemblyName = assembly.FullName ?? assembly.GetName().Name ?? string.Empty;

            var cachedMethods = AssemblyCache.GetOrAdd(assemblyName, _ => ScanAssembly(assembly));

            foreach (var method in cachedMethods) nonDeterministicMethods.Add(method);
        }

        return nonDeterministicMethods;
    }

    private static HashSet<string> ScanAssembly(Assembly assembly)
    {
        var methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsPublic: false, IsNestedPublic: false })
                    continue;

                if (!typeof(LibraryBase).IsAssignableFrom(type))
                    continue;

                foreach (var method in type.GetMethods(
                             BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    var bindableAttr = method.GetCustomAttribute<BindableMethodAttribute>();
                    var nonDeterministicAttr = method.GetCustomAttribute<NonDeterministicAttribute>();

                    if (bindableAttr != null && nonDeterministicAttr != null) methods.Add(method.Name);
                }
            }
        }
        catch (ReflectionTypeLoadException)
        {
        }

        return methods;
    }

    /// <summary>
    ///     Clears the assembly scan cache. Useful for testing.
    /// </summary>
    public static void ClearCache()
    {
        AssemblyCache.Clear();
    }
}
