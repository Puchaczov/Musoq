namespace Musoq.Parser.Nodes;

/// <summary>
///     Carries the source name and location of an argument label.
/// </summary>
public readonly record struct ArgumentName(string Name, TextSpan Span)
{
    public bool HasName => !string.IsNullOrWhiteSpace(Name);
}
