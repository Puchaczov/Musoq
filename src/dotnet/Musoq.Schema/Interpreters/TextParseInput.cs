namespace Musoq.Schema.Interpreters;

/// <summary>Borrowed input for one generated text parse, including its nested readers.</summary>
public ref struct TextParseInput(ReadOnlySpan<char> text)
{
    private string? _captureText;

    internal TextParseInput(string? text, bool suppressFailureDetails) : this(text.AsSpan())
    {
        _captureText = text;
        SuppressFailureDetails = suppressFailureDetails;
    }

    /// <summary>Gets the original input while it remains valid for this parse.</summary>
    public ReadOnlySpan<char> Text { get; } = text;

    internal string CaptureText => _captureText ??= Text.ToString();
    internal bool SuppressFailureDetails { get; }
}
