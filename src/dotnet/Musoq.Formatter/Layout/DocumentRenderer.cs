using System.Collections.Generic;
using System.Text;

namespace Musoq.Formatter.Layout;

internal sealed class DocumentRenderer(FormattingOptions options, string newline)
{
    private readonly StringBuilder _output = new();
    private readonly Stack<Frame> _pending = new();
    private int _column;
    private int _protectedEnd;

    internal string Render(Document document)
    {
        _pending.Push(new Frame(document, 0, LayoutMode.Expanded));
        while (_pending.TryPop(out var frame))
            Visit(frame);
        if (_output.Length == 0)
            return string.Empty;
        NewLine(BreakKind.Line);
        return _output.ToString();
    }

    private void Visit(Frame frame)
    {
        switch (frame.Document)
        {
            case TextDocument text:
                Write(text.Value, frame.Indent, text.Kind);
                break;
            case BreakDocument line:
                if (frame.Mode == LayoutMode.Flat && line.Kind == BreakKind.Optional)
                    Write(line.Inline, frame.Indent, TextKind.Generated);
                else
                    NewLine(line.Kind);
                break;
            case IndentedDocument indented:
                _pending.Push(frame with { Document = indented.Content, Indent = frame.Indent + options.IndentSize });
                break;
            case GroupedDocument grouped:
                var flat = grouped.FlatWidth != int.MaxValue && (long)_column + grouped.FlatWidth + SuffixWidth() <= options.LineWidth;
                _pending.Push(frame with { Document = grouped.Content, Mode = flat ? LayoutMode.Flat : LayoutMode.Expanded });
                break;
            case SequenceDocument sequence:
                for (var index = sequence.Parts.Count - 1; index >= 0; index--)
                    _pending.Push(frame with { Document = sequence.Parts[index] });
                break;
        }
    }

    private int SuffixWidth()
    {
        long width = 0;
        foreach (var frame in _pending)
        {
            if (frame.Document is BreakDocument or GroupedDocument)
                break;
            if (frame.Document.FlatWidth == int.MaxValue)
                break;
            width += frame.Document.FlatWidth;
            if (width >= options.LineWidth)
                return options.LineWidth;
        }
        return (int)width;
    }

    private void Write(string text, int indentation, TextKind kind)
    {
        if (text.Length == 0)
            return;
        if (_column == 0 && text != " ")
        {
            _output.Append(' ', indentation);
            _column = indentation;
        }
        _output.Append(text);
        var lastBreak = Math.Max(text.LastIndexOf('\n'), text.LastIndexOf('\r'));
        _column = lastBreak < 0 ? _column + text.Length : text.Length - lastBreak - 1;
        if (kind == TextKind.Source)
            _protectedEnd = _output.Length;
    }

    private void NewLine(BreakKind kind)
    {
        while (_output.Length > _protectedEnd && _output.Length > 0 && _output[^1] == ' ')
            _output.Length--;
        if (_output.Length == 0)
        {
            _column = 0;
            return;
        }
        if (_output[^1] is not ('\n' or '\r'))
            _output.Append(newline);
        if (kind == BreakKind.BlankLine && !HasBlankLine())
            _output.Append(newline);
        _column = 0;
    }

    private bool HasBlankLine()
    {
        var suffix = newline + newline;
        if (_output.Length < suffix.Length)
            return false;
        for (var index = 0; index < suffix.Length; index++)
        {
            if (_output[_output.Length - suffix.Length + index] != suffix[index])
                return false;
        }
        return true;
    }

    private enum LayoutMode { Expanded, Flat }

    private readonly record struct Frame(Document Document, int Indent, LayoutMode Mode);
}
