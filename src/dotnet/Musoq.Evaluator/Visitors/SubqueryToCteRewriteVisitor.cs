using System.Collections.Generic;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;
public partial class SubqueryToCteRewriteVisitor : CloneQueryVisitor
{
    private readonly List<CorrelatedSubqueryRewriteRequest> _rewriteRequests = [];
    private readonly HashSet<Node> _plannedSubqueries = new(ReferenceEqualityComparer.Instance);

    internal const string SubqueryAliasPrefix = GeneratedSubqueryContract.SubqueryPrefix;

    internal IReadOnlyList<CorrelatedSubqueryRewriteRequest> RewriteRequests => _rewriteRequests;

    protected override string VisitorName => nameof(SubqueryToCteRewriteVisitor);

    private void RegisterRewriteRequests(IEnumerable<CorrelatedSubqueryRewriteRequest> requests)
    {
        foreach (var request in requests)
            if (_plannedSubqueries.Add(request.Node))
                _rewriteRequests.Add(request);
    }
}
