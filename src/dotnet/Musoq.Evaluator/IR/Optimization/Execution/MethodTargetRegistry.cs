using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed class MethodTargetRegistry(string namePrefix, MethodTargetRegistry? parent)
{
    private readonly Dictionary<Type, ExecutionVariable> _variables = new();
    private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);
    private readonly Dictionary<MethodCacheKey, ExecutionVariable> _caches = new();
    private readonly List<ExecutionVariable> _createdTargets = [];

    public IReadOnlyList<ExecutionVariable> CreatedTargets => _createdTargets;
    public void AddExisting(ExecutionVariable variable)
    {
        _usedNames.Add(variable.Name);
        if (typeof(Plugins.LibraryBase).IsAssignableFrom(variable.Type.ResolveClrType()))
            _variables.TryAdd(variable.Type.ResolveClrType(), variable);
    }

    public void ForgetReusableTargets()
    {
        foreach (var variable in _variables.Values)
            _usedNames.Remove(variable.Name);
        _variables.Clear();
        _caches.Clear();
    }
    public ExecutionVariable? GetOrAdd(MethodInfo method, string? namePrefixOverride = null)
    {
        if (!ExecutionMethodTargetReuse.TryGetReusableTargetType(method, out var declaringType))
            return null;
        return GetOrAdd(declaringType, namePrefixOverride);
    }
    public ExecutionVariable? GetOrAdd(Type declaringType, string? namePrefixOverride = null)
    {
        if (declaringType.IsAbstract || !typeof(Plugins.LibraryBase).IsAssignableFrom(declaringType) || declaringType.GetConstructor(Type.EmptyTypes) == null)
            return null;
        if (_variables.TryGetValue(declaringType, out var variable))
            return variable;
        if (parent?.TryGetVariable(declaringType, out variable) == true)
            return variable;
        variable = new ExecutionVariable(
            CreateUniqueName($"__{ResolveNamePrefix(namePrefixOverride)}{declaringType.Name}{_variables.Count.ToString(CultureInfo.InvariantCulture)}"),
            declaringType);
        _variables.Add(declaringType, variable);
        _createdTargets.Add(variable);
        return variable;
    }
    public ExecutionVariable? GetOrAddCache(ExecutionMethodCall method, ExecutionVariable target)
    {
        var keyType = method.Arguments[0].ReturnType.ResolveClrType();
        var valueType = method.ReturnType.ResolveClrType();
        var cacheKey = new MethodCacheKey(method.Method.ResolveClrMethod(), keyType, valueType);
        if (_caches.TryGetValue(cacheKey, out var cache))
            return cache;
        var cacheType = typeof(ConcurrentDictionary<,>).MakeGenericType(keyType, valueType);
        cache = new ExecutionVariable(
            CreateUniqueName($"__{CreateCacheNamePrefix(target)}{method.Method.MethodName}Cache{_caches.Count.ToString(CultureInfo.InvariantCulture)}"),
            cacheType);
        _caches.Add(cacheKey, cache);
        return cache;
    }

    private string ResolveNamePrefix(string? namePrefixOverride)
    {
        return string.IsNullOrWhiteSpace(namePrefixOverride)
            ? namePrefix
            : namePrefixOverride;
    }

    private string CreateCacheNamePrefix(ExecutionVariable target)
    {
        const string generatedPrefix = "__";
        if (!target.Name.StartsWith(generatedPrefix, StringComparison.Ordinal))
            return namePrefix;
        var text = target.Name[generatedPrefix.Length..];
        if (string.IsNullOrWhiteSpace(text))
            return namePrefix;
        var length = 0;
        while (length < text.Length &&
               (length == 0 || !char.IsUpper(text[length])))
        {
            length++;
        }
        return length == 0 ? namePrefix : text[..length];
    }
    private bool TryGetVariable(Type declaringType, out ExecutionVariable variable)
    {
        if (_variables.TryGetValue(declaringType, out variable!))
            return true;
        if (parent != null && parent.TryGetVariable(declaringType, out variable))
            return true;
        variable = null!;
        return false;
    }
    private string CreateUniqueName(string candidate)
    {
        var baseName = ExecutionSymbolicNamePolicy.CreateLoweringIdentifierCandidate(candidate, 0);
        var name = baseName;
        var disambiguator = 1;
        while (!_usedNames.Add(name))
        {
            name = ExecutionSymbolicNamePolicy.CreateLoweringIdentifierCandidate(
                $"{baseName}_{disambiguator.ToString(CultureInfo.InvariantCulture)}",
                0);
            disambiguator++;
        }

        return name;
    }

    private sealed record MethodCacheKey(
        MethodInfo Method,
        Type KeyType,
        Type ValueType);
}

