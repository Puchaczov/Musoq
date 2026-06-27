using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    /// <summary>
    ///     Gets method that fits name and types of arguments passed.
    /// </summary>
    /// <param name="name">Function name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="entityType">Type of entity.</param>
    /// <returns>Method that fits requirements.</returns>
    public MethodInfo GetMethod(string name, Type[] methodArgs, Type? entityType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw SchemaArgumentException.ForEmptyString(nameof(name), "resolving a method");

        if (methodArgs == null)
            throw SchemaArgumentException.ForNullArgument(nameof(methodArgs), "resolving a method");

        if (!TryGetAnnotatedMethod(name, methodArgs, entityType, out var index, out var actualMethodName) ||
            actualMethodName is null)
        {
            var availableSignatures = GetAvailableMethodSignatures(name);
            var providedTypes = methodArgs.Select(arg => arg?.Name ?? "null").ToArray();

            throw MethodResolutionException.ForUnresolvedMethod(name, providedTypes, availableSignatures);
        }

        return _methods[actualMethodName][index];
    }

    /// <summary>
    ///     Gets the registered method if exists.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="methodArgs">The types of arguments methods contains.</param>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="result">Method metadata of founded method.</param>
    /// <returns>True if method exists, otherwise false.</returns>
    public bool TryGetMethod(string name, Type[] methodArgs, Type? entityType, [NotNullWhen(true)] out MethodInfo? result)
    {
        if (!TryGetAnnotatedMethod(name, methodArgs, entityType, out var index, out var actualMethodName) ||
            actualMethodName is null)
        {
            result = null;
            return false;
        }

        result = _methods[actualMethodName][index];
        return true;
    }

    public bool TryGetAggregationMethod(string name, Type[] methodArgs, Type? entityType,
        [NotNullWhen(true)] out MethodInfo? result)
    {
        var argTypesArray = methodArgs as Type[] ?? CopyToArray(methodArgs);
        var cacheKey = new MethodResolutionKey(name, argTypesArray, entityType);

        if (!_aggregationMethodCache.TryGetValue(cacheKey, out var cached))
        {
            var found = TryGetMethodWithNormalization(name, out var index, out var actualMethodName,
                (string exactName, out int idx) =>
                    TryGetAnnotatedMethodByExactName(
                        exactName,
                        methodArgs,
                        entityType,
                        static method => method.GetCustomAttribute<AggregationMethodAttribute>() != null,
                        out idx));

            cached = (found, index, actualMethodName);
            _aggregationMethodCache.TryAdd(cacheKey, cached);
        }

        if (!cached.Found || cached.ActualName is null)
        {
            result = null;
            return false;
        }

        result = _methods[cached.ActualName][cached.Index];
        return true;
    }

    public bool TryGetAggregationMethod(
        string name,
        Type[] methodArgs,
        Type? entityType,
        Func<MethodInfo, bool> methodFilter,
        out MethodInfo? result)
    {
        if (!TryGetAnnotatedMethod(
                name,
                methodArgs,
                entityType,
                method => method.GetCustomAttribute<AggregationMethodAttribute>() != null &&
                          methodFilter(method),
                out var index,
                out var actualMethodName) ||
            actualMethodName is null)
        {
            result = null;
            return false;
        }

        result = _methods[actualMethodName][index];
        return true;
    }

    /// <summary>
    ///     Tries to match method as if it weren't annotated. Assume that method specified parameters explicitly.
    /// </summary>
    /// <param name="name">Function name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="result">Method metadata of founded method.</param>
    /// <returns>True if some method fits, else false.</returns>
    public bool TryGetRawMethod(string name, Type[] methodArgs, [NotNullWhen(true)] out MethodInfo? result)
    {
        if (!TryGetRawMethod(name, methodArgs, out var index, out var actualMethodName) ||
            actualMethodName is null)
        {
            result = null;
            return false;
        }

        result = _methods[actualMethodName][index];
        return true;
    }

    /// <summary>
    ///     Register new method.
    /// </summary>
    /// <param name="methodInfo">Method to register.</param>
    protected void RegisterMethod(MethodInfo methodInfo)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);
        RegisterMethod(methodInfo.Name, methodInfo);
    }
    private string[] GetAvailableMethodSignatures(string methodName)
    {
        if (!_methods.TryGetValue(methodName, out var methods))
            return [];

        return methods.Select(m =>
        {
            var parameters = GetCachedParameters(m);
            var paramTypes = parameters.Select(p => p.ParameterType.Name).ToArray();
            return $"{methodName}({string.Join(", ", paramTypes)})";
        }).ToArray();
    }

    /// <summary>
    ///     Gets all registered methods with their metadata.
    /// </summary>
    /// <returns>Dictionary of method names to their MethodInfo list.</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllMethods()
    {
        return _methods.ToDictionary(
            kvp => kvp.Key, IReadOnlyList<MethodInfo> (kvp) => kvp.Value.AsReadOnly()
        );
    }

    /// <summary>
    ///     Finds a window function factory method by its SQL-level name.
    ///     Checks <see cref="WindowFunctionAttribute.Name"/> first, then falls back to the method name.
    ///     Supports name normalization (case-insensitive, underscore-insensitive).
    /// </summary>
    public bool TryGetWindowFunction(string sqlName, [NotNullWhen(true)] out MethodInfo? result)
    {
        var normalizedSqlName = MethodNameNormalizer.Normalize(sqlName);

        foreach (var kvp in _methods)
        {
            foreach (var method in kvp.Value)
            {
                var attr = method.GetCustomAttribute<WindowFunctionAttribute>();
                if (attr is null)
                    continue;

                var effectiveName = attr.Name ?? method.Name;
                var normalizedEffectiveName = MethodNameNormalizer.Normalize(effectiveName);

                if (normalizedEffectiveName == normalizedSqlName)
                {
                    result = method;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }
}
