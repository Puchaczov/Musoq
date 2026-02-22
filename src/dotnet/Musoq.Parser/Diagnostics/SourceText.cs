using System;
using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents source text with efficient line/column lookup capabilities.
/// </summary>
public sealed class SourceText
{
    private readonly int[] _lineStarts;

    /// <summary>
    ///     Creates a new SourceText instance.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="filePath">Optional file path.</param>
    public SourceText(string text, string? filePath = null)
    {
        Text = text ?? string.Empty;
        FilePath = filePath;
        _lineStarts = ComputeLineStarts(Text);
    }

    /// <summary>
    ///     Gets the source text content.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Gets the optional file path.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    ///     Gets the total length of the text.
    /// </summary>
    public int Length => Text.Length;

    /// <summary>
    ///     Gets the total number of lines.
    /// </summary>
    public int LineCount => _lineStarts.Length;

    /// <summary>
    ///     Gets the character at the specified index.
    /// </summary>
    public char this[int index] => Text[index];

    private static int[] ComputeLineStarts(string text)
    {
        var lineStarts = new List<int> { 0 };

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\n')
            {
                lineStarts.Add(i + 1);
            }
            else if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                lineStarts.Add(i + 1);
            }
        }

        return lineStarts.ToArray();
    }

    /// <summary>
    ///     Gets the source location for the given offset.
    /// </summary>
    /// <param name="offset">The byte offset (0-based).</param>
    /// <returns>The source location with line and column information.</returns>
    public SourceLocation GetLocation(int offset)
    {
        if (offset < 0)
            return SourceLocation.None;

        if (offset > Text.Length)
            offset = Text.Length;

        var lineIndex = GetLineIndex(offset);
        var lineStart = _lineStarts[lineIndex];
        var column = offset - lineStart + 1;
        var line = lineIndex + 1;

        return new SourceLocation(offset, line, column, FilePath);
    }

    private int GetLineIndex(int offset)
    {
        var low = 0;
        var high = _lineStarts.Length - 1;

        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var lineStart = _lineStarts[mid];

            if (lineStart == offset)
                return mid;

            if (lineStart < offset)
            {
                if (mid + 1 >= _lineStarts.Length || _lineStarts[mid + 1] > offset)
                    return mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return Math.Max(0, low - 1);
    }

    /// <summary>
    ///     Gets the start and end locations for a text span.
    /// </summary>
    public (SourceLocation Start, SourceLocation End) GetLocations(TextSpan span)
    {
        var start = GetLocation(span.Start);
        var end = GetLocation(span.End);
        return (start, end);
    }

    /// <summary>
    ///     Gets the text content of the specified line (1-based).
    /// </summary>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The text of the line (without line ending).</returns>
    public string GetLineText(int line)
    {
        if (line < 1 || line > _lineStarts.Length)
            return string.Empty;

        var lineIndex = line - 1;
        var start = _lineStarts[lineIndex];

        int end;
        if (lineIndex + 1 < _lineStarts.Length)
        {
            end = _lineStarts[lineIndex + 1];

            while (end > start && (Text[end - 1] == '\r' || Text[end - 1] == '\n'))
                end--;
        }
        else
        {
            end = Text.Length;
        }

        return Text[start..end];
    }

    /// <summary>
    ///     Gets the span of the specified line (1-based), including line ending.
    /// </summary>
    public TextSpan GetLineSpan(int line)
    {
        if (line < 1 || line > _lineStarts.Length)
            return TextSpan.Empty;

        var lineIndex = line - 1;
        var start = _lineStarts[lineIndex];

        int end;
        if (lineIndex + 1 < _lineStarts.Length)
            end = _lineStarts[lineIndex + 1];
        else
            end = Text.Length;

        return new TextSpan(start, end - start);
    }

    /// <summary>
    ///     Gets a substring from the source text.
    /// </summary>
    public string GetText(TextSpan span)
    {
        if (span.Start < 0 || span.Start >= Text.Length)
            return string.Empty;

        var end = Math.Min(span.End, Text.Length);
        return Text[span.Start..end];
    }

    /// <summary>
    ///     Gets a substring from the source text.
    /// </summary>
    public string GetText(int start, int length)
    {
        return GetText(new TextSpan(start, length));
    }

    /// <summary>
    ///     Creates a context snippet showing the error location with a pointer.
    /// </summary>
    /// <param name="span">The span to highlight.</param>
    /// <param name="maxContextLines">Maximum number of context lines before and after.</param>
    /// <returns>A formatted string showing the error context.</returns>
    public string GetContextSnippet(TextSpan span, int maxContextLines = 2)
    {
        var location = GetLocation(span.Start);
        if (!location.IsValid)
            return string.Empty;

        var lines = new List<string>();
        var startLine = Math.Max(1, location.Line - maxContextLines);
        var endLine = Math.Min(LineCount, location.Line + maxContextLines);


        var lineNumWidth = endLine.ToString().Length;

        for (var line = startLine; line <= endLine; line++)
        {
            var lineText = GetLineText(line);
            var lineNum = line.ToString().PadLeft(lineNumWidth);

            if (line == location.Line)
            {
                lines.Add($"  {lineNum} | {lineText}");


                var pointerPadding = new string(' ', lineNumWidth + 3 + location.Column - 1);
                var pointerLength = Math.Max(1, Math.Min(span.Length, lineText.Length - location.Column + 1));
                var pointer = new string('^', pointerLength);
                lines.Add($"{pointerPadding}{pointer}");
            }
            else
            {
                lines.Add($"  {lineNum} | {lineText}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    ///     Returns the full source text.
    /// </summary>
    public override string ToString()
    {
        return Text;
    }
}
