using Musoq.Parser;

namespace Musoq.Parser.Nodes.InterpretationSchema;

/// <summary>
///     A comment retained as part of an interpretation schema's metadata.
/// </summary>
/// <param name="Text">The original comment text, including its comment delimiters.</param>
/// <param name="Span">The source span occupied by the comment.</param>
public sealed record SchemaComment(string Text, TextSpan Span);
