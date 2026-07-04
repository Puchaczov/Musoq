using Microsoft.CodeAnalysis.Emit;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the compilation stage output: the emit result and produced
/// assembly/debug payloads.
/// </summary>
internal sealed record CompilationBuildArtifacts(EmitResult EmitResult, byte[]? DllFile, byte[]? PdbFile);
