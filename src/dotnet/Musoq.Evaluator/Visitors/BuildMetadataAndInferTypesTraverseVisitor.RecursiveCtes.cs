using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        LoadScope("CTE");
        var recursiveDefinitions = new RecursiveCteShapeAnalyzer().AnalyzeBoundSyntax(node);

        foreach (var inner in node.InnerExpression)
        {
            if (recursiveDefinitions.TryGetValue(inner.Name, out var descriptor))
                TraverseRecursiveCteDefinition(inner, descriptor);
            else
                inner.Accept(this);
        }

        node.OuterExpression.Accept(this);
        node.Accept(Visitor);
        RestoreScope();
    }

    private void TraverseRecursiveCteDefinition(
        CteInnerExpressionNode node,
        RecursiveCteShapeDescriptor descriptor)
    {
        if (Visitor is not BuildMetadataAndInferTypesVisitor metadataVisitor)
            throw new InvalidOperationException("Recursive CTE binding requires the metadata inference visitor.");

        LoadScope("CTE Inner Expression");
        Visitor.InnerCteBegins();
        LoadScope(descriptor.Boundary is UnionAllNode ? "UnionAll" : "Union");

        descriptor.Anchor.Accept(this);
        metadataVisitor.PrepareRecursiveCteAnchor(node);
        descriptor.RecursiveMember.Accept(this);
        metadataVisitor.VisitRecursiveCteBoundary(node.Name, descriptor.Boundary);

        RestoreScope();
        Visitor.InnerCteEnds();
        node.Accept(Visitor);
        RestoreScope();
    }
}
