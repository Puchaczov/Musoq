using System;
using System.Collections.Generic;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public class BuildMetadataAndInferTypesTraverseVisitor(IAwareExpressionVisitor visitor)
    : RawTraverseVisitor<IAwareExpressionVisitor>(visitor), IQueryPartAwareExpressionVisitor
{
    private readonly Stack<Scope> _scopes = new();

    private IdentifierNode _theMostInnerIdentifier;

    public Scope Scope { get; private set; } = new(null, -1, "Root");

    public override void Visit(GroupSelectNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(DotNode node)
    {
        var self = node;
        var theMostOuter = self;
        while (self is not null)
        {
            theMostOuter = self;
            self = self.Root as DotNode;
        }

        var ident = theMostOuter.Root as IdentifierNode;

        if (ident != null && node == theMostOuter && Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name))
        {
            if (theMostOuter.Expression is AccessObjectArrayNode arrayNode)
            {
                var tableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(ident.Name);
                var columnInfo = tableSymbol?.GetColumnByAliasAndName(ident.Name, arrayNode.ObjectName);

                if (columnInfo != null)
                {
                    string elementIntendedTypeName = null;
                    if (!string.IsNullOrEmpty(columnInfo.IntendedTypeName) &&
                        columnInfo.IntendedTypeName.EndsWith("[]"))
                        elementIntendedTypeName =
                            columnInfo.IntendedTypeName.Substring(0, columnInfo.IntendedTypeName.Length - 2);

                    var enhancedArrayNode = new AccessObjectArrayNode(
                        arrayNode.Token,
                        columnInfo.ColumnType,
                        ident.Name,
                        elementIntendedTypeName
                    );
                    enhancedArrayNode.Accept(Visitor);
                    return;
                }
            }

            IdentifierNode column = null;
            if (theMostOuter.Expression is DotNode dotNode)
                column = dotNode.Root as IdentifierNode;
            else
                column = theMostOuter.Expression as IdentifierNode;

            if (column != null)
            {
                Visit(new AccessColumnNode(column.Name, ident.Name, node.Span));
                return;
            }
        }

        if (ident != null && node == theMostOuter &&
            !Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name) &&
            !Visitor.IsCurrentContextColumn(ident.Name))
        {
            var column = theMostOuter.Expression as IdentifierNode;
            if (column != null)
            {
                Visit(new AccessColumnNode(column.Name, ident.Name,
                    ident.HasSpan ? ident.Span : TextSpan.Empty));
                return;
            }
        }

        var setTheMostInnerIdentifier = false;
        if (_theMostInnerIdentifier is null)
        {
            _theMostInnerIdentifier = node.Expression as IdentifierNode;
            if (_theMostInnerIdentifier != null) setTheMostInnerIdentifier = true;
        }

        if (_theMostInnerIdentifier is not null && setTheMostInnerIdentifier)
            Visitor.SetTheMostInnerIdentifierOfDotNode(_theMostInnerIdentifier);

        self = node;

        while (self is not null)
        {
            self.Root.Accept(this);
            self.Expression.Accept(this);
            self.Accept(Visitor);

            self = self.Expression as DotNode;
        }

        if (_theMostInnerIdentifier is not null && setTheMostInnerIdentifier)
        {
            Visitor.SetTheMostInnerIdentifierOfDotNode(null);
            _theMostInnerIdentifier = null;
        }
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
        SetQueryPart(QueryPart.Having);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

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

        var sourceId = join.Source is ApplyFromNode ? MetaAttributes.ProcessedQueryId : join.Source.Id;
        var firstTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[sourceId]);
        var secondTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.With.Id]);

        switch (join.JoinType)
        {
            case JoinType.Inner:
                break;
            case JoinType.OuterLeft:
                secondTableSymbol = secondTableSymbol.MakeNullableIfPossible();
                Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.With.Id], secondTableSymbol);
                break;
            case JoinType.OuterRight:
                firstTableSymbol = firstTableSymbol.MakeNullableIfPossible();
                Scope.ScopeSymbolTable.UpdateSymbol(Scope[sourceId], firstTableSymbol);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var id = $"{Scope[sourceId]}{Scope[join.With.Id]}";

        Scope.ScopeSymbolTable.AddSymbol(id, firstTableSymbol.MergeSymbols(secondTableSymbol));
        Scope[MetaAttributes.ProcessedQueryId] = id;

        join.Expression.Accept(this);
        join.Accept(Visitor);

        while (joins.Count > 0)
        {
            join = joins.Pop();
            join.With.Accept(this);

            var currentTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.With.Id]);
            var previousTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(id);

            switch (join.JoinType)
            {
                case JoinType.Inner:
                    break;
                case JoinType.OuterLeft:
                    currentTableSymbol = currentTableSymbol.MakeNullableIfPossible();
                    Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.With.Id], currentTableSymbol);
                    break;
                case JoinType.OuterRight:
                    previousTableSymbol = previousTableSymbol.MakeNullableIfPossible();
                    Scope.ScopeSymbolTable.UpdateSymbol(id, previousTableSymbol);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }

            id = $"{id}{Scope[join.With.Id]}";

            Scope.ScopeSymbolTable.AddSymbol(id, previousTableSymbol.MergeSymbols(currentTableSymbol));
            Scope[MetaAttributes.ProcessedQueryId] = id;

            join.Expression.Accept(this);
            join.Accept(Visitor);
        }
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

        var sourceId = apply.Source is JoinFromNode ? MetaAttributes.ProcessedQueryId : apply.Source.Id;
        var firstTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[sourceId]);
        var secondTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[apply.With.Id]);

        switch (apply.ApplyType)
        {
            case ApplyType.Cross:
                break;
            case ApplyType.Outer:
                secondTableSymbol = secondTableSymbol.MakeNullableIfPossible();
                Scope.ScopeSymbolTable.UpdateSymbol(Scope[apply.With.Id], secondTableSymbol);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var id = $"{Scope[sourceId]}{Scope[apply.With.Id]}";

        Scope.ScopeSymbolTable.AddSymbol(id, firstTableSymbol.MergeSymbols(secondTableSymbol));
        Scope[MetaAttributes.ProcessedQueryId] = id;

        apply.Accept(Visitor);

        while (applies.Count > 0)
        {
            apply = applies.Pop();
            apply.With.Accept(this);

            var currentTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[apply.With.Id]);
            var previousTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(id);

            switch (apply.ApplyType)
            {
                case ApplyType.Cross:
                    break;
                case ApplyType.Outer:
                    currentTableSymbol = currentTableSymbol.MakeNullableIfPossible();
                    Scope.ScopeSymbolTable.UpdateSymbol(Scope[apply.With.Id], currentTableSymbol);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }

            id = $"{id}{Scope[apply.With.Id]}";

            Scope.ScopeSymbolTable.AddSymbol(id, previousTableSymbol.MergeSymbols(currentTableSymbol));
            Scope[MetaAttributes.ProcessedQueryId] = id;

            apply.Accept(Visitor);
        }
    }

    public override void Visit(QueryNode node)
    {
        LoadQueryScope();

        SetQueryPart(QueryPart.From);
        node.From.Accept(this);

        SetQueryPart(QueryPart.Where);
        node.Where?.Accept(this);

        SetQueryPart(QueryPart.GroupBy);
        node.GroupBy?.Accept(this);

        SetQueryPart(QueryPart.Select);
        node.Select.Accept(this);

        node.Skip?.Accept(this);
        node.Take?.Accept(this);

        SetQueryPart(QueryPart.OrderBy);
        node.OrderBy?.Accept(this);
        node.Accept(Visitor);
        RestoreScope();
        SetQueryPart(QueryPart.None);
        EndQueryScope();
    }

    public override void Visit(DescNode node)
    {
        LoadScope("Desc");
        node.From.Accept(this);
        node.Accept(Visitor);
        RestoreScope();
    }

    public override void Visit(UnionNode node)
    {
        LoadScope("Union");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(UnionAllNode node)
    {
        LoadScope("UnionAll");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(ExceptNode node)
    {
        LoadScope("Except");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(IntersectNode node)
    {
        LoadScope("Intersect");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(CteExpressionNode node)
    {
        LoadScope("CTE");
        foreach (var exp in node.InnerExpression) exp.Accept(this);
        node.OuterExpression.Accept(this);
        node.Accept(Visitor);
        RestoreScope();
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        LoadScope("CTE Inner Expression");
        Visitor.InnerCteBegins();
        node.Value.Accept(this);
        Visitor.InnerCteEnds();
        node.Accept(Visitor);
        RestoreScope();
    }

    public virtual void SetQueryPart(QueryPart part)
    {
        Visitor.SetQueryPart(part);
    }

    public virtual void QueryBegins()
    {
        Visitor.QueryBegins();
    }

    public virtual void QueryEnds()
    {
        Visitor.QueryEnds();
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
        RestoreScope();
    }
}
