using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class OuterJoinAdvisoryAnalyzer
{
    public static void Analyze(SemanticAdvisoryContext context)
    {
        var sourceColumns = CreateSourceColumnMap(context.Metadata.SourceContracts);
        var variables = context.Metadata.ScriptVariableDefinitions
            .ToDictionary(static definition => definition.Name, StringComparer.Ordinal);
        foreach (var query in EnumerateQueries(context.Query))
        {
            var optionalAliases = CollectOptionalAliases(query.From);
            if (optionalAliases.Count == 0)
                continue;

            AnalyzeQueryPredicates(context, query, optionalAliases, sourceColumns, variables);
        }
    }

    private static void AnalyzeQueryPredicates(
        SemanticAdvisoryContext context,
        QueryNode query,
        IReadOnlyDictionary<string, OptionalAlias> optionalAliases,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, BoundSchemaColumn>> sourceColumns,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        var predicate = query.Where?.Expression;
        if (predicate is null)
            return;

        foreach (var alias in optionalAliases.Values)
            AnalyzeOptionalAlias(context, predicate, alias, sourceColumns, variables);
    }

    private static void AnalyzeOptionalAlias(
        SemanticAdvisoryContext context,
        Node predicate,
        OptionalAlias alias,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, BoundSchemaColumn>> sourceColumns,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        if (ContainsPresenceGuard(predicate, alias.Name))
            return;

        ReportAmbiguousNullChecks(context, predicate, alias, sourceColumns);

        var missingRow = EvaluateMissingRow(predicate, alias.Name, variables);
        if (!missingRow.CanBeTrue)
        {
            context.Report(
                DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter,
                ErrorCatalog.GetMessage(DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter, alias.Name),
                missingRow.RejectingNode?.Span ?? predicate.Span);
        }
    }

    private static void ReportAmbiguousNullChecks(
        SemanticAdvisoryContext context,
        Node predicate,
        OptionalAlias alias,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, BoundSchemaColumn>> sourceColumns)
    {
        foreach (var node in EnumerateNodes(predicate))
        {
            if (node is not IsNullNode { IsNegated: false, Expression: AccessColumnNode column } ||
                !string.Equals(column.Alias, alias.Name, StringComparison.OrdinalIgnoreCase) ||
                !IsOriginalNullable(sourceColumns, column.Alias, column.Name))
                continue;

            context.Report(
                DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck,
                ErrorCatalog.GetMessage(DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck, column.Alias, column.Name),
                column.HasSpan ? column.Span : node.Span);
        }
    }

    private static bool IsOriginalNullable(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, BoundSchemaColumn>> sourceColumns,
        string alias,
        string column)
    {
        return sourceColumns.TryGetValue(alias, out var columns) &&
               columns.TryGetValue(column, out var metadata) &&
               metadata.IsNullable;
    }

    private static IReadOnlyDictionary<string, OptionalAlias> CollectOptionalAliases(FromNode from)
    {
        var aliases = new Dictionary<string, OptionalAlias>(StringComparer.OrdinalIgnoreCase);
        CollectOptionalAliases(from, aliases);
        return aliases;
    }

    private static void CollectOptionalAliases(FromNode from, IDictionary<string, OptionalAlias> aliases)
    {
        switch (from)
        {
            case ExpressionFromNode expression:
                CollectOptionalAliases(expression.Expression, aliases);
                return;
            case JoinNode join:
                CollectOptionalAliases(join.Join, aliases);
                return;
            case ApplyNode apply:
                CollectOptionalAliases(apply.Apply, aliases);
                return;
            case JoinFromNode join:
                AddOptionalJoinAliases(join.JoinType, join.Source, join.With, aliases);
                CollectOptionalAliases(join.Source, aliases);
                CollectOptionalAliases(join.With, aliases);
                return;
            case JoinSourcesTableFromNode join:
                AddOptionalJoinAliases(join.JoinType, join.First, join.Second, aliases);
                CollectOptionalAliases(join.First, aliases);
                CollectOptionalAliases(join.Second, aliases);
                return;
            case JoinInMemoryWithSourceTableFromNode join:
                AddOptionalJoinAliases(join.JoinType, join.InMemoryTableAlias, join.SourceTable, aliases);
                CollectOptionalAliases(join.SourceTable, aliases);
                return;
            case ApplyFromNode apply:
                if (apply.ApplyType == ApplyType.Outer)
                    AddAliases(apply.With, aliases);
                CollectOptionalAliases(apply.Source, aliases);
                CollectOptionalAliases(apply.With, aliases);
                return;
            case ApplySourcesTableFromNode apply:
                if (apply.ApplyType == ApplyType.Outer)
                    AddAliases(apply.Second, aliases);
                CollectOptionalAliases(apply.First, aliases);
                CollectOptionalAliases(apply.Second, aliases);
                return;
            case ApplyInMemoryWithSourceTableFromNode apply:
                if (apply.ApplyType == ApplyType.Outer)
                    AddAliases(apply.SourceTable, aliases);
                CollectOptionalAliases(apply.SourceTable, aliases);
                return;
        }
    }

    private static void AddOptionalJoinAliases(
        JoinType joinType,
        FromNode first,
        FromNode second,
        IDictionary<string, OptionalAlias> aliases)
    {
        if (joinType is JoinType.OuterRight or JoinType.OuterFull)
            AddAliases(first, aliases);

        if (joinType is JoinType.OuterLeft or JoinType.AsOfLeft or JoinType.OuterFull)
            AddAliases(second, aliases);
    }

    private static void AddOptionalJoinAliases(
        JoinType joinType,
        string firstAlias,
        FromNode second,
        IDictionary<string, OptionalAlias> aliases)
    {
        if (joinType is JoinType.OuterRight or JoinType.OuterFull)
            AddAlias(firstAlias, aliases);

        if (joinType is JoinType.OuterLeft or JoinType.AsOfLeft or JoinType.OuterFull)
            AddAliases(second, aliases);
    }

    private static void AddAliases(FromNode from, IDictionary<string, OptionalAlias> aliases)
    {
        foreach (var alias in EnumerateAliases(from))
            AddAlias(alias, aliases);
    }

    private static void AddAlias(string alias, IDictionary<string, OptionalAlias> aliases)
    {
        if (!string.IsNullOrWhiteSpace(alias))
            aliases.TryAdd(alias, new OptionalAlias(alias));
    }

    private static IEnumerable<string> EnumerateAliases(FromNode from)
    {
        switch (from)
        {
            case ExpressionFromNode expression:
                return EnumerateAliases(expression.Expression);
            case JoinNode join:
                return EnumerateAliases(join.Join);
            case ApplyNode apply:
                return EnumerateAliases(apply.Apply);
            case JoinFromNode join:
                return EnumerateAliases(join.Source).Concat(EnumerateAliases(join.With));
            case JoinSourcesTableFromNode join:
                return EnumerateAliases(join.First).Concat(EnumerateAliases(join.Second));
            case ApplyFromNode apply:
                return EnumerateAliases(apply.Source).Concat(EnumerateAliases(apply.With));
            case ApplySourcesTableFromNode apply:
                return EnumerateAliases(apply.First).Concat(EnumerateAliases(apply.Second));
            case JoinInMemoryWithSourceTableFromNode join:
                return new[] { join.InMemoryTableAlias }.Concat(EnumerateAliases(join.SourceTable));
            case ApplyInMemoryWithSourceTableFromNode apply:
                return new[] { apply.InMemoryTableAlias }.Concat(EnumerateAliases(apply.SourceTable));
            default:
                return string.IsNullOrWhiteSpace(from.Alias) ? [] : [from.Alias];
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, BoundSchemaColumn>> CreateSourceColumnMap(
        IReadOnlyList<BoundSourceContract> contracts)
    {
        return contracts
            .Where(static contract => !string.IsNullOrWhiteSpace(contract.Identity.Alias))
            .GroupBy(static contract => contract.Identity.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyDictionary<string, BoundSchemaColumn>)group
                    .SelectMany(static contract => contract.Columns)
                    .GroupBy(static column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<QueryNode> EnumerateQueries(Node root)
    {
        if (root is QueryNode query)
            yield return query;

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(root))
            foreach (var nested in EnumerateQueries(child))
                yield return nested;
    }

    private static IEnumerable<Node> EnumerateNodes(Node root)
    {
        yield return root;
        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(root))
            foreach (var nested in EnumerateNodes(child))
                yield return nested;
    }

    private static bool ContainsPresenceGuard(Node predicate, string alias)
    {
        return EnumerateNodes(predicate).Any(node =>
            node is RowPresenceNode { Expression: IdentifierNode identifier } &&
            string.Equals(identifier.Name, alias, StringComparison.OrdinalIgnoreCase));
    }

    private static MissingPredicateResult EvaluateMissingRow(
        Node node,
        string alias,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        var staticValue = ScriptVariableInitializerEvaluator.EvaluateStaticExpression(node, variables);
        if (staticValue.Success && staticValue.Value is bool value)
            return value ? MissingPredicateResult.True : MissingPredicateResult.FalseFor(node);

        switch (node)
        {
            case AndNode and:
            {
                var left = EvaluateMissingRow(and.Left, alias, variables);
                var right = EvaluateMissingRow(and.Right, alias, variables);
                return Combine(left, right, BooleanOperator.And);
            }
            case OrNode or:
            {
                var left = EvaluateMissingRow(or.Left, alias, variables);
                var right = EvaluateMissingRow(or.Right, alias, variables);
                return Combine(left, right, BooleanOperator.Or);
            }
            case NotNode not:
            {
                var result = EvaluateMissingRow(not.Expression, alias, variables);
                return Negate(result);
            }
            case RowPresenceNode presence when IsAlias(presence.Expression, alias):
                return presence.IsPresent
                    ? MissingPredicateResult.FalseFor(presence)
                    : MissingPredicateResult.True;
            case IsNullNode isNull when IsOptionalReference(isNull.Expression, alias):
                return isNull.IsNegated
                    ? MissingPredicateResult.FalseFor(isNull)
                    : MissingPredicateResult.True;
            case LikeNode like when IsOptionalPatternOperand(like.Left, alias):
            case RLikeNode rlike when IsOptionalPatternOperand(rlike.Left, alias):
            case InNode @in when IsOptionalReference(@in.Left, alias):
            case BetweenNode between when IsOptionalReference(between.Expression, alias):
                return MissingPredicateResult.UnknownFor(node);
            case EqualityNode equality when IsOptionalComparison(equality, alias):
            case DiffNode diff when IsOptionalComparison(diff, alias):
            case GreaterNode greater when IsOptionalComparison(greater, alias):
            case GreaterOrEqualNode greaterOrEqual when IsOptionalComparison(greaterOrEqual, alias):
            case LessNode less when IsOptionalComparison(less, alias):
            case LessOrEqualNode lessOrEqual when IsOptionalComparison(lessOrEqual, alias):
                return MissingPredicateResult.UnknownFor(node);
            default:
                return MissingPredicateResult.Indeterminate;
        }
    }

    private static MissingPredicateResult Combine(
        MissingPredicateResult left,
        MissingPredicateResult right,
        BooleanOperator operation)
    {
        var values = MissingTruth.None;
        foreach (var leftValue in Expand(left.Values))
        foreach (var rightValue in Expand(right.Values))
            values |= EvaluateBoolean(leftValue, rightValue, operation);

        var rejectingNode = !CanBeTrue(values)
            ? left.RejectingNode ?? right.RejectingNode
            : null;
        return new MissingPredicateResult(values, rejectingNode);
    }

    private static MissingPredicateResult Negate(MissingPredicateResult result)
    {
        var values = MissingTruth.None;
        foreach (var value in Expand(result.Values))
            values |= value switch
            {
                MissingTruth.True => MissingTruth.False,
                MissingTruth.False => MissingTruth.True,
                _ => MissingTruth.Unknown
            };

        return new MissingPredicateResult(values, result.RejectingNode);
    }

    private static MissingTruth EvaluateBoolean(
        MissingTruth left,
        MissingTruth right,
        BooleanOperator operation)
    {
        if (operation == BooleanOperator.And)
        {
            if (left == MissingTruth.False || right == MissingTruth.False)
                return MissingTruth.False;
            return left == MissingTruth.True && right == MissingTruth.True
                ? MissingTruth.True
                : MissingTruth.Unknown;
        }

        if (left == MissingTruth.True || right == MissingTruth.True)
            return MissingTruth.True;
        return left == MissingTruth.False && right == MissingTruth.False
            ? MissingTruth.False
            : MissingTruth.Unknown;
    }

    private enum BooleanOperator
    {
        And,
        Or
    }

    private static IEnumerable<MissingTruth> Expand(MissingTruth values)
    {
        if ((values & MissingTruth.True) != 0)
            yield return MissingTruth.True;
        if ((values & MissingTruth.False) != 0)
            yield return MissingTruth.False;
        if ((values & MissingTruth.Unknown) != 0)
            yield return MissingTruth.Unknown;
    }

    private static bool CanBeTrue(MissingTruth values) => (values & MissingTruth.True) != 0;

    private static bool IsOptionalComparison(BinaryNode node, string alias)
    {
        return IsOptionalReference(node.Left, alias) || IsOptionalReference(node.Right, alias);
    }

    private static bool IsOptionalPatternOperand(Node node, string alias)
    {
        return IsOptionalReference(node, alias);
    }

    private static bool IsOptionalReference(Node node, string alias)
    {
        return node is AccessColumnNode column &&
               string.Equals(column.Alias, alias, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAlias(Node node, string alias)
    {
        return node is IdentifierNode identifier &&
               string.Equals(identifier.Name, alias, StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct OptionalAlias(string Name);

    [Flags]
    private enum MissingTruth
    {
        None = 0,
        Unknown = 1,
        True = 2,
        False = 4
    }

    private readonly record struct MissingPredicateResult(MissingTruth Truth, Node? RejectingNode)
    {
        public MissingTruth Values => Truth;

        public bool CanBeTrue => OuterJoinAdvisoryAnalyzer.CanBeTrue(Values);

        public static MissingPredicateResult True { get; } = new(MissingTruth.True, null);

        public static MissingPredicateResult FalseFor(Node node) => new(MissingTruth.False, node);

        public static MissingPredicateResult UnknownFor(Node node) => new(MissingTruth.Unknown, node);

        public static MissingPredicateResult Indeterminate { get; } = new(MissingTruth.Unknown | MissingTruth.True | MissingTruth.False, null);
    }

}
