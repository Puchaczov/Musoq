using System;

namespace Musoq.Parser.Nodes;

public class WindowFrameNode : Node
{
    public string FrameType { get; } // "ROWS" or "RANGE"
    public string StartBound { get; } // "UNBOUNDED PRECEDING", "CURRENT ROW", etc.
    public string EndBound { get; } // "UNBOUNDED FOLLOWING", "CURRENT ROW", etc.

    public WindowFrameNode(string frameType, string startBound, string endBound = null)
    {
        FrameType = frameType;
        StartBound = startBound;
        EndBound = endBound ?? "CURRENT ROW"; // Default end bound
        Id = $"{nameof(WindowFrameNode)}{GetHashCode()}";
    }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override string ToString()
    {
        if (EndBound == "CURRENT ROW" && StartBound != "CURRENT ROW")
            return $"{FrameType} BETWEEN {StartBound} AND {EndBound}";
        return $"{FrameType} {StartBound}";
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}