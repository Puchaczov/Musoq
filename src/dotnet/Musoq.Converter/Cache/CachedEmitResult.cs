namespace Musoq.Converter.Cache;

/// <summary>
///     Immutable record holding the cached result of a Roslyn Compilation.Emit() call.
/// </summary>
/// <param name="DllFile">The emitted DLL bytes.</param>
/// <param name="PdbFile">The emitted PDB bytes.</param>
/// <param name="AccessToClassPath">
///     The fully qualified type name baked into the DLL (e.g., "Query.Compiled_42.CompiledQuery").
/// </param>
public sealed record CachedEmitResult(byte[] DllFile, byte[] PdbFile, string AccessToClassPath);
