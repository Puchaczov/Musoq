using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class BuildMetadataAndInferTypesTraverseVisitor(IAwareExpressionVisitor visitor)
    : IQueryPartAwareExpressionVisitor
{
    private readonly Stack<Scope> _scopes = new();
    private readonly IAwareExpressionVisitor _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        
    private IdentifierNode _theMostInnerIdentifier;

    public Scope Scope { get; private set; } = new(null, -1, "Root");

    public void Visit(SelectNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupSelectNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(StringNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(IntegerNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(BooleanNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(WordNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(NullNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(ContainsNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessMethodNode node)
    {
        node.Arguments.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessRawIdentifierNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(IsNullNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
        node.Arguments.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessColumnNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AllColumnsNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(IdentifierNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AccessObjectArrayNode node)
    {
        // Let the main visitor handle all AccessObjectArrayNode processing
        // The BuildMetadataAndInferTypesVisitor will do the proper type inference
        node.Accept(_visitor);
    }



    public void Visit(AccessObjectKeyNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(PropertyValueNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(DotNode node)
    {
        var self = node;
        var theMostOuter = self;
        while (!(self is null))
        {
            theMostOuter = self;
            self = self.Root as DotNode;
        }

        var ident = (IdentifierNode) theMostOuter.Root;
        if (node == theMostOuter && Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name))
        {
            // Check for aliased indexed access pattern: f.Name[0], f.Data[2], etc.
            if (theMostOuter.Expression is AccessObjectArrayNode arrayNode)
            {
                // Get column type for proper type inference
                var tableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(ident.Name);
                var columnInfo = tableSymbol?.GetColumnByAliasAndName(ident.Name, arrayNode.ObjectName);
                
                if (columnInfo != null)
                {
                    // Transform to enhanced AccessObjectArrayNode with column type information
                    var enhancedArrayNode = new AccessObjectArrayNode(
                        arrayNode.Token, 
                        columnInfo.ColumnType, 
                        ident.Name
                    );
                    enhancedArrayNode.Accept(_visitor);
                    return;
                }
            }
            
            IdentifierNode column;
            if (theMostOuter.Expression is DotNode dotNode)
            {
                column = (IdentifierNode) dotNode.Root;
            }
            else
            {
                column = (IdentifierNode) theMostOuter.Expression;
            }

            Visit(new AccessColumnNode(column.Name, ident.Name, TextSpan.Empty));
            return;
        }

        var setTheMostInnerIdentifier = false;
        if (_theMostInnerIdentifier is null)
        {
            _theMostInnerIdentifier = (IdentifierNode)node.Expression;
            setTheMostInnerIdentifier = true;
        }

        if (_theMostInnerIdentifier is not null && setTheMostInnerIdentifier)
        {
            _visitor.SetTheMostInnerIdentifierOfDotNode(_theMostInnerIdentifier);
        }

        self = node;
            
        while (self is not null)
        {
            self.Root.Accept(this);
            self.Expression.Accept(this);
            self.Accept(_visitor);

            self = self.Expression as DotNode;
        }
            
        if (_theMostInnerIdentifier is not null && setTheMostInnerIdentifier)
        {
            _visitor.SetTheMostInnerIdentifierOfDotNode(null);
            _theMostInnerIdentifier = null;
        }
    }

    public void Visit(AccessCallChainNode node)
    {
        node.Accept(_visitor);
    }

    public virtual void Visit(WhereNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Having?.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(HavingNode node)
    {
        SetQueryPart(QueryPart.Having);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SkipNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TakeNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        node.SourceTable.Accept(this);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        node.SourceTable.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SchemaFromNode node)
    {
        node.Parameters.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(InMemoryTableFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(JoinFromNode node)
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

        var firstTableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(Scope[join.Source.Id]);
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
                Scope.ScopeSymbolTable.UpdateSymbol(Scope[join.Source.Id], firstTableSymbol);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var id = $"{Scope[join.Source.Id]}{Scope[join.With.Id]}";

        Scope.ScopeSymbolTable.AddSymbol(id, firstTableSymbol.MergeSymbols(secondTableSymbol));
        Scope[MetaAttributes.ProcessedQueryId] = id;

        join.Expression.Accept(this);
        join.Accept(_visitor);

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
            join.Accept(_visitor);
        }
    }

    public void Visit(ApplyFromNode node)
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

        apply.Accept(_visitor);

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

            apply.Accept(_visitor);
        }
    }

    public void Visit(ExpressionFromNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessMethodFromNode node)
    {
        node.AccessMethod.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(PropertyFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(CreateTransformationTableNode node)
    {
        foreach (var item in node.Fields)
            item.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(RenameTableNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TranslatedSetTreeNode node)
    {
        foreach (var item in node.Nodes)
            item.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(IntoNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(QueryScope node)
    {
        node.Accept(_visitor);
    }

    public void Visit(ShouldBePresentInTheTable node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TranslatedSetOperatorNode node)
    {
        foreach (var item in node.CreateTableNodes)
            item.Accept(_visitor);

        node.FQuery.Accept(this);
        node.SQuery.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(QueryNode node)
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
        node.OrderBy?.Accept(this);
        node.Accept(_visitor);
        RestoreScope();
        SetQueryPart(QueryPart.None);
        EndQueryScope();
    }

    public void Visit(OrNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(HyphenNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AndNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(EqualityNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GreaterOrEqualNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LessOrEqualNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GreaterNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LessNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(DiffNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(NotNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LikeNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(RLikeNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FieldNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FieldOrderedNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ArgsListNode node)
    {
        foreach (var item in node.Args)
            item.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(DecimalNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(Node node)
    {
        throw new NotSupportedException("Node cannot be visited.");
    }

    public void Visit(DescNode node)
    {
        LoadScope("Desc");
        node.From.Accept(this);
        node.Accept(_visitor);
        RestoreScope();
    }

    public void Visit(StarNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FSlashNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ModuloNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AddNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InternalQueryNode node)
    {
    }

    public void Visit(RootNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SingleSetNode node)
    {
        node.Query.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(UnionNode node)
    {
        LoadScope("Union");
        TraverseSetOperator(node);
    }

    public void Visit(UnionAllNode node)
    {
        LoadScope("UnionAll");
        TraverseSetOperator(node);
    }

    public void Visit(ExceptNode node)
    {
        LoadScope("Except");
        TraverseSetOperator(node);
    }

    public void Visit(RefreshNode node)
    {
        foreach (var item in node.Nodes)
            item.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(IntersectNode node)
    {
        LoadScope("Intersect");
        TraverseSetOperator(node);
    }

    public void Visit(PutTrueNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(MultiStatementNode node)
    {
        foreach (var cNode in node.Nodes)
            cNode.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(CteExpressionNode node)
    {
        LoadScope("CTE");
        foreach (var exp in node.InnerExpression)
        {
            exp.Accept(this);
        }
        node.OuterExpression.Accept(this);
        node.Accept(_visitor);
        RestoreScope();
    }

    public void Visit(CteInnerExpressionNode node)
    {
        LoadScope("CTE Inner Expression");
        _visitor.InnerCteBegins();
        node.Value.Accept(this);
        _visitor.InnerCteEnds();
        node.Accept(_visitor);
        RestoreScope();
    }

    public void Visit(JoinNode node)
    {
        node.Join.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ApplyNode node)
    {
        node.Apply.Accept(this);
        node.Accept(_visitor);
    }

    private void LoadQueryScope()
    {
        LoadScope("Query");
        _visitor.QueryBegins();
    }
        
    private void EndQueryScope()
    {
        _visitor.QueryEnds();
    }

    private void LoadScope(string name)
    {
        var newScope = Scope.AddScope(name);
        _scopes.Push(Scope);
        Scope = newScope;
        
        _visitor.SetScope(newScope);
    }

    private void RestoreScope()
    {
        Scope = _scopes.Pop();
        _visitor.SetScope(Scope);
    }

    public void Visit(OrderByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Accept(_visitor);
    }

    private void TraverseSetOperator(SetOperatorNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
        RestoreScope();
    }

    public void SetQueryPart(QueryPart part)
    {
        _visitor.SetQueryPart(part);
    }

    public void Visit(CreateTableNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(CoupleNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(SchemaMethodFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AliasedFromNode node)
    {
        node.Args.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(StatementsArrayNode node)
    {
        foreach (var statement in node.Statements)
            statement.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(StatementNode node)
    {
        node.Node.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(CaseNode node)
    {   
        node.Else.Accept(this);
            
        for (var i = node.WhenThenPairs.Length - 1; i >= 0; --i)
        {
            node.WhenThenPairs[i].When.Accept(this);
            node.WhenThenPairs[i].Then.Accept(this);
        }

        node.Accept(_visitor);
    }

    public void Visit(WhenNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ThenNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ElseNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FieldLinkNode node)
    {
        node.Accept(_visitor);
    }

    public void QueryBegins()
    {
        _visitor.QueryBegins();
    }

    public void QueryEnds()
    {
        _visitor.QueryEnds();
    }

    /// <summary>
    /// Determines if an AccessObjectArrayNode represents direct column access rather than property access
    /// This handles direct column access like Name[0] (not aliased like f.Name[0])
    /// </summary>
    private bool IsDirectColumnAccess(AccessObjectArrayNode node)
    {
        var columnName = node.ObjectName;
        
        // Don't transform these special object references - let original array access logic handle them
        var specialObjectReferences = new[] { "Self", "This", "Current" };
        
        if (specialObjectReferences.Any(reference => string.Equals(reference, columnName, StringComparison.OrdinalIgnoreCase)))
        {
            return false; // These are object references, not columns
        }
        
        // For now, be conservative and return false since table context timing is problematic
        // The main BuildMetadataAndInferTypesVisitor should handle the transformation
        return false;
    }
}