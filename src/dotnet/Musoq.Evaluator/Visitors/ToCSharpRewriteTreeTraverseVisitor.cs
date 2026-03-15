using System;
using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public class ToCSharpRewriteTreeTraverseVisitor : RawTraverseVisitor<IToCSharpTranslationExpressionVisitor>
{
    private readonly CompilationOptions _compilationOptions;
    private ScopeWalker _walker;

    public ToCSharpRewriteTreeTraverseVisitor(IToCSharpTranslationExpressionVisitor visitor, ScopeWalker walker,
        CompilationOptions compilationOptions)
        : base(visitor)
    {
        _walker = walker;
        _compilationOptions = compilationOptions;
    }

    public override void Visit(WhereNode node)
    {
        Visitor.AddNullSuspiciousSection();
        node.Expression.Accept(this);
        node.Accept(Visitor);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(GroupByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Having?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(HavingNode node)
    {
        Visitor.AddNullSuspiciousSection();
        node.Expression.Accept(this);
        node.Accept(Visitor);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        Visitor.SetInsideJoinOrApply(true);
        Visitor.AddNullSuspiciousSection();

        node.SourceTable.Accept(this);
        node.Expression.Accept(this);
        node.Accept(Visitor);

        Visitor.SetInsideJoinOrApply(false);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        Visitor.SetInsideJoinOrApply(true);
        Visitor.AddNullSuspiciousSection();

        node.SourceTable.Accept(this);
        node.Accept(Visitor);

        Visitor.SetInsideJoinOrApply(false);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        Visitor.SetInsideJoinOrApply(true);
        Visitor.AddNullSuspiciousSection();

        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(Visitor);

        Visitor.SetInsideJoinOrApply(false);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        Visitor.SetInsideJoinOrApply(true);
        Visitor.AddNullSuspiciousSection();

        node.First.Accept(this);
        node.Second.Accept(this);

        Visitor.SetQueryIdentifier(node.Alias);

        node.Accept(Visitor);

        Visitor.SetInsideJoinOrApply(false);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(JoinFromNode node)
    {
        var joins = new Stack<JoinFromNode>();

        var join = node;
        while (join != null)
        {
            joins.Push(join);
            join = join.Source as JoinFromNode;
        }

        join = joins.Pop();

        join.Source.Accept(this);
        join.With.Accept(this);
        join.Expression.Accept(this);

        while (joins.Count > 1)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
        }

        if (joins.Count > 0)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
        }

        join.Accept(Visitor);
    }

    public override void Visit(ApplyFromNode node)
    {
        var applies = new Stack<ApplyFromNode>();

        var apply = node;
        while (apply != null)
        {
            applies.Push(apply);
            apply = apply.Source as ApplyFromNode;
        }

        apply = applies.Pop();

        apply.Source.Accept(this);
        apply.With.Accept(this);

        while (applies.Count > 1)
        {
            apply = applies.Pop();
            apply.With.Accept(this);
        }

        if (applies.Count > 0)
        {
            apply = applies.Pop();
            apply.With.Accept(this);
        }

        apply.Accept(Visitor);
    }

    public override void Visit(ExpressionFromNode node)
    {
        Visitor.SetQueryIdentifier(node.Alias);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(InterpretFromNode node)
    {
        Visitor.SetQueryIdentifier(node.Alias);
        node.InterpretCall.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(AliasedFromNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(CreateTransformationTableNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        node.Accept(Visitor);

        _walker = _walker.Parent();
    }

    public override void Visit(QueryNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        Visitor.SetMethodAccessType(MethodAccessType.ResultQuery);

        if (
            _compilationOptions.ParallelizationMode == ParallelizationMode.None ||
            node.Skip != null ||
            node.Take != null ||
            node.OrderBy != null
        )
            Visitor.SetResultParallelizationImpossible();

        Visitor.SetQueryIdentifier(node.From.Alias);

        Visitor.InitializeCseForQuery(node);

        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
    }

    public override void Visit(BetweenNode node)
    {
        throw new InvalidOperationException(
            "BetweenNode should have been desugared to AND/comparison nodes before code generation.");
    }

    public override void Visit(ArgsListNode node)
    {
        foreach (var item in node.Args)
        {
            Visitor.AddNullSuspiciousSection();
            item.Accept(this);
            Visitor.RemoveNullSuspiciousSection();
        }

        node.Accept(Visitor);
    }

    public override void Visit(Node node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(DescNode node)
    {
        _walker = _walker.NextChild();

        node.Accept(Visitor);

        _walker = _walker.Parent();
    }

    public override void Visit(InternalQueryNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        Visitor.SetMethodAccessType(MethodAccessType.TransformingQuery);
        Visitor.SetQueryIdentifier(node.From.Alias);

        Visitor.InitializeCseForQuery(node);

        node.Refresh?.Accept(this);
        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);
        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
    }

    public override void Visit(UnionNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(UnionAllNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(ExceptNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(IntersectNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(MultiStatementNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        foreach (var cNode in node.Nodes) cNode.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }

    public override void Visit(CteExpressionNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        foreach (var exp in node.InnerExpression) exp.Accept(this);
        node.OuterExpression.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        node.Value.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }

    public override void Visit(CaseNode node)
    {
        var oldMethodAccessType = Visitor.SetMethodAccessType(MethodAccessType.ResultQuery);

        Visitor.SetCaseWhenContext(true);

        node.Else.Accept(this);

        for (var i = node.WhenThenPairs.Length - 1; i >= 0; --i)
        {
            node.WhenThenPairs[i].When.Accept(this);
            node.WhenThenPairs[i].Then.Accept(this);
        }

        Visitor.SetCaseWhenContext(false);

        node.Accept(Visitor);

        Visitor.SetMethodAccessType(oldMethodAccessType);
    }

    public override void Visit(WhenNode node)
    {
        Visitor.AddNullSuspiciousSection();
        node.Expression.Accept(this);
        node.Accept(Visitor);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(ThenNode node)
    {
        Visitor.AddNullSuspiciousSection();
        node.Expression.Accept(this);
        node.Accept(Visitor);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(ElseNode node)
    {
        Visitor.AddNullSuspiciousSection();
        node.Expression.Accept(this);
        node.Accept(Visitor);
        Visitor.RemoveNullSuspiciousSection();
    }

    public override void Visit(FieldLinkNode node)
    {
        throw new NotSupportedException();
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

    private void TraverseSetOperatorWithScope(SetOperatorNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        if (node.Right is SetOperatorNode)
        {
            var howManyTimesChildWereUsed = 0;
            var nodes = new Stack<SetOperatorNode>();
            nodes.Push(node);

            node.Left.Accept(this);
            Visitor.IncrementMethodIdentifier();

            while (nodes.Count > 0)
            {
                var current = nodes.Pop();

                if (current.Right is SetOperatorNode operatorNode)
                {
                    nodes.Push(operatorNode);

                    _walker = _walker.NextChild();
                    Visitor.SetScope(_walker.Scope);

                    operatorNode.Left.Accept(this);
                    Visitor.IncrementMethodIdentifier();

                    current.Accept(Visitor);
                    howManyTimesChildWereUsed += 1;
                    continue;
                }

                current.Right.Accept(this);
                Visitor.IncrementMethodIdentifier();

                current.Accept(Visitor);

                howManyTimesChildWereUsed += 1;
            }

            for (var i = 0; i < howManyTimesChildWereUsed; ++i)
            {
                _walker = _walker.Parent();
                Visitor.SetScope(_walker.Scope);
            }
        }
        else
        {
            node.Left.Accept(this);

            Visitor.IncrementMethodIdentifier();

            node.Right.Accept(this);

            node.Accept(Visitor);

            _walker = _walker.Parent();
            Visitor.SetScope(_walker.Scope);
        }
    }
}
