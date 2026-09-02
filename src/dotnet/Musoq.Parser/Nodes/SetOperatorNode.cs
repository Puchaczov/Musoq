using Musoq.Parser.Tokens;

using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes;

public abstract class SetOperatorNode : BinaryNode
{
    protected SetOperatorNode(TokenType type, string[] keys, Node left, Node right, bool isNested,
        bool isTheLastOne)
        : this(type, keys, left, right, isNested, isTheLastOne, null, null, null)
    {
    }

    protected SetOperatorNode(
        TokenType type,
        string[] keys,
        Node left,
        Node right,
        bool isNested,
        bool isTheLastOne,
        OrderByNode? resultOrderBy,
        SkipNode? resultSkip,
        TakeNode? resultTake)
        : base(left, right)
    {
        _ = type;

        Keys = keys;
        IsNested = isNested;
        IsTheLastOne = isTheLastOne;
        ResultOrderBy = resultOrderBy;
        ResultSkip = resultSkip;
        ResultTake = resultTake;
        Id = $"{CalculateId(this)}{resultOrderBy?.Id}{resultSkip?.Id}{resultTake?.Id}";

        var last = (Node?)resultTake ?? resultSkip ?? resultOrderBy ?? right;
        if (left.HasSpan && last.HasSpan)
        {
            Span = ComputeSpan(left, last);
            FullSpan = Span;
        }
    }

    public string[] Keys { get; }

    /// <summary>
    ///     Gets the source spans of explicit set-operator keys in the same order as <see cref="Keys" />.
    ///     The collection is empty when the key list was omitted or explicitly empty.
    /// </summary>
    public IReadOnlyList<TextSpan> KeySpans { get; init; } = [];

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public string ResultTableName { get; protected set; } = string.Empty;

    public OrderByNode? ResultOrderBy { get; }

    public SkipNode? ResultSkip { get; }

    public TakeNode? ResultTake { get; }

    public bool IsNested { get; }

    public bool IsTheLastOne { get; }

    protected string FormatResultModifiers()
    {
        var modifiers = new Node?[] { ResultOrderBy, ResultSkip, ResultTake };
        return string.Concat(modifiers
            .Where(static modifier => modifier != null)
            .Select(static modifier => $" {modifier!.ToString()}"));
    }
}
