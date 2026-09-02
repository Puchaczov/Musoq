using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical.Rewriting;

internal static partial class LogicalPlanRewriter
{
    public static LogicalNode RewriteChildren(LogicalNode node, Func<LogicalNode, LogicalNode> rewriteNode)
    {
        return node switch
        {
            AggregateNode aggregate => RewriteInput(
                aggregate.Input,
                rewriteNode,
                input => new AggregateNode(
                    aggregate.GroupKeys,
                    aggregate.GroupKeyNames,
                    aggregate.GroupKeyTypes,
                    aggregate.Bindings,
                    input),
                aggregate),
            FilterNode filter => RewriteInput(
                filter.Input,
                rewriteNode,
                input => new FilterNode(filter.Predicate, input),
                filter),
            HavingFilterNode having => RewriteInput(
                having.Input,
                rewriteNode,
                input => new HavingFilterNode(having.Predicate, input),
                having),
            ProjectNode project => RewriteInput(
                project.Input,
                rewriteNode,
                input => new ProjectNode(project.Fields, input) { IsDistinct = project.IsDistinct },
                project),
            QualifyFilterNode qualify => RewriteInput(
                qualify.Input,
                rewriteNode,
                input => new QualifyFilterNode(qualify.Predicate, input),
                qualify),
            SkipNode skip => RewriteInput(
                skip.Input,
                rewriteNode,
                input => new SkipNode(skip.Count, input),
                skip),
            SortNode sort => RewriteInput(
                sort.Input,
                rewriteNode,
                input => new SortNode(sort.Keys, input),
                sort),
            TakeNode take => RewriteInput(
                take.Input,
                rewriteNode,
                input => new TakeNode(take.Count, input),
                take),
            WindowNode window => RewriteInput(
                window.Input,
                rewriteNode,
                input => new WindowNode(window.Registrations, input),
                window),
            UnpivotNode unpivot => RewriteInput(
                unpivot.Source,
                rewriteNode,
                input => new UnpivotNode(
                    unpivot.Alias,
                    unpivot.NameColumn,
                    unpivot.ValueColumn,
                    unpivot.Entries,
                    unpivot.KeepFields,
                    input,
                    unpivot.OutputSchema),
                unpivot),
            ApplyNode apply => RewritePair(
                apply.Left,
                apply.Right,
                rewriteNode,
                (left, right) => new ApplyNode(apply.Kind, left, right, apply.WithOrdinality),
                apply),
            JoinNode join => RewritePair(
                join.Left,
                join.Right,
                rewriteNode,
                (left, right) => new JoinNode(join.Kind, join.OnPredicate, left, right, join.TieBreak, join.WithOrdinality),
                join),
            SetOperationNode setOperation => RewritePair(
                setOperation.Left,
                setOperation.Right,
                rewriteNode,
                (left, right) => new SetOperationNode(setOperation.Kind, left, right, setOperation.Keys),
                setOperation),
            RecursiveCteNode recursiveCte => RewritePair(
                recursiveCte.Anchor,
                recursiveCte.RecursiveMember,
                rewriteNode,
                (anchor, member) => new RecursiveCteNode(
                    recursiveCte.Name,
                    anchor,
                    member,
                    recursiveCte.UnionKind,
                    recursiveCte.Keys,
                    recursiveCte.IdentityFieldIndexes),
                recursiveCte),
            CteNode cte => RewriteCte(cte, rewriteNode),
            MultiStatementNode multiStatement => RewriteMultiStatement(multiStatement, rewriteNode),
            _ => node
        };
    }

    private static LogicalNode RewriteInput(
        LogicalNode input,
        Func<LogicalNode, LogicalNode> rewriteNode,
        Func<LogicalNode, LogicalNode> createNode,
        LogicalNode originalNode)
    {
        var rewrittenInput = rewriteNode(input);
        return ReferenceEquals(rewrittenInput, input)
            ? originalNode
            : createNode(rewrittenInput);
    }

    private static LogicalNode RewritePair(
        LogicalNode left,
        LogicalNode right,
        Func<LogicalNode, LogicalNode> rewriteNode,
        Func<LogicalNode, LogicalNode, LogicalNode> createNode,
        LogicalNode originalNode)
    {
        var rewrittenLeft = rewriteNode(left);
        var rewrittenRight = rewriteNode(right);
        return ReferenceEquals(rewrittenLeft, left) && ReferenceEquals(rewrittenRight, right)
            ? originalNode
            : createNode(rewrittenLeft, rewrittenRight);
    }

    private static CteNode RewriteCte(CteNode cte, Func<LogicalNode, LogicalNode> rewriteNode)
    {
        var definitions = new CteDefinition[cte.Definitions.Length];
        var changed = false;

        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = cte.Definitions[index];
            var plan = rewriteNode(definition.Plan);
            definitions[index] = ReferenceEquals(plan, definition.Plan)
                ? definition
                : definition with { Plan = plan };
            changed |= !ReferenceEquals(definitions[index], definition);
        }

        var query = rewriteNode(cte.Query);
        changed |= !ReferenceEquals(query, cte.Query);

        return changed ? new CteNode(definitions, query) : cte;
    }

    private static MultiStatementNode RewriteMultiStatement(
        MultiStatementNode multiStatement,
        Func<LogicalNode, LogicalNode> rewriteNode)
    {
        var statements = new LogicalNode[multiStatement.Statements.Length];
        var changed = false;

        for (var index = 0; index < statements.Length; index++)
        {
            statements[index] = rewriteNode(multiStatement.Statements[index]);
            changed |= !ReferenceEquals(statements[index], multiStatement.Statements[index]);
        }

        return changed ? new MultiStatementNode(statements) : multiStatement;
    }
}
