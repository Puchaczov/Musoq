using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Execution;

internal static class HashJoinAliasResolver
{
    public static (bool CanBindLeft, bool CanBindRight) GetUsage(
        IReadOnlyList<IrExpression> keys,
        RowShape left,
        RowShape right)
    {
        var referencesLeft = false;
        var referencesRight = false;
        var leftPrimaryAliases = CollectPrimaryAliases(left);
        var rightPrimaryAliases = CollectPrimaryAliases(right);
        var leftAliases = CollectBindableAliases(left);
        var rightAliases = CollectBindableAliases(right);

        foreach (var key in keys)
        {
            var columns = ColumnRefExtractor.Extract(key);
            var referencesLeftPrimary = ReferencesAliases(columns, leftPrimaryAliases);
            var referencesRightPrimary = ReferencesAliases(columns, rightPrimaryAliases);

            if (referencesLeftPrimary || referencesRightPrimary)
            {
                referencesLeft |= referencesLeftPrimary;
                referencesRight |= referencesRightPrimary;
                continue;
            }

            referencesLeft |= ReferencesAliases(columns, leftAliases);
            referencesRight |= ReferencesAliases(columns, rightAliases);
        }

        var referencesNeitherSide = !referencesLeft && !referencesRight;
        return (
            referencesNeitherSide || referencesLeft && !referencesRight,
            referencesNeitherSide || referencesRight && !referencesLeft);
    }

    private static HashSet<string> CollectPrimaryAliases(RowShape shape)
    {
        var aliases = CreateAliasSet();
        var sourceAlias = RowShapeLookup.ResolveSourceAlias(shape);
        if (!string.IsNullOrWhiteSpace(sourceAlias))
            aliases.Add(sourceAlias);
        return aliases;
    }

    private static HashSet<string> CollectBindableAliases(RowShape shape)
    {
        var aliases = CollectPrimaryAliases(shape);
        if (shape is not TableRowShape)
            return aliases;

        foreach (var field in shape.Fields)
        {
            AddQualifiedAlias(field.Name, aliases);
            AddQualifiedAlias(field.QualifiedName, aliases);
        }

        return aliases;
    }

    private static void AddQualifiedAlias(string name, HashSet<string> aliases)
    {
        var separatorIndex = name.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex > 0)
            aliases.Add(name[..separatorIndex]);
    }

    private static bool ReferencesAliases(IReadOnlyList<ColumnRef> columns, HashSet<string> aliases)
    {
        return columns.Any(column => aliases.Contains(column.Alias));
    }

    private static HashSet<string> CreateAliasSet()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
