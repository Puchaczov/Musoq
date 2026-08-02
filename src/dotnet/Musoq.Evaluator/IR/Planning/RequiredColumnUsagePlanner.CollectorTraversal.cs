using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnUsagePlanner
{
    private sealed partial class RequiredColumnUsageCollector
    {
        public void Collect(LogicalNode node)
        {
            if (node is CteNode cte)
            {
                Collect(cte.Query);

                var materializedDefinitions = cte.Definitions
                    .Where(definition => RequiresMaterializedDefinition(cte, definition.Name))
                    .Select(static definition => definition.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var definition in cte.Definitions)
                    CollectCteDefinition(definition, materializedDefinitions.Contains(definition.Name));

                return;
            }

            AddNodeUsages(node);

            foreach (var child in node.Children)
                Collect(child);
        }

        public RequiredColumnUsageResult CreateResult()
        {
            var usagesBySourceId = CreateUsagesBySourceId();
            var requiredColumnsByAlias = CreateRequiredColumnsByAlias();
            var decisions = CreateDecisions(usagesBySourceId);

            return new RequiredColumnUsageResult(requiredColumnsByAlias, usagesBySourceId, decisions);
        }

        private void AddNodeUsages(LogicalNode node)
        {
            switch (node)
            {
                case SchemaScanNode scan:
                    AddExpressions(scan.Arguments, RequiredColumnUsageReason.SourceArgument);
                    break;
                case InterpretSourceNode interpret:
                    AddExpressions(interpret.Arguments, RequiredColumnUsageReason.SourceArgument);
                    break;
                case AccessMethodSourceNode accessMethod:
                    AddExpression(accessMethod.MethodCallExpression, RequiredColumnUsageReason.ApplyCorrelation);
                    break;
                case PropertySourceNode propertySource:
                    AddPropertySource(propertySource);
                    break;
                case FilterNode filter:
                    AddExpression(filter.Predicate, RequiredColumnUsageReason.Where);
                    break;
                case ProjectNode project:
                    AddProjectedFields(project.Fields);
                    break;
                case HavingFilterNode having:
                    AddExpression(having.Predicate, RequiredColumnUsageReason.Having);
                    break;
                case QualifyFilterNode qualify:
                    AddExpression(qualify.Predicate, RequiredColumnUsageReason.Qualify);
                    break;
                case SortNode sort:
                    AddOrderFields(sort.Keys, RequiredColumnUsageReason.OrderBy);
                    break;
                case UnpivotNode unpivot:
                    AddUnpivotExpressions(unpivot);
                    break;
                case AggregateNode aggregate:
                    AddExpressions(aggregate.GroupKeys, RequiredColumnUsageReason.GroupBy);
                    AddAggregateBindings(aggregate.Bindings);
                    break;
                case JoinNode join:
                    AddExpression(join.OnPredicate, RequiredColumnUsageReason.JoinPredicate);
                    if (join.TieBreak != null)
                        AddExpression(join.TieBreak.Expression, RequiredColumnUsageReason.JoinPredicate);
                    break;
                case WindowNode window:
                    AddWindowRegistrations(window.Registrations);
                    break;
                case SetOperationNode setOperation:
                    AddSetOperationKeys(setOperation);
                    break;
            }
        }

        private void AddProjectedFields(IReadOnlyList<ProjectedField> fields)
        {
            foreach (var field in fields)
                AddExpression(field.Expression, ResolveProjectionReason(field));
        }

        private void AddProjectedFields(
            IReadOnlyList<ProjectedField> fields,
            IReadOnlySet<string> requiredOutputColumns)
        {
            foreach (var field in fields)
            {
                if (ContainsOutputColumn(requiredOutputColumns, field.OutputName))
                    AddExpression(field.Expression, ResolveProjectionReason(field));
            }
        }

        private void CollectCteDefinition(CteDefinition definition, bool materialized)
        {
            if (!materialized &&
                _requiredColumnsByCteName.TryGetValue(definition.Name, out var requiredColumns) &&
                requiredColumns.Count > 0)
            {
                CollectCteDefinitionPlan(definition.Plan, requiredColumns);
                return;
            }

            Collect(definition.Plan);
        }

        private static bool RequiresMaterializedDefinition(CteNode cte, string cteName)
        {
            return CountCteReferences(cte, cteName) != 1 ||
                   !IsTerminalReadOnceReference(cte.Query, cteName);
        }

        private static int CountCteReferences(LogicalNode node, string cteName)
        {
            var count = node is CteRefNode reference &&
                        string.Equals(reference.CteName, cteName, StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;

            foreach (var child in node.Children)
                count += CountCteReferences(child, cteName);

            return count;
        }

        private static bool IsTerminalReadOnceReference(LogicalNode node, string cteName)
        {
            if (node is MultiStatementNode { Statements.Length: 1 } multiStatement)
                return IsTerminalReadOnceReference(multiStatement.Statements[0], cteName);

            return node is ProjectNode { IsDistinct: false, Input: CteRefNode reference } &&
                   string.Equals(reference.CteName, cteName, StringComparison.OrdinalIgnoreCase);
        }

        private void CollectCteDefinitionPlan(
            LogicalNode node,
            IReadOnlySet<string> requiredOutputColumns)
        {
            if (node is MultiStatementNode { Statements.Length: 1 } multiStatement)
            {
                CollectCteDefinitionPlan(multiStatement.Statements[0], requiredOutputColumns);
                return;
            }

            if (node is ProjectNode { IsDistinct: false } project)
            {
                AddProjectedFields(project.Fields, requiredOutputColumns);
                Collect(project.Input);
                return;
            }

            Collect(node);
        }

        private static bool ContainsOutputColumn(
            IReadOnlySet<string> requiredOutputColumns,
            string outputName)
        {
            if (requiredOutputColumns.Contains(outputName))
                return true;

            return requiredOutputColumns.Any(required =>
                outputName.EndsWith($".{required}", StringComparison.OrdinalIgnoreCase) ||
                required.EndsWith($".{outputName}", StringComparison.OrdinalIgnoreCase));
        }

        private static RequiredColumnUsageReason ResolveProjectionReason(ProjectedField field)
        {
            return field.OutputName.StartsWith("__", StringComparison.Ordinal)
                ? RequiredColumnUsageReason.HiddenIntermediateProjection
                : RequiredColumnUsageReason.Projection;
        }

        private void AddOrderFields(
            IReadOnlyList<OrderField> fields,
            RequiredColumnUsageReason reason)
        {
            foreach (var field in fields)
                AddExpression(field.Expression, reason);
        }

        private void AddAggregateBindings(IReadOnlyList<AggregateBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                AddExpressions(binding.SetArguments, RequiredColumnUsageReason.AggregateSetArgument);
                if (binding.FilterPredicate != null)
                    AddExpression(binding.FilterPredicate, RequiredColumnUsageReason.AggregateSetArgument);
                AddExpressions(binding.GetArguments, RequiredColumnUsageReason.AggregateGetArgument);
            }
        }

        private void AddWindowRegistrations(IReadOnlyList<WindowRegistration> registrations)
        {
            foreach (var registration in registrations)
            {
                AddExpressions(registration.PartitionKeys, RequiredColumnUsageReason.WindowPartition);
                AddOrderFields(registration.OrderKeys, RequiredColumnUsageReason.WindowOrder);
                AddExpressions(registration.ValueArguments, RequiredColumnUsageReason.WindowValue);
                if (registration.FilterPredicate != null)
                    AddExpression(registration.FilterPredicate, RequiredColumnUsageReason.WindowValue);
            }
        }

        private void AddPropertySource(PropertySourceNode propertySource)
        {
            if (propertySource.PropertiesChain.Length == 0)
                return;

            AddColumn(
                propertySource.SourceAlias,
                propertySource.PropertiesChain[0].PropertyName,
                RequiredColumnUsageReason.ApplyCorrelation);
        }

    }
}
