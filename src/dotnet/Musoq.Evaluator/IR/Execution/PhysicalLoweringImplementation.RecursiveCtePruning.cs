using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static void AddRecursiveCteRequiredColumns(
        PhysicalCteNode cte,
        IReadOnlyDictionary<string, HashSet<string>> required)
    {
        foreach (var definition in cte.Definitions)
        {
            if (definition.Plan is not PhysicalRecursiveCteNode recursive ||
                !required.TryGetValue(definition.Name, out var columns))
            {
                continue;
            }

            foreach (var key in recursive.Keys)
                columns.Add(key);

            foreach (var invariant in recursive.Invariants)
            {
                if (invariant.ExistingCteName == null ||
                    !required.TryGetValue(invariant.ExistingCteName, out var invariantColumns))
                {
                    continue;
                }

                foreach (var field in invariant.Fields)
                    invariantColumns.Add(GetColumnRoot(field.OutputName));
            }

            if (recursive.UnionKind == RecursiveCteUnionKind.FullRow)
            {
                AddAllOutputColumns(recursive, columns);
                continue;
            }

            if (!TryGetRecursiveOutputProjects(recursive, out var anchor, out var member) ||
                anchor.Fields.Length != member.Fields.Length)
            {
                AddAllOutputColumns(recursive, columns);
                continue;
            }

            var selfAliases = CollectCteRefsByAlias(
                    recursive.RecursiveMember,
                    new HashSet<string>([recursive.Name], StringComparer.OrdinalIgnoreCase))
                .Keys
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            AddMandatorySelfColumns(
                recursive.RecursiveMember,
                member,
                selfAliases,
                recursive.OutputSchema,
                columns);

            var changed = true;
            while (changed)
            {
                changed = false;
                for (var index = 0; index < anchor.Fields.Length; index++)
                {
                    if (!IsRequiredOutputField(anchor.Fields[index].OutputName, columns))
                        continue;

                    changed |= AddSelfColumns(
                        member.Fields[index].Expression,
                        selfAliases,
                        recursive.OutputSchema,
                        columns);
                }
            }

            if (columns.Count == 0)
                AddAllOutputColumns(recursive, columns);
        }
    }

    private static PhysicalRecursiveCteNode ApplyRecursiveCteDefinitionPruning(
        string definitionName,
        PhysicalRecursiveCteNode recursive,
        CteDefinitionPruningPlan pruningPlan)
    {
        if (!pruningPlan.TryGetRequiredColumns(definitionName, out var required) ||
            required.Count == 0 ||
            !TryGetRecursiveOutputProjects(recursive, out var anchor, out var member) ||
            anchor.Fields.Length != member.Fields.Length)
        {
            return recursive;
        }

        var retainedIndexes = Enumerable.Range(0, anchor.Fields.Length)
            .Where(index => IsRequiredOutputField(anchor.Fields[index].OutputName, required))
            .ToArray();
        if (retainedIndexes.Length == 0 || retainedIndexes.Length == anchor.Fields.Length)
            return recursive;

        if (!TryPruneRecursiveBranch(recursive.Anchor, anchor, retainedIndexes, out var prunedAnchor) ||
            !TryPruneRecursiveBranch(recursive.RecursiveMember, member, retainedIndexes, out var prunedMember))
        {
            return recursive;
        }

        var identityFieldIndexes = recursive.IdentityFieldIndexes
            .Select(fieldIndex => Array.IndexOf(retainedIndexes, fieldIndex))
            .ToArray();
        if (identityFieldIndexes.Any(static fieldIndex => fieldIndex < 0))
        {
            throw new InvalidOperationException(
                $"Recursive CTE '{recursive.Name}' pruning removed an identity field.");
        }

        return new PhysicalRecursiveCteNode(
            recursive.Name,
            prunedAnchor,
            prunedMember,
            recursive.UnionKind,
            recursive.Keys,
            identityFieldIndexes,
            recursive.Invariants);
    }

    private static bool TryGetRecursiveOutputProjects(
        PhysicalRecursiveCteNode recursive,
        [NotNullWhen(true)] out PhysicalProjectNode? anchor,
        [NotNullWhen(true)] out PhysicalProjectNode? member)
    {
        anchor = UnwrapSingleStatement(recursive.Anchor) as PhysicalProjectNode;
        member = UnwrapSingleStatement(recursive.RecursiveMember) as PhysicalProjectNode;
        return anchor != null && member != null;
    }

    private static bool TryPruneRecursiveBranch(
        PhysicalNode branch,
        PhysicalProjectNode project,
        IReadOnlyList<int> retainedIndexes,
        out PhysicalNode pruned)
    {
        var fields = retainedIndexes
            .Select((fieldIndex, outputIndex) => project.Fields[fieldIndex] with { OutputIndex = outputIndex })
            .ToArray();
        var input = PruneProjectInput(project.Input, fields.Select(static field => field.Expression).ToArray());
        var prunedProject = new PhysicalProjectNode(fields, input) { IsDistinct = project.IsDistinct };

        if (ReferenceEquals(branch, project))
        {
            pruned = prunedProject;
            return true;
        }

        if (branch is PhysicalMultiStatementNode { Statements.Length: 1 })
        {
            pruned = new PhysicalMultiStatementNode([prunedProject]);
            return true;
        }

        pruned = branch;
        return false;
    }

    private static void AddMandatorySelfColumns(
        PhysicalNode node,
        PhysicalProjectNode outputProject,
        IReadOnlySet<string> selfAliases,
        OutputSchema recursiveOutputSchema,
        ISet<string> required)
    {
        if (!ReferenceEquals(node, outputProject))
        {
            foreach (var expression in EnumerateNodeExpressions(node))
                AddSelfColumns(expression, selfAliases, recursiveOutputSchema, required);
        }

        foreach (var child in node.Children)
            AddMandatorySelfColumns(child, outputProject, selfAliases, recursiveOutputSchema, required);
    }

    private static bool AddSelfColumns(
        IrExpression expression,
        IReadOnlySet<string> selfAliases,
        OutputSchema outputSchema,
        ISet<string> required)
    {
        var changed = false;
        foreach (var column in ColumnRefExtractor.Extract(expression))
        {
            if (string.IsNullOrWhiteSpace(column.Alias) || !selfAliases.Contains(column.Alias))
                continue;

            var normalized = NormalizeColumnName(column.ColumnName, column.Alias);
            var match = outputSchema.FindByName(normalized) ??
                        outputSchema.FindByName(GetColumnRoot(normalized));
            if (match != null)
                changed |= required.Add(match.Name);
        }

        return changed;
    }

    private static void AddAllOutputColumns(PhysicalRecursiveCteNode recursive, ISet<string> required)
    {
        foreach (var column in recursive.OutputSchema.Columns)
            required.Add(column.Name);
    }
}
