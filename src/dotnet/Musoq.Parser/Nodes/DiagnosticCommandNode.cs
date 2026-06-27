namespace Musoq.Parser.Nodes;

public sealed class DiagnosticCommandNode : Node
{
    public DiagnosticCommandNode(
        DiagnosticCommandKind kind,
        int commandStart,
        int innerStart,
        int innerEnd,
        string innerQueryText,
        TextSpan span)
    {
        Kind = kind;
        CommandStart = commandStart;
        InnerStart = innerStart;
        InnerEnd = innerEnd;
        InnerQueryText = innerQueryText;
        Span = span;
        FullSpan = span;
    }

    public DiagnosticCommandKind Kind { get; }

    public int CommandStart { get; }

    public int InnerStart { get; }

    public int InnerEnd { get; }

    public string InnerQueryText { get; }

    public override Type ReturnType { get; } = typeof(void);

    public override string Id { get; } = $"{nameof(DiagnosticCommandNode)}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit((Node)this);
    }

    public override string ToString()
    {
        var command = Kind == DiagnosticCommandKind.ExplainAnalyze
            ? "EXPLAIN ANALYZE"
            : "PROFILE";

        return $"{command} {InnerQueryText}";
    }
}
