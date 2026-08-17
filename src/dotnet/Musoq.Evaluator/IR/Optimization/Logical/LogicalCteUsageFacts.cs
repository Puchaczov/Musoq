using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal static partial class LogicalCteUsageFacts
{
    public static bool ContainsPlanningSensitiveSource(LogicalNode node)
    {
        if (node is AccessMethodSourceNode or DescNode or InterpretSourceNode or PropertySourceNode or SchemaScanNode)
            return true;

        foreach (var child in node.Children)
        {
            if (ContainsPlanningSensitiveSource(child))
                return true;
        }

        return false;
    }

    public static IEnumerable<string> CollectCteReferences(LogicalNode node)
    {
        if (node is CteRefNode cteRef)
            yield return cteRef.CteName;

        foreach (var expression in CollectLocalExpressions(node))
        {
            foreach (var reference in CteTableRefCollector.Collect(expression))
                yield return reference;
        }

        foreach (var child in node.Children)
        {
            foreach (var reference in CollectCteReferences(child))
                yield return reference;
        }
    }

    public static IEnumerable<IrExpression> CollectLocalExpressions(LogicalNode node)
    {
        switch (node)
        {
            case AccessMethodSourceNode accessMethod:
                yield return accessMethod.MethodCallExpression;
                break;

            case AggregateNode aggregate:
                foreach (var groupKey in aggregate.GroupKeys)
                    yield return groupKey;

                foreach (var binding in aggregate.Bindings)
                {
                    foreach (var argument in binding.SetArguments)
                        yield return argument;

                    if (binding.FilterPredicate != null)
                        yield return binding.FilterPredicate;

                    foreach (var argument in binding.GetArguments)
                        yield return argument;
                }

                break;

            case DescNode desc:
                foreach (var argument in desc.Arguments)
                    yield return argument;

                break;

            case FilterNode filter:
                yield return filter.Predicate;
                break;

            case HavingFilterNode having:
                yield return having.Predicate;
                break;

            case InterpretSourceNode interpret:
                foreach (var argument in interpret.Arguments)
                    yield return argument;

                break;

            case JoinNode join:
                yield return join.OnPredicate;
                break;

            case ProjectNode project:
                foreach (var field in project.Fields)
                    yield return field.Expression;

                break;

            case QualifyFilterNode qualify:
                yield return qualify.Predicate;
                break;

            case SchemaScanNode scan:
                foreach (var argument in scan.Arguments)
                    yield return argument;

                break;

            case SortNode sort:
                foreach (var key in sort.Keys)
                    yield return key.Expression;

                break;

            case ValuesScanNode values:
                foreach (var row in values.Rows)
                {
                    foreach (var field in row.Fields)
                        yield return field.Value;
                }

                break;

            case WindowNode window:
                foreach (var registration in window.Registrations)
                {
                    foreach (var partitionKey in registration.PartitionKeys)
                        yield return partitionKey;

                    foreach (var orderKey in registration.OrderKeys)
                        yield return orderKey.Expression;

                    foreach (var argument in registration.ValueArguments)
                        yield return argument;

                    if (registration.FilterPredicate != null)
                        yield return registration.FilterPredicate;
                }

                break;
        }
    }

    private sealed partial class CteTableRefCollector : IrExpressionVisitor<IReadOnlyList<string>>
    {
        private readonly List<string> _references = [];

        public static IReadOnlyList<string> Collect(IrExpression expression)
        {
            var collector = new CteTableRefCollector();
            collector.Visit(expression);
            return collector._references;
        }

        protected override IReadOnlyList<string> VisitColumnRef(ColumnRef node) => _references;

        protected override IReadOnlyList<string> VisitScriptParameterRef(ScriptParameterRef node) => _references;

        protected override IReadOnlyList<string> VisitScriptVariableRef(ScriptVariableRef node) => _references;

        protected override IReadOnlyList<string> VisitLiteral(Literal node) => _references;

        protected override IReadOnlyList<string> VisitWildcardLiteral(WildcardLiteral node) => _references;

        protected override IReadOnlyList<string> VisitBinaryOp(BinaryOp node)
        {
            Visit(node.Left);
            Visit(node.Right);
            return _references;
        }

        protected override IReadOnlyList<string> VisitUnaryOp(UnaryOp node)
        {
            Visit(node.Operand);
            return _references;
        }

        protected override IReadOnlyList<string> VisitMethodCall(MethodCall node)
        {
            foreach (var argument in node.Arguments)
                Visit(argument);

            return _references;
        }

        protected override IReadOnlyList<string> VisitStrictCast(StrictCast node)
        {
            Visit(node.Expression);
            return _references;
        }

        protected override IReadOnlyList<string> VisitIsNullCheck(IsNullCheck node)
        {
            Visit(node.Expression);
            return _references;
        }

        protected override IReadOnlyList<string> VisitRowPresence(RowPresence node) => _references;

        protected override IReadOnlyList<string> VisitInCheck(InCheck node)
        {
            Visit(node.Expression);
            foreach (var value in node.Values)
                Visit(value);

            return _references;
        }

        protected override IReadOnlyList<string> VisitPatternMatch(PatternMatch node)
        {
            Visit(node.Expression);
            Visit(node.Pattern);
            return _references;
        }

        protected override IReadOnlyList<string> VisitBetween(Between node)
        {
            Visit(node.Expression);
            Visit(node.Low);
            Visit(node.High);
            return _references;
        }

        protected override IReadOnlyList<string> VisitCaseWhen(CaseWhen node)
        {
            foreach (var branch in node.Branches)
            {
                Visit(branch.Condition);
                Visit(branch.Result);
            }

            if (node.ElseExpression is not null)
                Visit(node.ElseExpression);

            return _references;
        }

        protected override IReadOnlyList<string> VisitCoalesce(Coalesce node)
        {
            foreach (var expression in node.Expressions)
                Visit(expression);

            return _references;
        }

        protected override IReadOnlyList<string> VisitAggregateRef(AggregateRef node) => _references;

        protected override IReadOnlyList<string> VisitWindowFunctionRef(WindowFunctionRef node) => _references;

        protected override IReadOnlyList<string> VisitArrayAccess(ArrayAccess node)
        {
            Visit(node.Array);
            Visit(node.Index);
            return _references;
        }

        protected override IReadOnlyList<string> VisitCteTableRef(CteTableRef node)
        {
            _references.Add(node.Name);
            return _references;
        }
    }
}

