using System;
using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public class RewriteQueryTraverseVisitor : IExpressionVisitor
{
    private readonly IScopeAwareExpressionVisitor _visitor;

    /// <summary>
    ///     Tracks the current ApplyType when traversing inside an ApplyFromNode.With.
    ///     Used to detect when AccessMethodFromNode with Interpret() should be transformed to InterpretFromNode.
    /// </summary>
    private ApplyType? _currentApplyType;

    private ScopeWalker _walker;

    public RewriteQueryTraverseVisitor(IScopeAwareExpressionVisitor visitor, ScopeWalker walker)
    {
        _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        _walker = walker;
    }

    public void Visit(SelectNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupSelectNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);
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

    public void Visit(HexIntegerNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(BinaryIntegerNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(OctalIntegerNode node)
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
        node.Root.Accept(this);


        if (node.Root is IdentifierNode ident &&
            node.Expression is AccessObjectArrayNode arrayNode &&
            !arrayNode.IsColumnAccess &&
            _walker.Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name))
        {
            var tableSymbol = _walker.Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(ident.Name);
            var columnInfo = tableSymbol?.GetColumnByAliasAndName(ident.Name, arrayNode.ObjectName);

            if (columnInfo != null)
            {
                string elementIntendedTypeName = null;
                if (!string.IsNullOrEmpty(columnInfo.IntendedTypeName) && columnInfo.IntendedTypeName.EndsWith("[]"))
                    elementIntendedTypeName =
                        columnInfo.IntendedTypeName.Substring(0, columnInfo.IntendedTypeName.Length - 2);


                var enhancedArrayNode = new AccessObjectArrayNode(
                    arrayNode.Token,
                    columnInfo.ColumnType,
                    ident.Name,
                    elementIntendedTypeName);


                enhancedArrayNode.Accept(this);
                node.Accept(_visitor);
                return;
            }
        }

        node.Expression.Accept(this);
        node.Accept(_visitor);
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


        _currentApplyType = node.ApplyType;
        node.Second.Accept(this);
        _currentApplyType = null;

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
        join.Expression.Accept(this);
        join.Accept(_visitor);

        while (joins.Count > 1)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
            join.Accept(_visitor);
        }

        if (joins.Count <= 0) return;

        join = joins.Pop();
        join.With.Accept(this);
        join.Expression.Accept(this);
        join.Accept(_visitor);
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


        _currentApplyType = apply.ApplyType;
        apply.With.Accept(this);
        _currentApplyType = null;

        apply.Accept(_visitor);

        while (applies.Count > 1)
        {
            apply = applies.Pop();


            _currentApplyType = apply.ApplyType;
            apply.With.Accept(this);
            _currentApplyType = null;

            apply.Accept(_visitor);
        }

        if (applies.Count <= 0) return;

        apply = applies.Pop();


        _currentApplyType = apply.ApplyType;
        apply.With.Accept(this);
        _currentApplyType = null;

        apply.Accept(_visitor);
    }

    public void Visit(ExpressionFromNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InterpretFromNode node)
    {
        node.InterpretCall.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessMethodFromNode node)
    {
        if (_currentApplyType.HasValue && IsInterpretFunctionCall(node.AccessMethod.Name))
        {
            var interpretCallNode = CreateInterpretCallNode(node.AccessMethod);


            interpretCallNode.Accept(this);


            var interpretFromNode = new InterpretFromNode(node.Alias, interpretCallNode, _currentApplyType.Value);
            interpretFromNode.Accept(_visitor);
            return;
        }


        node.Accept(_visitor);
    }

    public void Visit(SchemaMethodFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(PropertyFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AliasedFromNode node)
    {
        if (_currentApplyType.HasValue && IsInterpretFunctionCall(node.Identifier))
        {
            var interpretCallNode = CreateInterpretCallNodeFromAliasedFrom(node);


            interpretCallNode.Accept(this);


            var interpretFromNode =
                new InterpretFromNode(node.Alias, interpretCallNode, _currentApplyType.Value, node.ReturnType);
            interpretFromNode.Accept(_visitor);
            return;
        }

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
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
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
        throw new NotSupportedException();
    }

    public void Visit(DescNode node)
    {
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        node.From.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
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

    public void Visit(BitwiseAndNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(BitwiseOrNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(BitwiseXorNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LeftShiftNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(RightShiftNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InternalQueryNode node)
    {
        _walker = _walker.NextChild();

        node.From.Accept(this);
        node.Where.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Refresh?.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
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
        TraverseSetOperator(node);
    }

    public void Visit(UnionAllNode node)
    {
        TraverseSetOperator(node);
    }

    public void Visit(ExceptNode node)
    {
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
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        foreach (var exp in node.InnerExpression) exp.Accept(this);
        node.OuterExpression.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
    }

    public void Visit(CteInnerExpressionNode node)
    {
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        node.Value.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
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

    public void Visit(OrderByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(CreateTableNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(CoupleNode node)
    {
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

    public void Visit(InterpretCallNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(ParseCallNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TryInterpretCallNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TryParseCallNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(PartialInterpretCallNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(InterpretAtCallNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(BinarySchemaNode node)
    {
    }

    public void Visit(TextSchemaNode node)
    {
    }

    public void Visit(FieldDefinitionNode node)
    {
    }

    public void Visit(ComputedFieldNode node)
    {
    }

    public void Visit(TextFieldDefinitionNode node)
    {
    }

    public void Visit(FieldConstraintNode node)
    {
    }

    public void Visit(PrimitiveTypeNode node)
    {
    }

    public void Visit(ByteArrayTypeNode node)
    {
    }

    public void Visit(StringTypeNode node)
    {
    }

    public void Visit(SchemaReferenceTypeNode node)
    {
    }

    public void Visit(ArrayTypeNode node)
    {
    }

    public void Visit(BitsTypeNode node)
    {
    }

    public void Visit(AlignmentNode node)
    {
    }

    public void Visit(RepeatUntilTypeNode node)
    {
    }

    public void Visit(InlineSchemaTypeNode node)
    {
    }

    /// <summary>
    ///     Checks if the method name is one of the interpret functions (Interpret, Parse, InterpretAt, TryInterpret,
    ///     TryParse).
    /// </summary>
    private static bool IsInterpretFunctionCall(string methodName)
    {
        return methodName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryParse", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Creates the appropriate interpret call node based on the method name.
    /// </summary>
    private static Node CreateInterpretCallNode(AccessMethodNode accessMethod)
    {
        var args = accessMethod.Arguments.Args;


        if (accessMethod.Name.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 3)
                throw new InvalidOperationException(
                    "InterpretAt function requires 3 arguments: data, offset, and schema name");

            var dataNode = args[0];
            var offsetNode = args[1];
            var schemaNameNode = args[2] as StringNode
                                 ?? throw new InvalidOperationException(
                                     "Third argument of InterpretAt must be a string literal schema name");

            return new InterpretAtCallNode(dataNode, offsetNode, schemaNameNode.Value);
        }

        if (args.Length < 2)
            throw new InvalidOperationException(
                $"{accessMethod.Name} function requires 2 arguments: data and schema name");

        var dataArg = args[0];
        var schemaArg = args[1] as StringNode
                        ?? throw new InvalidOperationException(
                            $"Second argument of {accessMethod.Name} must be a string literal schema name");

        if (accessMethod.Name.Equals("Parse", StringComparison.OrdinalIgnoreCase))
            return new ParseCallNode(dataArg, schemaArg.Value);

        if (accessMethod.Name.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase))
            return new TryInterpretCallNode(dataArg, schemaArg.Value);

        if (accessMethod.Name.Equals("TryParse", StringComparison.OrdinalIgnoreCase))
            return new TryParseCallNode(dataArg, schemaArg.Value);

        return new InterpretCallNode(dataArg, schemaArg.Value);
    }

    /// <summary>
    ///     Creates the appropriate interpret call node (InterpretCallNode, ParseCallNode, or InterpretAtCallNode)
    ///     from an AliasedFromNode that represents an Interpret/Parse/InterpretAt call.
    /// </summary>
    private Node CreateInterpretCallNodeFromAliasedFrom(AliasedFromNode node)
    {
        if (node.Args.Args.Length < 2)
            throw new InvalidOperationException(
                $"Interpret call requires at least 2 arguments, got {node.Args.Args.Length}");

        var dataSource = node.Args.Args[0];


        if (node.Identifier.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase))
        {
            if (node.Args.Args.Length < 3)
                throw new InvalidOperationException(
                    $"InterpretAt call requires 3 arguments, got {node.Args.Args.Length}");

            var offset = node.Args.Args[1];
            var schemaNameForAt = node.Args.Args[2] switch
            {
                StringNode stringNode => stringNode.Value,
                WordNode wordNode => wordNode.Value,
                _ => throw new InvalidOperationException(
                    $"Expected schema name as string or identifier, got {node.Args.Args[2].GetType().Name}")
            };

            return new InterpretAtCallNode(dataSource, offset, schemaNameForAt, node.ReturnType);
        }


        var schemaName = node.Args.Args[1] switch
        {
            StringNode stringNode => stringNode.Value,
            WordNode wordNode => wordNode.Value,
            _ => throw new InvalidOperationException(
                $"Expected schema name as string or identifier, got {node.Args.Args[1].GetType().Name}")
        };


        if (node.Identifier.Equals("Parse", StringComparison.OrdinalIgnoreCase))
            return new ParseCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase))
            return new TryInterpretCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("TryParse", StringComparison.OrdinalIgnoreCase))
            return new TryParseCallNode(dataSource, schemaName, node.ReturnType);

        return new InterpretCallNode(dataSource, schemaName, node.ReturnType);
    }

    public void Visit(FromNode node)
    {
        node.Accept(_visitor);
    }

    private void TraverseSetOperator(SetOperatorNode node)
    {
        _walker = _walker.NextChild();
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
        _walker = _walker.Parent();
    }
}
