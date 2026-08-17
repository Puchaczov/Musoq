using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Checks the binding contract at the last target-neutral boundary.  A renderer
/// must never have to guess how a field is read when the field belongs to a
/// known row shape.
/// </summary>
internal static class ExecutionBindingInvariantValidator
{
    public static void Validate(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var shapesByAlias = plan.Shapes
            .Select(static shape => (Shape: shape, HasAlias: RowShapeLookup.TryResolveSourceAlias(shape, out var alias), Alias: alias))
            .Where(static item => item.HasAlias)
            .GroupBy(static item => item.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static item => item.Shape).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var fieldRead in ExecutionIrAnalysis.CollectExpressions<ExecutionFieldRead>(plan.Body))
        {
            if (string.IsNullOrWhiteSpace(fieldRead.Alias) ||
                !shapesByAlias.TryGetValue(fieldRead.Alias, out var shapes))
                continue;

            var accessStrategy = fieldRead.AccessStrategy;
            if (accessStrategy == null)
            {
                if (shapes.Any(shape => IsUnresolvedIndexedField(shape, fieldRead.FieldName)))
                {
                    throw new InvalidOperationException(
                        $"Execution binding invariant violated: field '{fieldRead.Alias}.{fieldRead.FieldName}' " +
                        "belongs to a known row carrier but has no access strategy. Lowering must resolve the " +
                        "carrier-specific binding before rendering.");
                }

                continue;
            }

            foreach (var shape in shapes)
            {
                if (TryFindField(shape, fieldRead.FieldName, out var field) &&
                    !IsCompatible(field.AccessStrategy, accessStrategy))
                {
                    throw new InvalidOperationException(
                        $"Execution binding invariant violated: field '{fieldRead.Alias}.{fieldRead.FieldName}' " +
                        $"uses '{accessStrategy.GetType().Name}' for carrier '{shape.GetType().Name}', " +
                        $"but its declared binding is '{field.AccessStrategy.GetType().Name}'.");
                }
            }
        }
    }

    private static bool TryFindField(RowShape shape, string fieldName, out FieldBinding field)
    {
        var candidates = shape.Fields.Concat(shape is TableRowShape table ? table.Contexts : []);
        field = candidates.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, fieldName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, fieldName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, GetUnqualifiedName(fieldName), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, GetUnqualifiedName(fieldName), StringComparison.OrdinalIgnoreCase))!;
        return field != null;
    }

    private static bool IsUnresolvedIndexedField(RowShape shape, string fieldName)
    {
        if (TryFindField(shape, fieldName, out var field))
            return field.AccessStrategy is PositionalAccess or NestedPositionalAccess;

        return !fieldName.Contains('.', StringComparison.Ordinal) &&
               !fieldName.Contains('[', StringComparison.Ordinal) &&
               shape.Fields.Any(static candidate => candidate.AccessStrategy is PositionalAccess);
    }

    private static bool IsCompatible(FieldAccessStrategy declared, FieldAccessStrategy actual)
    {
        return declared switch
        {
            PositionalAccess => actual is not
                (ClrPropertyAccess or ReflectedMemberAccess or ExpandoDictionaryAccess or RuntimeDynamicMemberAccess),
            NestedPositionalAccess => actual is not
                (NestedClrPropertyAccess or ReflectedMemberAccess or ExpandoDictionaryAccess or RuntimeDynamicMemberAccess),
            _ => true
        };
    }

    private static string GetUnqualifiedName(string name)
    {
        var separatorIndex = name.LastIndexOf('.');
        return separatorIndex < 0 ? name : name[(separatorIndex + 1)..];
    }
}
