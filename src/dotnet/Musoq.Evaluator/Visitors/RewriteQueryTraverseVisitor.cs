using System;
using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public class RewriteQueryTraverseVisitor : RawTraverseVisitor<IScopeAwareExpressionVisitor>
{
    /// <summary>
    ///     Tracks the current ApplyType when traversing inside an ApplyFromNode.With.
    ///     Used to detect when AccessMethodFromNode with Interpret() should be transformed to InterpretFromNode.
    /// </summary>
    private ApplyType? _currentApplyType;

    private ScopeWalker _walker;

    public RewriteQueryTraverseVisitor(IScopeAwareExpressionVisitor visitor, ScopeWalker walker)
        : base(visitor)
    {
        _walker = walker;
    }

    public override void Visit(DotNode node)
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
                node.Accept(Visitor);
                return;
            }
        }

        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(GroupByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Having?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
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
        join.Accept(Visitor);

        while (joins.Count > 1)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
            join.Accept(Visitor);
        }

        if (joins.Count <= 0) return;

        join = joins.Pop();
        join.With.Accept(this);
        join.Expression.Accept(this);
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
        if (_currentApplyType.HasValue && IsInterpretFunctionCall(node.Identifier))
        {
            var interpretCallNode = CreateInterpretCallNodeFromAliasedFrom(node);


            interpretCallNode.Accept(this);


            var interpretFromNode =
                new InterpretFromNode(node.Alias, interpretCallNode, _currentApplyType.Value, node.ReturnType);
            interpretFromNode.Accept(Visitor);
            return;
        }

        node.Accept(Visitor);
    }

    public override void Visit(QueryNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Window?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }

    public override void Visit(WindowFunctionNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(WindowSpecificationNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(WindowDefinitionNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(WindowNode node)
    {
        foreach (var definition in node.Definitions)
            definition.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(InternalQueryNode node)
    {
        _walker = _walker.NextChild();

        node.From.Accept(this);
        node.Where.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Refresh?.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
    }

    public override void Visit(DescNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        node.From.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
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

    public override void Visit(InterpretCallNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(ParseCallNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(TryInterpretCallNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(TryParseCallNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(PartialInterpretCallNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(InterpretAtCallNode node)
    {
        node.Accept(Visitor);
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

    private static bool IsInterpretFunctionCall(string methodName)
    {
        return methodName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryParse", StringComparison.OrdinalIgnoreCase);
    }

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
                IdentifierNode identifierNode => throw new InvalidOperationException(
                    $"Schema name '{identifierNode.Name}' must be quoted. Use 'InterpretAt(source, offset, \'{identifierNode.Name}\')' instead of 'InterpretAt(source, offset, {identifierNode.Name})'."),
                _ => throw new InvalidOperationException(
                    $"Expected schema name as a quoted string, got {node.Args.Args[2].GetType().Name}")
            };

            return new InterpretAtCallNode(dataSource, offset, schemaNameForAt, node.ReturnType);
        }


        var schemaName = node.Args.Args[1] switch
        {
            StringNode stringNode => stringNode.Value,
            WordNode wordNode => wordNode.Value,
            IdentifierNode identifierNode => throw new InvalidOperationException(
                $"Schema name '{identifierNode.Name}' must be quoted. Use '{node.Identifier}(source, \'{identifierNode.Name}\')' instead of '{node.Identifier}(source, {identifierNode.Name})'."),
            _ => throw new InvalidOperationException(
                $"Expected schema name as a quoted string, got {node.Args.Args[1].GetType().Name}")
        };


        if (node.Identifier.Equals("Parse", StringComparison.OrdinalIgnoreCase))
            return new ParseCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase))
            return new TryInterpretCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("TryParse", StringComparison.OrdinalIgnoreCase))
            return new TryParseCallNode(dataSource, schemaName, node.ReturnType);

        return new InterpretCallNode(dataSource, schemaName, node.ReturnType);
    }

    private void TraverseSetOperatorWithScope(SetOperatorNode node)
    {
        _walker = _walker.NextChild();
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
        _walker = _walker.Parent();
    }
}
