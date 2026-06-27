using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class ParserNodeChildTraversal
{
    public static void TraverseChildren(Node node, IExpressionVisitor visitor)
    {
        foreach (var child in EnumerateChildren(node))
            child.Accept(visitor);
    }

    public static void TraverseCteInnerExpressionsThenOuter(CteExpressionNode node, IExpressionVisitor visitor)
    {
        foreach (var child in EnumerateCteInnerExpressionsThenOuter(node))
            child.Accept(visitor);
    }

    public static IEnumerable<Node> EnumerateChildren(Node node)
    {
        return node switch
        {
            SelectNode select => select.Fields,
            ParameterBlockNode parameters => parameters.Parameters,
            ParameterDeclarationNode parameter => Optional(parameter.DefaultValue),
            BinaryNode binary => Optional(binary.Left, binary.Right),
            AccessRefreshAggregationScoreNode score => Optional(score.Arguments),
            AccessMethodNode accessMethod => Optional(accessMethod.Arguments),
            IsNullNode isNull => Optional(isNull.Expression),
            RowPresenceNode rowPresence => Optional(rowPresence.Expression),
            AllColumnsNode allColumns => AllColumnsChildren(allColumns),
            DotNode dot => Optional(dot.Root, dot.Expression),
            RootNode root => Optional(root.Expression),
            SingleSetNode set => Optional(set.Query),
            RefreshNode refresh => refresh.Nodes,
            MultiStatementNode multiStatement => multiStatement.Nodes,
            CteExpressionNode cte => CteChildren(cte),
            CteInnerExpressionNode cteInner => Optional(cteInner.Value),
            JoinNode join => Optional(join.Join),
            ApplyNode apply => Optional(apply.Apply),
            OrderByNode orderBy => orderBy.Fields,
            StatementsArrayNode statements => statements.Statements,
            StatementNode statement => Optional(statement.Node),
            CaseNode caseNode => CaseChildren(caseNode),
            WhenNode whenNode => Optional(whenNode.Expression),
            ThenNode thenNode => Optional(thenNode.Expression),
            ElseNode elseNode => Optional(elseNode.Expression),
            WindowFunctionNode windowFunction => Optional(windowFunction.FunctionCall, windowFunction.WindowSpecification),
            WindowSpecificationNode windowSpecification => WindowSpecificationChildren(windowSpecification),
            WindowFrameNode windowFrame => Optional(windowFrame.Start, windowFrame.End),
            WindowFrameBoundNode => [],
            WindowDefinitionNode windowDefinition => Optional(windowDefinition.Specification),
            WindowNode window => window.Definitions,
            QualifyNode qualify => Optional(qualify.Expression),
            WhereNode where => Optional(where.Expression),
            GroupByNode groupBy => GroupByChildren(groupBy),
            HavingNode having => Optional(having.Expression),
            JoinInMemoryWithSourceTableFromNode join => Optional(join.SourceTable, join.Expression, join.TieBreak),
            ApplyInMemoryWithSourceTableFromNode apply => Optional(apply.SourceTable),
            SchemaFromNode schema => Optional(schema.Parameters),
            JoinSourcesTableFromNode join => Optional(join.Expression, join.First, join.Second, join.TieBreak),
            ApplySourcesTableFromNode apply => Optional(apply.First, apply.Second),
            DerivedTableFromNode derived => Optional(derived.Query),
            ValuesFromNode values => ValuesChildren(values),
            UnpivotFromNode unpivot => UnpivotChildren(unpivot),
            JoinFromNode join => Optional(join.Source, join.With, join.Expression, join.TieBreak),
            ApplyFromNode apply => Optional(apply.Source, apply.With),
            ExpressionFromNode expression => Optional(expression.Expression),
            InterpretFromNode interpret => Optional(interpret.InterpretCall),
            AccessMethodFromNode accessMethod => Optional(accessMethod.AccessMethod),
            AliasedFromNode aliased => Optional(aliased.Args),
            CreateTransformationTableNode transformation => transformation.Fields,
            TranslatedSetTreeNode translated => translated.Nodes,
            QueryNode query => QueryChildren(query),
            InterpretCallNode interpret => Optional(interpret.DataSource),
            ParseCallNode parse => Optional(parse.DataSource),
            InterpretAtCallNode interpretAt => Optional(interpretAt.DataSource, interpretAt.Offset),
            TryInterpretCallNode tryInterpret => Optional(tryInterpret.DataSource),
            TryParseCallNode tryParse => Optional(tryParse.DataSource),
            PartialInterpretCallNode partial => Optional(partial.DataSource),
            PartialParseCallNode partial => Optional(partial.DataSource),
            FieldDefinitionNode field => Optional(field.AtOffset, field.WhenCondition, field.Constraint),
            ComputedFieldNode computed => Optional(computed.Expression),
            FieldConstraintNode constraint => Optional(constraint.Expression),
            ArrayTypeNode array => Optional(array.ElementType, array.SizeExpression),
            InlineSchemaTypeNode inlineSchema => inlineSchema.Fields,
            ShortCircuitingNodeLeft shortCircuit => Optional(shortCircuit.Expression),
            ShortCircuitingNodeRight shortCircuit => Optional(shortCircuit.Expression),
            NotNode not => Optional(not.Expression),
            InQueryNode inQuery => Optional(inQuery.Left, inQuery.Subquery),
            ExistsQueryNode exists => Optional(exists.Subquery),
            ScalarSubqueryNode scalar => Optional(scalar.Subquery),
            BetweenNode between => Optional(between.Expression, between.Min, between.Max),
            CastNode cast => Optional(cast.Expression),
            FieldNode field => Optional(field.Expression),
            ArgsListNode args => args.Args,
            DescNode desc => Optional(desc.Query ?? desc.From),
            ArrayIndexNode arrayIndex => Optional(arrayIndex.Array, arrayIndex.Index),
            ScriptVariableDeclarationNode scriptVariable => Optional(scriptVariable.Initializer),
            _ => []
        };
    }

    private static IEnumerable<Node> AllColumnsChildren(AllColumnsNode node)
    {
        if (node.ReplaceItems is not { Length: > 0 })
            yield break;

        foreach (var replaceItem in node.ReplaceItems)
            yield return replaceItem.Expression;
    }

    private static IEnumerable<Node> CaseChildren(CaseNode node)
    {
        yield return node.Else;

        for (var i = node.WhenThenPairs.Length - 1; i >= 0; --i)
        {
            yield return node.WhenThenPairs[i].When;
            yield return node.WhenThenPairs[i].Then;
        }
    }

    private static IEnumerable<Node> CteChildren(CteExpressionNode node)
    {
        yield return node.OuterExpression;

        foreach (var expression in node.InnerExpression)
            yield return expression;
    }

    private static IEnumerable<Node> EnumerateCteInnerExpressionsThenOuter(CteExpressionNode node)
    {
        foreach (var expression in node.InnerExpression)
            yield return expression;

        yield return node.OuterExpression;
    }

    private static IEnumerable<Node> GroupByChildren(GroupByNode node)
    {
        foreach (var field in node.Fields)
            yield return field;

        if (node.Having != null)
            yield return node.Having;
    }

    private static IEnumerable<Node> Optional(params Node?[] nodes)
    {
        foreach (var node in nodes)
            if (node != null)
                yield return node;
    }

    private static IEnumerable<Node> QueryChildren(QueryNode node)
    {
        yield return node.From;

        if (node.Where != null)
            yield return node.Where;

        yield return node.Select;

        if (node.Take != null)
            yield return node.Take;

        if (node.Skip != null)
            yield return node.Skip;

        if (node.GroupBy != null)
            yield return node.GroupBy;

        if (node.Window != null)
            yield return node.Window;

        if (node.Qualify != null)
            yield return node.Qualify;

        if (node.OrderBy != null)
            yield return node.OrderBy;
    }

    private static IEnumerable<Node> ValuesChildren(ValuesFromNode node)
    {
        foreach (var row in node.Rows)
        foreach (var field in row.Fields)
            yield return field.Expression;
    }

    private static IEnumerable<Node> UnpivotChildren(UnpivotFromNode node)
    {
        yield return node.Source;

        foreach (var entry in node.Entries)
            yield return entry.Expression;

        foreach (var keepField in node.KeepFields)
            yield return keepField;
    }

    private static IEnumerable<Node> WindowSpecificationChildren(WindowSpecificationNode node)
    {
        foreach (var field in node.PartitionFields)
            yield return field;

        foreach (var field in node.OrderByFields)
            yield return field;

        if (node.Frame != null)
            yield return node.Frame;
    }
}
