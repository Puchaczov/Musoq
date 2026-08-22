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
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);

        if (node.ResultOrderBy != null || node.ResultSkip != null || node.ResultTake != null)
        {
            var bindingVisitor = Visitor as BuildMetadataAndInferTypesVisitor
                                 ?? throw new InvalidOperationException(
                                     "Set result modifiers require the metadata and type inference visitor.");
            bindingVisitor.BeginSetResultModifierBinding();
            try
            {
                node.ResultOrderBy?.Accept(this);
                node.ResultSkip?.Accept(this);
                node.ResultTake?.Accept(this);
                bindingVisitor.AttachSetResultModifiers(node);
            }
            finally
            {
                bindingVisitor.EndSetResultModifierBinding();
            }
        }

        RestoreScope();
    }
}
