using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

/// <summary>
///     Legacy static compatibility facade. New compilation paths should own an
///     <see cref="EvaluatorRuntimeEnvironment"/> explicitly.
/// </summary>
public static class RuntimeLibraries
{
    private static readonly object Gate = new();
    private static EvaluatorRuntimeEnvironment? _environment = new();

    internal static EvaluatorRuntimeEnvironment Environment => GetEnvironment();

    internal static IMetadataReferenceCache MetadataReferences => Environment.MetadataReferenceCache;

    internal static IRuntimeReferenceProvider Default => Environment.ReferenceProvider;

    public static MetadataReference[] References => WithEnvironment(static environment => environment.References);

    public static void CreateReferences()
    {
        WithEnvironment(static environment => environment.CreateReferences());
    }

    /// <summary>
    ///     Disposes the current legacy default environment. Static compatibility access throws
    ///     until <see cref="ResetDefaultEnvironment"/> creates a replacement.
    /// </summary>
    public static void DisposeDefaultEnvironment()
    {
        EvaluatorRuntimeEnvironment? environment;
        lock (Gate)
        {
            environment = _environment;
            _environment = null;
        }

        environment?.Dispose();
    }

    /// <summary>
    ///     Replaces and disposes the legacy default environment.
    /// </summary>
    public static void ResetDefaultEnvironment()
    {
        var replacement = new EvaluatorRuntimeEnvironment();
        EvaluatorRuntimeEnvironment? previous;
        lock (Gate)
        {
            previous = _environment;
            _environment = replacement;
        }

        previous?.Dispose();
    }

    internal static T WithEnvironment<T>(Func<EvaluatorRuntimeEnvironment, T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (Gate)
        {
            var environment = _environment ?? throw new ObjectDisposedException(nameof(RuntimeLibraries));
            return operation(environment);
        }
    }

    internal static void WithEnvironment(Action<EvaluatorRuntimeEnvironment> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (Gate)
        {
            var environment = _environment ?? throw new ObjectDisposedException(nameof(RuntimeLibraries));
            operation(environment);
        }
    }

    private static EvaluatorRuntimeEnvironment GetEnvironment()
    {
        lock (Gate)
        {
            return _environment ?? throw new ObjectDisposedException(nameof(RuntimeLibraries));
        }
    }
}
