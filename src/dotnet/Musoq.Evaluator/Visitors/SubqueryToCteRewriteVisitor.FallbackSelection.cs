using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private readonly Dictionary<Node, string> _fallbackReasons = new(ReferenceEqualityComparer.Instance);

    internal IReadOnlyDictionary<Node, string> FallbackReasons => _fallbackReasons;

    internal void RecordFallback(Node node, string reason) => _fallbackReasons[node] = reason;

    private QueryNode SelectSubqueryFallbacks(QueryNode query, SubqueryCorrelationAnalysis analysis) =>
        new SubqueryFallbackSelection(this, analysis).Select(query);

    internal string? ProbeSubquerySpecialization(Node node, SubqueryCorrelationAnalysis analysis)
    {
        var probe = Musoq.Evaluator.IR.Optimization.Logical.SubqueryToCteNormalizationPass.CreateSpecializationProbe();
        var definitions = new List<CteInnerExpressionNode>();
        try
        {
            switch (node)
            {
                case ScalarSubqueryNode scalar:
                    probe.PrepareScalarSubquery(scalar, analysis, definitions);
                    break;
                case InQueryNode membership:
                    probe.PreparePredicateApplySubquery(SubqueryInfo.CreateIn(membership, false), analysis, definitions);
                    break;
                case ExistsQueryNode exists:
                    probe.PreparePredicateApplySubquery(SubqueryInfo.CreateExists(exists, false), analysis, definitions);
                    break;
            }
            return null;
        }
        catch (VisitorException exception) when (SubqueryDiagnosticFactory.IsInvalidSubquery(exception))
        {
            return exception.Message;
        }
    }

}
