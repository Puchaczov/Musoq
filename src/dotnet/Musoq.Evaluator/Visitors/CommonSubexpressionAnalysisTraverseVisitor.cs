using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Traverses the query tree to visit all expressions for CSE analysis.
///     This traverser visits expressions in all relevant clauses (SELECT, WHERE, HAVING, ORDER BY, etc.)
///     to count occurrences for potential caching.
/// </summary>
public class CommonSubexpressionAnalysisTraverseVisitor(CommonSubexpressionAnalysisVisitor visitor)
    : RawTraverseVisitor<CommonSubexpressionAnalysisVisitor>(visitor)
{
    public override void Visit(Node node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(GroupSelectNode node)
    {
        foreach (var field in node.Fields) field.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(GroupByNode node)
    {
        Visitor.InSeparateScopeContext = true;
        foreach (var field in node.Fields) field.Accept(this);
        Visitor.InSeparateScopeContext = false;
        node.Accept(Visitor);
    }

    public override void Visit(HavingNode node)
    {
        Visitor.InSeparateScopeContext = true;
        node.Expression.Accept(this);
        Visitor.InSeparateScopeContext = false;
        node.Accept(Visitor);
    }

    public override void Visit(OrderByNode node)
    {
        Visitor.InSeparateScopeContext = true;
        foreach (var field in node.Fields)
            field.Accept(this);
        Visitor.InSeparateScopeContext = false;

        node.Accept(Visitor);
    }

    public override void Visit(CaseNode node)
    {
        Visitor.InPassThroughUnsafeContext = true;
        foreach (var whenNode in node.WhenThenPairs)
        {
            whenNode.When.Accept(this);
            whenNode.Then.Accept(this);
        }

        node.Else?.Accept(this);
        Visitor.InPassThroughUnsafeContext = false;

        node.Accept(Visitor);
    }

    public override void Visit(SchemaFromNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(AliasedFromNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        node.First.Accept(this);
        node.Second.Accept(this);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(AccessMethodFromNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(CreateTransformationTableNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(TranslatedSetTreeNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(TranslatedSetOperatorNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(QueryNode node)
    {
        node.From?.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);
        node.GroupBy?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Skip?.Accept(this);
        node.Take?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(InternalQueryNode node)
    {
        node.From?.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);
        node.GroupBy?.Accept(this);
        node.Skip?.Accept(this);
        node.Take?.Accept(this);
        node.Refresh?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(ApplyNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(RefreshNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(InterpretCallNode node)
    {
    }

    public override void Visit(ParseCallNode node)
    {
    }

    public override void Visit(InterpretAtCallNode node)
    {
    }

    public override void Visit(TryInterpretCallNode node)
    {
    }

    public override void Visit(TryParseCallNode node)
    {
    }

    public override void Visit(PartialInterpretCallNode node)
    {
    }

    public override void Visit(BinarySchemaNode node)
    {
    }

    public override void Visit(TextSchemaNode node)
    {
    }

    public override void Visit(FieldDefinitionNode node)
    {
    }

    public override void Visit(ComputedFieldNode node)
    {
    }

    public override void Visit(TextFieldDefinitionNode node)
    {
    }

    public override void Visit(FieldConstraintNode node)
    {
    }

    public override void Visit(PrimitiveTypeNode node)
    {
    }

    public override void Visit(ByteArrayTypeNode node)
    {
    }

    public override void Visit(StringTypeNode node)
    {
    }

    public override void Visit(SchemaReferenceTypeNode node)
    {
    }

    public override void Visit(ArrayTypeNode node)
    {
    }

    public override void Visit(BitsTypeNode node)
    {
    }

    public override void Visit(AlignmentNode node)
    {
    }

    public override void Visit(RepeatUntilTypeNode node)
    {
    }

    public override void Visit(InlineSchemaTypeNode node)
    {
    }
}
