using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    private sealed class QueryRewriteContext(SelectNode select, ExpressionFromNode from)
    {
        public OrderByNode? OrderBy { get; init; }

        public QualifyNode? Qualify { get; init; }

        // ReSharper disable once PropertyCanBeMadeInitOnly.Local - join splitting can replace this after context creation.
        public WindowNode? Window { get; set; }

        public GroupByNode? GroupBy { get; init; }

        public SkipNode? Skip { get; init; }

        public TakeNode? Take { get; init; }

        public SelectNode Select { get; } = select;

        public WhereNode? Where { get; set; }

        public ExpressionFromNode From { get; } = from;

        public SelectNode ScoreSelect { get; set; } = select;

        public WhereNode? ScoreWhere { get; set; }

        public OrderByNode? ScoreOrderBy { get; set; }

        public List<Node> SplitNodes { get; } = [];

        public string Source { get; set; } = from.Alias.ToRowsSource().WithRowsUsage();

        public QueryNode? LastJoinQuery { get; set; }

        public IReadOnlyList<AccessMethodNode>? UsedRefreshMethods { get; set; }

        public int AliasIndex { get; set; }

        public AliasesPositionsSymbol AliasesPositionsSymbol { get; } = new();

        public bool PreserveDirectApplyChain { get; set; }
    }
}
