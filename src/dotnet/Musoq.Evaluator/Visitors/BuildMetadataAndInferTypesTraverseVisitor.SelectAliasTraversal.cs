using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(IdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (Visitor is BuildMetadataAndInferTypesVisitor buildVisitor &&
            buildVisitor.TryGetSelectAliasExpressionForCurrentClause(node.Name, out var aliasExpression))
        {
            buildVisitor.EnterSelectAliasReference(node.Name);
            try
            {
                aliasExpression.Accept(this);
            }
            finally
            {
                buildVisitor.ExitSelectAliasReference(node.Name);
            }

            return;
        }

        base.Visit(node);
    }
}
