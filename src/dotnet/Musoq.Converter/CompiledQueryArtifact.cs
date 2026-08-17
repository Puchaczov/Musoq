using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Converter;

/// <summary>
///     Engine-owned immutable implementation of <see cref="ICompiledQueryArtifact" />.
/// </summary>
public sealed class CompiledQueryArtifact : ICompiledQueryArtifact
{
    public const string CurrentArtifactFormatVersion = "2";

    private readonly byte[] _assemblyBytes;
    private readonly byte[]? _symbolsBytes;
    private readonly IReadOnlyDictionary<string, string> _metadata;

    public CompiledQueryArtifact(
        byte[] assemblyBytes,
        byte[]? symbolsBytes,
        string runnableTypeName,
        string engineVersion,
        string artifactFormatVersion,
        string compilationOptionsSignature,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(assemblyBytes);
        ArgumentNullException.ThrowIfNull(metadata);

        if (assemblyBytes.Length == 0)
            throw new ArgumentException("Assembly bytes cannot be empty.", nameof(assemblyBytes));

        _assemblyBytes = CopyBytes(assemblyBytes);
        _symbolsBytes = CopyBytesOrNull(symbolsBytes);
        RunnableTypeName = RequireText(runnableTypeName, nameof(runnableTypeName));
        EngineVersion = RequireText(engineVersion, nameof(engineVersion));
        ArtifactFormatVersion = RequireText(artifactFormatVersion, nameof(artifactFormatVersion));
        CompilationOptionsSignature = RequireText(compilationOptionsSignature, nameof(compilationOptionsSignature));
        _metadata = CopyMetadata(metadata);
    }

    public byte[] AssemblyBytes => CopyBytes(_assemblyBytes);

    public byte[]? SymbolsBytes => CopyBytesOrNull(_symbolsBytes);

    public string RunnableTypeName { get; }

    public string EngineVersion { get; }

    public string ArtifactFormatVersion { get; }

    public string CompilationOptionsSignature { get; }

    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    internal byte[] AssemblyBytesUnsafe => _assemblyBytes;

    internal byte[]? SymbolsBytesUnsafe => _symbolsBytes;

    private static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;
    }

    private static byte[] CopyBytes(byte[] bytes)
    {
        return bytes.ToArray();
    }

    private static byte[]? CopyBytesOrNull(byte[]? bytes)
    {
        return bytes?.ToArray();
    }

    private static IReadOnlyDictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        var copy = new Dictionary<string, string>(metadata.Count, StringComparer.Ordinal);
        foreach (var entry in metadata)
        {
            if (entry.Key == null)
                throw new ArgumentException("Metadata keys cannot be null.", nameof(metadata));
            if (entry.Value == null)
                throw new ArgumentException("Metadata values cannot be null.", nameof(metadata));

            copy[entry.Key] = entry.Value;
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}
