using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(TranslatedSetTreeNode node)
    {
    }

    public override void Visit(TranslatedSetOperatorNode node)
    {
    }

    public override void Visit(UnionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "Union");
    }

    public override void Visit(UnionAllNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "UnionAll");
    }

    public override void Visit(ExceptNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "Except");
    }

    public override void Visit(IntersectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitSetOperationNode(node, "Intersect");
    }

    public override void Visit(PutTrueNode node)
    {
        Nodes.Push(new PutTrueNode());
    }

    public override void Visit(MultiStatementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var items = new Node[node.Nodes.Length];

        for (var i = node.Nodes.Length - 1; i >= 0; --i)
            items[i] = Nodes.Pop();

        Nodes.Push(new MultiStatementNode(items, node.ReturnType));
    }

    public override void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        var set = Nodes.Pop();

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)Nodes.Pop();

        Nodes.Push(new CteExpressionNode(sets, set));
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var set = Nodes.Pop();

        var collector = new GetSelectFieldsVisitor();
        var traverser = new GetSelectFieldsTraverseVisitor(collector);

        set.Accept(traverser);

        var table = new VariableTable(collector.CollectedFieldNames);
        var parentScope = _sourceBinding.CurrentScope.Parent ??
                          throw new VisitorException(
                              VisitorName,
                              "VisitCteInnerExpressionNode",
                              "CTE binding requires a parent scope.");

        parentScope.ScopeSymbolTable.AddSymbol(node.Name,
            new TableSymbol(node.Name, new TransitionSchema(node.Name, table), table, false));

        if (_compilationOptions.UsePrimitiveTypeValidation)
            foreach (var fieldInfo in collector.CollectedFieldNames)
                if (!BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(fieldInfo.ColumnType))
                {
                    var fieldNode = new FieldNode(new IntegerNode("0", "s"), fieldInfo.ColumnIndex,
                        fieldInfo.ColumnName);
                    if (TryReportInvalidExpressionType(fieldNode, fieldInfo.ColumnType, $"CTE '{node.Name}'",
                            fieldNode))
                        continue;
                    throw new InvalidQueryExpressionTypeException(
                        fieldNode,
                        fieldInfo.ColumnType,
                        $"CTE '{node.Name}'");
                }

        Nodes.Push(new CteInnerExpressionNode(set, node.Name));
    }
}
