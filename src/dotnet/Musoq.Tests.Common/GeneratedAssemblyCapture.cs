using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Musoq.Tests.Common;

/// <summary>
/// Captures actual assembly-load notifications during a compilation. Runtime metadata lookup is
/// limited to the exact generated type named by its build artifact; no private members are read.
/// </summary>
public sealed class GeneratedAssemblyCapture : IDisposable
{
    private readonly ConcurrentBag<Assembly> _assemblies = [];

    public GeneratedAssemblyCapture() => AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args) => _assemblies.Add(args.LoadedAssembly);

    public Type RequireType(string fullName)
    {
        var types = _assemblies.Select(assembly => assembly.GetType(fullName, throwOnError: false))
            .OfType<Type>().Distinct().ToArray();
        if (types.Length != 1)
            throw new InvalidOperationException($"Expected one loaded generated type '{fullName}', found {types.Length}.");
        return types[0];
    }

    public void Dispose() => AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
}
