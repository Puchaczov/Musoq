using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    private void LoadQueryScope()
    {
        LoadScope("Query");
        Visitor.QueryBegins();
    }

    private void EndQueryScope()
    {
        Visitor.QueryEnds();
    }

    private void LoadScope(string name)
    {
        var newScope = Scope.AddScope(name);
        _scopes.Push(Scope);
        Scope = newScope;

        Visitor.SetScope(newScope);
    }

    private void RestoreScope()
    {
        Scope = _scopes.Pop();
        Visitor.SetScope(Scope);
    }

    private void TraverseSetOperatorWithScope(SetOperatorNode node)
    {
        VisitChildrenThenNode(node);
        RestoreScope();
    }
}
