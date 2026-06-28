namespace Musoq.Converter;

/// <summary>
///     Controls validation performed when creating an executable query from a compiled artifact.
/// </summary>
public sealed class CompiledQueryArtifactLoadOptions
{
    /// <summary>
    ///     Gets the default artifact load options.
    /// </summary>
    public static CompiledQueryArtifactLoadOptions Default { get; } = new();

    /// <summary>
    ///     Gets the artifact validation mode. The default fast mode avoids regenerating executable code.
    /// </summary>
    public CompiledQueryArtifactValidationMode ValidationMode { get; init; } =
        CompiledQueryArtifactValidationMode.Fast;
}
