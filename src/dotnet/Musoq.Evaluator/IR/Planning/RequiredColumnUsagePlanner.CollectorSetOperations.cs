using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnUsagePlanner
{
    private sealed partial class RequiredColumnUsageCollector
    {
        private void AddSetOperationKeys(SetOperationNode setOperation)
        {
            var keyNames = ResolveSetOperationKeyNames(setOperation);
            if (keyNames.Length == 0)
                return;

            var keys = new HashSet<string>(keyNames, StringComparer.OrdinalIgnoreCase);
            AddSetOperationKeys(setOperation.Left, keys, SetOperationKeyMatchMode.IncludeAllProjectedFields);
            AddSetOperationKeys(setOperation.Right, keys, SetOperationKeyMatchMode.IncludeAllProjectedFields);
        }

        private static string[] ResolveSetOperationKeyNames(SetOperationNode setOperation)
        {
            return setOperation.Keys.Length > 0
                ? setOperation.Keys
                : setOperation.OutputSchema.Columns.Select(static column => column.Name).ToArray();
        }

        private void AddSetOperationKeys(
            LogicalNode node,
            IReadOnlySet<string> keys,
            SetOperationKeyMatchMode keyMatchMode)
        {
            switch (node)
            {
                case ProjectNode project:
                    AddProjectSetOperationKeys(project, keys, keyMatchMode);
                    AddSetOperationKeys(project.Input, keys, keyMatchMode);
                    break;
                case FilterNode filter:
                    AddSetOperationKeys(filter.Input, keys, keyMatchMode);
                    break;
                case HavingFilterNode having:
                    AddSetOperationKeys(having.Input, keys, keyMatchMode);
                    break;
                case QualifyFilterNode qualify:
                    AddSetOperationKeys(qualify.Input, keys, keyMatchMode);
                    break;
                case SortNode sort:
                    AddSetOperationKeys(sort.Input, keys, keyMatchMode);
                    break;
                case SkipNode skip:
                    AddSetOperationKeys(skip.Input, keys, keyMatchMode);
                    break;
                case TakeNode take:
                    AddSetOperationKeys(take.Input, keys, keyMatchMode);
                    break;
                default:
                    foreach (var child in node.Children)
                        AddSetOperationKeys(child, keys, keyMatchMode);
                    break;
            }
        }

        private void AddProjectSetOperationKeys(
            ProjectNode project,
            IReadOnlySet<string> keys,
            SetOperationKeyMatchMode keyMatchMode)
        {
            foreach (var field in project.Fields)
            {
                if (ShouldAddSetOperationKey(field, keys, keyMatchMode))
                    AddExpression(field.Expression, RequiredColumnUsageReason.SetOperationKey);
            }
        }

        private static bool ShouldAddSetOperationKey(
            ProjectedField field,
            IReadOnlySet<string> keys,
            SetOperationKeyMatchMode keyMatchMode)
        {
            return keyMatchMode == SetOperationKeyMatchMode.IncludeAllProjectedFields || keys.Contains(field.OutputName);
        }

        private void AddExpressions(
            IReadOnlyList<IrExpression> expressions,
            RequiredColumnUsageReason reason)
        {
            foreach (var expression in expressions)
                AddExpression(expression, reason);
        }

        private void AddExpression(IrExpression? expression, RequiredColumnUsageReason reason)
        {
            if (expression == null)
                return;

            foreach (var column in ColumnRefExtractor.Extract(expression))
            {
                if (string.IsNullOrWhiteSpace(column.Alias) || string.IsNullOrWhiteSpace(column.ColumnName))
                    continue;

                AddColumn(column.Alias, column.ColumnName, reason);
            }
        }
    }
}
