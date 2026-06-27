using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using Musoq.Plugins.Attributes;
using AliasRefExtractor = Musoq.Evaluator.IR.Expressions.AliasRefExtractor;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class PredicateMovementPhysicalPass : IPlanOptimizationPass<PhysicalNode>
{
    public string Name => "PredicateMovement";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var rewritten = Rewrite(plan);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(plan, "No physical predicate movements were applied.")
            : OptimizationResult<PhysicalNode>.Changed(rewritten, "Applied physical predicate movement filters to join inputs.");
    }

    private static PhysicalNode Rewrite(PhysicalNode node)
    {
        if (node is PhysicalJoinCandidateNode join)
        {
            var localPredicates = DiscoverLocalInnerJoinPredicates(join);
            var leftPredicates = MergePredicates(join.LeftMovedPredicates, localPredicates.Left);
            var rightPredicates = MergePredicates(join.RightMovedPredicates, localPredicates.Right);
            var left = ApplyPredicates(Rewrite(join.Left), leftPredicates);
            var right = ApplyPredicates(Rewrite(join.Right), rightPredicates);

            if (ReferenceEquals(left, join.Left) &&
                ReferenceEquals(right, join.Right) &&
                leftPredicates.Length == 0 &&
                rightPredicates.Length == 0)
            {
                return join;
            }

            return new PhysicalJoinCandidateNode(
                join.Kind,
                join.OnPredicate,
                left,
                right,
                join.LeftMovedPredicates,
                join.RightMovedPredicates,
                join.TieBreak);
        }

        return PhysicalPlanRewriter.RewriteChildren(node, Rewrite);
    }

    private static PhysicalNode ApplyPredicates(PhysicalNode input, IrExpression[] predicates)
    {
        var result = input;

        foreach (var predicate in predicates)
            result = ApplyPredicate(result, predicate);

        return result;
    }

    private static PhysicalNode ApplyPredicate(PhysicalNode input, IrExpression predicate)
    {
        return input switch
        {
            PhysicalFilterNode filter => new PhysicalFilterNode(
                filter.Predicate,
                ApplyPredicate(filter.Input, predicate)),
            PhysicalSortNode sort => new PhysicalSortNode(
                sort.Keys,
                ApplyPredicate(sort.Input, predicate)),
            PhysicalProjectNode { IsDistinct: false } project
                when ProjectPreservesPredicateColumns(project, predicate) => new PhysicalProjectNode(
                    project.Fields,
                    ApplyPredicate(project.Input, predicate)),
            _ => new PhysicalFilterNode(predicate, input)
        };
    }

    private static (IrExpression[] Left, IrExpression[] Right) DiscoverLocalInnerJoinPredicates(
        PhysicalJoinCandidateNode join)
    {
        if (join.Kind != JoinKind.Inner)
            return ([], []);

        var existingLeft = join.LeftMovedPredicates.Select(IrExpressionPrinter.Print).ToHashSet(StringComparer.Ordinal);
        var existingRight = join.RightMovedPredicates.Select(IrExpressionPrinter.Print).ToHashSet(StringComparer.Ordinal);
        var left = new List<IrExpression>();
        var right = new List<IrExpression>();

        foreach (var predicate in SplitConjuncts(join.OnPredicate))
        {
            if (!IsMovablePredicate(predicate))
                continue;

            var aliases = AliasRefExtractor.Extract(predicate).ToArray();
            if (aliases.Length != 1)
                continue;

            var predicateText = IrExpressionPrinter.Print(predicate);
            if (ProducesAlias(join.Left, aliases[0]) &&
                CanPushDiscoveredPredicate(join.Left, predicate) &&
                !existingLeft.Contains(predicateText))
            {
                left.Add(predicate);
            }
            else if (ProducesAlias(join.Right, aliases[0]) &&
                     CanPushDiscoveredPredicate(join.Right, predicate) &&
                     !existingRight.Contains(predicateText))
            {
                right.Add(predicate);
            }
        }

        return (left.ToArray(), right.ToArray());
    }

    private static IrExpression[] MergePredicates(
        IReadOnlyList<IrExpression> existing,
        IReadOnlyList<IrExpression> discovered)
    {
        if (discovered.Count == 0)
            return existing.ToArray();

        var result = new List<IrExpression>(existing.Count + discovered.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var predicate in existing.Concat(discovered))
        {
            if (seen.Add(IrExpressionPrinter.Print(predicate)))
                result.Add(predicate);
        }

        return result.ToArray();
    }

    private static IEnumerable<IrExpression> SplitConjuncts(IrExpression predicate)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } and)
        {
            foreach (var left in SplitConjuncts(and.Left))
                yield return left;

            foreach (var right in SplitConjuncts(and.Right))
                yield return right;

            yield break;
        }

        yield return predicate;
    }

    private static bool ProjectPreservesPredicateColumns(PhysicalProjectNode project, IrExpression predicate)
    {
        var refs = ColumnRefExtractor.Extract(predicate);
        return refs.Count > 0 &&
               refs.All(column => ProjectPreservesColumn(project.Fields, column));
    }

    private static bool ProjectPreservesColumn(
        IReadOnlyList<ProjectedField> fields,
        ColumnRef column)
    {
        return fields.Any(field =>
            field.Expression is ColumnRef projected &&
            string.Equals(projected.Alias, column.Alias, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(projected.ColumnName, column.ColumnName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ProducesAlias(PhysicalNode node, string alias)
    {
        return node switch
        {
            PhysicalSchemaScanNode scan => string.Equals(scan.Alias, alias, StringComparison.OrdinalIgnoreCase),
            PhysicalCteRefNode cteRef => string.Equals(cteRef.Alias, alias, StringComparison.OrdinalIgnoreCase),
            PhysicalValuesScanNode values => string.Equals(values.Alias, alias, StringComparison.OrdinalIgnoreCase),
            PhysicalUnpivotNode unpivot => string.Equals(unpivot.Alias, alias, StringComparison.OrdinalIgnoreCase),
            _ => node.Children.Any(child => ProducesAlias(child, alias))
        };
    }

    private static bool CanPushDiscoveredPredicate(PhysicalNode node, IrExpression predicate)
    {
        return node switch
        {
            PhysicalSchemaScanNode or PhysicalValuesScanNode => true,
            PhysicalFilterNode filter => CanPushDiscoveredPredicate(filter.Input, predicate),
            PhysicalSortNode sort => CanPushDiscoveredPredicate(sort.Input, predicate),
            PhysicalProjectNode { IsDistinct: false } project
                when ProjectPreservesPredicateColumns(project, predicate) =>
                CanPushDiscoveredPredicate(project.Input, predicate),
            _ => false
        };
    }

    private static bool IsMovablePredicate(IrExpression expression)
    {
        return expression.ReturnType == typeof(bool) && IsDeterministicExpression(expression);
    }

    private static bool IsDeterministicExpression(IrExpression expression)
    {
        return expression switch
        {
            Literal or WildcardLiteral or ColumnRef or RowPresence or ScriptParameterRef or ScriptVariableRef => true,
            BinaryOp binary => IsDeterministicExpression(binary.Left) &&
                               IsDeterministicExpression(binary.Right),
            UnaryOp unary => IsDeterministicExpression(unary.Operand),
            MethodCall methodCall => IsDeterministicMethod(methodCall.Method) &&
                                     methodCall.Arguments.All(IsDeterministicExpression),
            IsNullCheck isNull => IsDeterministicExpression(isNull.Expression),
            InCheck inCheck => IsDeterministicExpression(inCheck.Expression) &&
                               inCheck.Values.All(IsDeterministicExpression),
            PatternMatch patternMatch => IsDeterministicExpression(patternMatch.Expression) &&
                                         IsDeterministicExpression(patternMatch.Pattern),
            Between between => IsDeterministicExpression(between.Expression) &&
                               IsDeterministicExpression(between.Low) &&
                               IsDeterministicExpression(between.High),
            CaseWhen caseWhen => caseWhen.Branches.All(static branch =>
                                     IsDeterministicExpression(branch.Condition) &&
                                     IsDeterministicExpression(branch.Result)) &&
                                 (caseWhen.ElseExpression == null ||
                                  IsDeterministicExpression(caseWhen.ElseExpression)),
            Coalesce coalesce => coalesce.Expressions.All(IsDeterministicExpression),
            ArrayAccess arrayAccess => IsDeterministicExpression(arrayAccess.Array) &&
                                       IsDeterministicExpression(arrayAccess.Index),
            AggregateRef or WindowFunctionRef or CteTableRef => false,
            _ => false
        };
    }

    private static bool IsDeterministicMethod(MethodInfo method)
    {
        return method.GetCustomAttribute<NonDeterministicAttribute>() == null &&
               method.GetParameters().All(static parameter =>
                   parameter.GetCustomAttribute<InjectQueryStatsAttribute>() == null &&
                   parameter.GetCustomAttribute<InjectTypeAttribute>() == null);
    }
}
