using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class RewriteQueryTraverseVisitor(IScopeAwareExpressionVisitor visitor, ScopeWalker walker)
    : InterpretationSchemaDefinitionSkippingTraverseVisitor<IScopeAwareExpressionVisitor>(visitor)
{
    /// <summary>
    ///     Tracks the current ApplyType when traversing inside an ApplyFromNode.With.
    ///     Used to detect when AccessMethodFromNode with Interpret() should be transformed to InterpretFromNode.
    /// </summary>
    private ApplyType? _currentApplyType;

    private ScopeWalker _walker = walker;

    public override void Visit(DotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Root.Accept(this);


        if (node is { Root: IdentifierNode ident, Expression: AccessObjectArrayNode { IsColumnAccess: false } arrayNode } &&
            _walker.Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name))
        {
            var tableSymbol = _walker.Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(ident.Name);
            if (tableSymbol is null)
                throw new InvalidOperationException($"Table symbol '{ident.Name}' was not found.");
            var columnInfo = tableSymbol.GetColumnByAliasAndName(ident.Name, arrayNode.ObjectName);
            if (columnInfo is null)
                throw new InvalidOperationException($"Column '{arrayNode.ObjectName}' was not found for alias '{ident.Name}'.");

            string? elementIntendedTypeName = null;
            if (!string.IsNullOrEmpty(columnInfo.IntendedTypeName) && columnInfo.IntendedTypeName.EndsWith("[]", StringComparison.Ordinal))
                elementIntendedTypeName =
                    columnInfo.IntendedTypeName[..^2];


            var enhancedArrayNode = new AccessObjectArrayNode(
                arrayNode.Token,
                columnInfo.ColumnType,
                ident.Name,
                elementIntendedTypeName);


            enhancedArrayNode.Accept(this);
            node.Accept(Visitor);
            return;
        }

        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(GroupByNode node)
    {
        VisitChildrenThenNode(node);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.First.Accept(this);


        _currentApplyType = node.ApplyType;
        node.Second.Accept(this);
        _currentApplyType = null;

        node.Accept(Visitor);
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
        join.TieBreak?.Accept(this);
        join.Accept(Visitor);

        while (joins.Count > 1)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
            join.TieBreak?.Accept(this);
            join.Accept(Visitor);
        }

        if (joins.Count <= 0) return;

        join = joins.Pop();
        join.With.Accept(this);
        join.Expression.Accept(this);
        join.TieBreak?.Accept(this);
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


        _currentApplyType = apply.ApplyType;
        apply.With.Accept(this);
        _currentApplyType = null;

        apply.Accept(Visitor);

        while (applies.Count > 1)
        {
            apply = applies.Pop();


            _currentApplyType = apply.ApplyType;
            apply.With.Accept(this);
            _currentApplyType = null;

            apply.Accept(Visitor);
        }

        if (applies.Count <= 0) return;

        apply = applies.Pop();


        _currentApplyType = apply.ApplyType;
        apply.With.Accept(this);
        _currentApplyType = null;

        apply.Accept(Visitor);
    }

    public override void Visit(AccessMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_currentApplyType.HasValue && IsInterpretFunctionCall(node.AccessMethod.Name))
        {
            var interpretCallNode = CreateInterpretCallNode(node.AccessMethod);


            interpretCallNode.Accept(this);


            var interpretFromNode = new InterpretFromNode(node.Alias, interpretCallNode, _currentApplyType.Value);
            interpretFromNode.Accept(Visitor);
            return;
        }


        node.Accept(Visitor);
    }

    public override void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_currentApplyType.HasValue && IsInterpretFunctionCall(node.Identifier))
        {
            var interpretCallNode = CreateInterpretCallNodeFromAliasedFrom(node);


            interpretCallNode.Accept(this);


            var interpretFromNode =
                new InterpretFromNode(node.Alias, interpretCallNode, _currentApplyType.Value, node.ReturnType ?? typeof(object));
            interpretFromNode.Accept(Visitor);
            return;
        }

        node.Accept(Visitor);
    }

    public override void Visit(QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        TraverseChildren(node);
        node.Accept(Visitor);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }
}
