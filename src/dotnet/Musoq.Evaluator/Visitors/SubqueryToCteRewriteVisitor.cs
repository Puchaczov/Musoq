using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.Visitors;
public partial class SubqueryToCteRewriteVisitor : CloneQueryVisitor
{
    internal const string SubqueryAliasPrefix = GeneratedSubqueryContract.SubqueryPrefix;

    protected override string VisitorName => nameof(SubqueryToCteRewriteVisitor);
}
