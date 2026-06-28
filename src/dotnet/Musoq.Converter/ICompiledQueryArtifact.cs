using System.Collections.Generic;

namespace Musoq.Converter;

/// <summary>
///     Portable runtime-v2 compiled query artifact.
///     Hosts own persistence, invalidation, eviction, encryption, compression, and cache policy.
/// </summary>
public interface ICompiledQueryArtifact
{
    /// <summary>
    ///     Gets the emitted query assembly bytes.
    /// </summary>
    byte[] AssemblyBytes { get; }

    /// <summary>
    ///     Gets optional portable PDB bytes.
    /// </summary>
    byte[]? SymbolsBytes { get; }

    /// <summary>
    ///     Gets the generated runnable type name.
    /// </summary>
    string RunnableTypeName { get; }

    /// <summary>
    ///     Gets the exact engine/runtime signature that produced the artifact.
    /// </summary>
    string EngineVersion { get; }

    /// <summary>
    ///     Gets the artifact format version.
    /// </summary>
    string ArtifactFormatVersion { get; }

    /// <summary>
    ///     Gets the signature of compilation options that affect the generated executable shape.
    /// </summary>
    string CompilationOptionsSignature { get; }

    /// <summary>
    ///     Gets engine metadata required to validate and reload the artifact.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
