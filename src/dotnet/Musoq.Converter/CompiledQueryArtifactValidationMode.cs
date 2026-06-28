namespace Musoq.Converter;

/// <summary>
///     Describes how strictly an artifact is validated before loading its runnable type.
/// </summary>
public enum CompiledQueryArtifactValidationMode
{
    /// <summary>
    ///     Validate parser, semantic, planning, source shape, options, script, and runtime signatures without
    ///     rebuilding generated executable C#.
    /// </summary>
    Fast = 0,

    /// <summary>
    ///     Validate the fast checks and also rebuild generated executable C# to compare its hash.
    /// </summary>
    StrictGeneratedCodeHash = 1
}
