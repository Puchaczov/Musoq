using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static bool TryRewriteFinalAggregateProjection(
        PhysicalProjectNode project,
        IReadOnlyList<PostOperation> postOperations,
        AggregateBinding[] bindings,
        AggregateFinalizationGroupKeys groupKeys,
        out PhysicalProjectNode rewrittenProject,
        out IReadOnlyList<PostOperation> rewrittenPostOperations)
    {
        rewrittenProject = RewriteFinalAggregateProjection(project, bindings);
        rewrittenPostOperations = RewriteFinalAggregatePostOperations(postOperations, bindings);

        if (groupKeys.Expressions.Count == 0 && rewrittenProject.Fields.Length != bindings.Length)
            return false;

        var bindingsByIdentifier = CreateAggregateBindingsMap(bindings);
        if (!CanFinalizeAggregateFields(rewrittenProject.Fields, groupKeys, bindings, bindingsByIdentifier) ||
            !CanFinalizeAggregatePostOperations(rewrittenPostOperations, groupKeys, bindings, bindingsByIdentifier))
        {
            return false;
        }

        return CollectAggregateFinalSourceColumns(
            rewrittenProject.Fields,
            null,
            rewrittenPostOperations,
            groupKeys).Length == 0;
    }

    private static PhysicalProjectNode RewriteFinalAggregateProjection(
        PhysicalProjectNode project,
        IReadOnlyList<AggregateBinding> bindings)
    {
        return project with
        {
            Fields = RewriteFinalAggregateProjectedFields(project.Fields, bindings)
        };
    }

    private static IReadOnlyList<PostOperation> RewriteFinalAggregatePostOperations(
        IReadOnlyList<PostOperation> postOperations,
        IReadOnlyList<AggregateBinding> bindings)
    {
        if (postOperations.Count == 0)
            return postOperations;

        return postOperations
            .Select(operation => operation switch
            {
                SortOperation sort => sort with
                {
                    Keys = RewriteFinalAggregateOrderFields(sort.Keys, bindings),
                    ProjectedFields = RewriteFinalAggregateProjectedFields(sort.ProjectedFields, bindings)
                },
                TopNOperation topN => topN with
                {
                    Keys = RewriteFinalAggregateOrderFields(topN.Keys, bindings),
                    ProjectedFields = RewriteFinalAggregateProjectedFields(topN.ProjectedFields, bindings)
                },
                TopOffsetOperation topOffset => topOffset with
                {
                    Keys = RewriteFinalAggregateOrderFields(topOffset.Keys, bindings),
                    ProjectedFields = RewriteFinalAggregateProjectedFields(topOffset.ProjectedFields, bindings)
                },
                _ => operation
            })
            .ToArray();
    }

    private static AggregateBinding[]? RewriteAggregateBindings(
        AggregateBinding[] bindings,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var rewritten = new AggregateBinding[bindings.Length];

        for (var index = 0; index < bindings.Length; index++)
        {
            var binding = bindings[index];
            var setArguments = RewriteFinalJoinExpressions(binding.SetArguments, projectedExpressions, cteRef);
            var getArguments = RewriteFinalJoinExpressions(binding.GetArguments, projectedExpressions, cteRef);
            if (setArguments == null || getArguments == null)
                return null;

            rewritten[index] = binding with
            {
                SetArguments = setArguments.ToArray(),
                GetArguments = getArguments.ToArray()
            };
        }

        return rewritten;
    }

    private static PhysicalProjectNode? RewriteAggregateProject(
        PhysicalProjectNode project,
        IReadOnlyDictionary<string, IrExpression> projectedExpressions,
        PhysicalCteRefNode cteRef)
    {
        var fields = new ProjectedField[project.Fields.Length];

        for (var index = 0; index < fields.Length; index++)
        {
            var field = project.Fields[index];
            var expression = field.Expression is AggregateRef
                ? field.Expression
                : RewriteFinalJoinExpression(field.Expression, projectedExpressions, cteRef);
            if (expression == null)
                return null;

            fields[index] = field with { Expression = expression };
        }

        return project with { Fields = fields };
    }

    private static ProjectedField[] RewriteFinalAggregateProjectedFields(
        IReadOnlyList<ProjectedField> fields,
        IReadOnlyList<AggregateBinding> bindings)
    {
        var bindingsByIdentifier = CreateAggregateBindingsMap(bindings);

        return fields
            .Select(field => field with
            {
                Expression = AggregateRefRewriter.Rewrite(field.Expression, bindingsByIdentifier)
            })
            .ToArray();
    }

    private static OrderField[] RewriteFinalAggregateOrderFields(
        IReadOnlyList<OrderField> fields,
        IReadOnlyList<AggregateBinding> bindings)
    {
        var bindingsByIdentifier = CreateAggregateBindingsMap(bindings);

        return fields
            .Select(field => field with
            {
                Expression = AggregateRefRewriter.Rewrite(field.Expression, bindingsByIdentifier)
            })
            .ToArray();
    }

    private static bool CanFinalizeAggregateFields(
        IReadOnlyList<ProjectedField> fields,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        return fields.All(field => CanFinalizeAggregateExpression(
            field.Expression,
            groupKeys,
            bindings,
            bindingsByIdentifier));
    }

    private static bool CanFinalizeAggregatePostOperations(
        IReadOnlyList<PostOperation> postOperations,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        foreach (var postOperation in postOperations)
        {
            switch (postOperation)
            {
                case SortOperation sort:
                    if (!CanFinalizeAggregateOrderFields(sort.Keys, groupKeys, bindings, bindingsByIdentifier) ||
                        !CanFinalizeAggregateFields(sort.ProjectedFields, groupKeys, bindings, bindingsByIdentifier))
                        return false;
                    break;
                case TopNOperation topN:
                    if (!CanFinalizeAggregateOrderFields(topN.Keys, groupKeys, bindings, bindingsByIdentifier) ||
                        !CanFinalizeAggregateFields(topN.ProjectedFields, groupKeys, bindings, bindingsByIdentifier))
                        return false;
                    break;
                case TopOffsetOperation topOffset:
                    if (!CanFinalizeAggregateOrderFields(topOffset.Keys, groupKeys, bindings, bindingsByIdentifier) ||
                        !CanFinalizeAggregateFields(topOffset.ProjectedFields, groupKeys, bindings, bindingsByIdentifier))
                        return false;
                    break;
            }
        }

        return true;
    }

    private static bool CanFinalizeAggregateOrderFields(
        IReadOnlyList<OrderField> fields,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        return fields.All(field => CanFinalizeAggregateExpression(
            field.Expression,
            groupKeys,
            bindings,
            bindingsByIdentifier));
    }

    private static bool CanFinalizeAggregateExpression(
        IrExpression expression,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        if (TryGetGroupKeyExpressionIndex(expression, groupKeys) != null)
            return true;

        return expression switch
        {
            Literal or WildcardLiteral or ScriptParameterRef => true,
            BinaryOp binary => CanFinalizeAggregateExpression(binary.Left, groupKeys, bindings, bindingsByIdentifier) &&
                               CanFinalizeAggregateExpression(binary.Right, groupKeys, bindings, bindingsByIdentifier),
            UnaryOp unary => CanFinalizeAggregateExpression(unary.Operand, groupKeys, bindings, bindingsByIdentifier),
            AggregateRef aggregateRef => TryResolveAggregateBinding(aggregateRef.Identifier, bindingsByIdentifier, out _),
            ColumnRef columnRef => TryResolveAggregateBinding(
                string.IsNullOrWhiteSpace(columnRef.Alias)
                    ? columnRef.ColumnName
                    : $"{columnRef.Alias}.{columnRef.ColumnName}",
                bindingsByIdentifier,
                out _) ||
                TryResolveAggregateBinding(columnRef.ColumnName, bindingsByIdentifier, out _),
            MethodCall methodCall => CanFinalizeAggregateMethodCall(methodCall, groupKeys, bindings, bindingsByIdentifier),
            _ => false
        };
    }

    private static bool CanFinalizeAggregateMethodCall(
        MethodCall methodCall,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        if (TryGetGroupKeyExpressionIndex(methodCall, groupKeys) != null)
            return true;

        if (IsRowNumberMethod(methodCall.Method))
            return false;

        if (TryResolveProjectedAggregate(methodCall, bindings, bindingsByIdentifier, out _))
            return true;

        return !RequiresSourceInjection(methodCall.Method) &&
               methodCall.Arguments.All(argument => CanFinalizeAggregateExpression(
                   argument,
                   groupKeys,
                   bindings,
                   bindingsByIdentifier));
    }
}
