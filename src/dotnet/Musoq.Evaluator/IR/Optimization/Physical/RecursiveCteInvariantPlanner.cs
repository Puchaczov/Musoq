using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static partial class RecursiveCteInvariantPlanner
{
    public static PhysicalRecursiveCteNode Plan(
        PhysicalRecursiveCteNode recursive,
        IReadOnlyDictionary<string, OutputSchema> externalCteSchemas)
    {
        if (recursive.Invariants.Length != 0)
            return recursive;

        var selfAliases = CollectSelfAliases(recursive.Name, recursive.RecursiveMember);
        if (selfAliases.Count == 0)
            return recursive;

        var referencedColumns = CollectReferencedColumns(recursive.RecursiveMember);
        var transitionCteNames = CollectTransitionCteNames(recursive.RecursiveMember);
        var definitions = new List<PhysicalRecursiveCteInvariantDefinition>();
        var member = ExtractInvariantTransitionProducers(
            recursive.RecursiveMember,
            recursive.Name,
            externalCteSchemas,
            selfAliases,
            referencedColumns,
            definitions);
        transitionCteNames.UnionWith(definitions.Select(static definition => definition.Name));
        member = Extract(
            member,
            recursive.Name,
            selfAliases,
            transitionCteNames,
            referencedColumns,
            definitions,
            externalCteSchemas);
        if (definitions.Count == 0)
            return recursive;

        var referenceCounts = CountInvariantReferences(member, definitions);
        member = SelectHashIndexes(member, definitions, referenceCounts);

        return recursive with
        {
            RecursiveMember = member,
            Invariants = definitions.ToArray()
        };
    }

    private static PhysicalNode ExtractInvariantTransitionProducers(
        PhysicalNode member,
        string recursiveName,
        IReadOnlyDictionary<string, OutputSchema> externalCteSchemas,
        IReadOnlySet<string> selfAliases,
        IReadOnlyDictionary<string, HashSet<string>> referencedColumns,
        List<PhysicalRecursiveCteInvariantDefinition> definitions)
    {
        if (member is not PhysicalMultiStatementNode multiStatement || multiStatement.Statements.Length < 2)
            return member;

        var producerNames = InferTransitionProducerNames(
            multiStatement,
            recursiveName,
            externalCteSchemas.Keys);
        if (producerNames.Count == 0)
            return member;

        var statements = multiStatement.Statements.ToList();
        var removed = 0;
        foreach (var (originalIndex, producerName) in producerNames.OrderBy(static pair => pair.Key))
        {
            var index = originalIndex - removed;
            if (index < 0 || index >= statements.Count - 1)
                continue;

            var statement = statements[index];
            if (!CanExtract(
                    statement,
                    recursiveName,
                    selfAliases,
                    new HashSet<string>(StringComparer.Ordinal)))
                continue;

            var producerReference = statements
                .Skip(index + 1)
                .SelectMany(EnumerateCteReferences)
                .FirstOrDefault(reference => string.Equals(
                    reference.CteName,
                    producerName,
                    StringComparison.Ordinal));
            if (producerReference == null || !TryCreateTransitionDefinition(
                    statement,
                    producerName,
                    producerReference.Alias,
                    definitions.Count,
                    referencedColumns,
                    externalCteSchemas,
                    out var definition))
            {
                continue;
            }

            definitions.Add(definition);
            statements.RemoveAt(index);
            removed++;
            for (var rewriteIndex = index; rewriteIndex < statements.Count; rewriteIndex++)
            {
                statements[rewriteIndex] = RewriteCteReference(
                    statements[rewriteIndex],
                    producerName,
                    definition);
            }
        }

        return statements.Count == multiStatement.Statements.Length
            ? member
            : new PhysicalMultiStatementNode(statements.ToArray());
    }

    private static Dictionary<int, string> InferTransitionProducerNames(
        PhysicalMultiStatementNode multiStatement,
        string recursiveName,
        IEnumerable<string> externalCteNames)
    {
        var knownNames = new HashSet<string>(externalCteNames, StringComparer.Ordinal)
        {
            recursiveName
        };
        var result = new Dictionary<int, string>();
        var nextProducerIndex = 0;

        foreach (var statement in multiStatement.Statements)
        foreach (var reference in EnumerateCteReferences(statement))
        {
            if (!knownNames.Add(reference.CteName))
                continue;

            result[nextProducerIndex++] = reference.CteName;
        }

        return result;
    }

    private static bool TryCreateTransitionDefinition(
        PhysicalNode statement,
        string producerName,
        string producerAlias,
        int ordinal,
        IReadOnlyDictionary<string, HashSet<string>> referencedColumns,
        IReadOnlyDictionary<string, OutputSchema> externalCteSchemas,
        out PhysicalRecursiveCteInvariantDefinition definition)
    {
        var project = UnwrapProject(statement);
        if (project == null)
        {
            definition = null!;
            return false;
        }

        var referenced = referencedColumns.TryGetValue(producerAlias, out var names)
            ? names
            : null;
        var selectedFields = project.Fields
            .Where(field => referenced == null || referenced.Count == 0 ||
                            referenced.Contains(field.OutputName))
            .Select((field, index) => field with { OutputIndex = index })
            .ToArray();
        if (selectedFields.Length == 0)
            selectedFields = project.Fields.Select((field, index) => field with { OutputIndex = index }).ToArray();

        TryCollectSources(project.Input, externalCteSchemas, out var sources);
        var sourceAliases = sources.Select(static source => source.Alias)
            .Append(producerAlias)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var name = CreateInvariantName(producerName, ordinal);
        definition = new PhysicalRecursiveCteInvariantDefinition(
            name,
            project.Input,
            producerAlias,
            sourceAliases,
            selectedFields,
            PhysicalRecursiveCteInvariantStorageKind.Snapshot,
            [],
            []);
        return true;
    }

    private static PhysicalProjectNode? UnwrapProject(PhysicalNode node)
    {
        while (node is PhysicalMultiStatementNode { Statements: [var statement] })
            node = statement;
        return node as PhysicalProjectNode;
    }

    private static PhysicalNode RewriteCteReference(
        PhysicalNode node,
        string producerName,
        PhysicalRecursiveCteInvariantDefinition definition)
    {
        if (node is PhysicalCteRefNode cteRef && string.Equals(
                cteRef.CteName,
                producerName,
                StringComparison.Ordinal))
        {
            return new PhysicalCteRefNode(definition.Name, cteRef.Alias, definition.OutputSchema);
        }

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => RewriteCteReference(child, producerName, definition));
    }

    private static IEnumerable<PhysicalCteRefNode> EnumerateCteReferences(PhysicalNode node)
    {
        if (node is PhysicalCteRefNode cteRef)
        {
            yield return cteRef;
            yield break;
        }

        foreach (var child in node.Children)
        foreach (var reference in EnumerateCteReferences(child))
            yield return reference;
    }

    private static PhysicalNode Extract(
        PhysicalNode node,
        string recursiveName,
        IReadOnlySet<string> selfAliases,
        IReadOnlySet<string> transitionCteNames,
        IReadOnlyDictionary<string, HashSet<string>> referencedColumns,
        List<PhysicalRecursiveCteInvariantDefinition> definitions,
        IReadOnlyDictionary<string, OutputSchema> externalCteSchemas)
    {
        if (CanExtract(node, recursiveName, selfAliases, transitionCteNames) &&
            TryCollectSources(node, externalCteSchemas, out var sources) &&
            sources.Count > 0)
        {
            var ordinal = definitions.Count;
            var name = CreateInvariantName(recursiveName, ordinal);
            var fields = CreateFields(sources, referencedColumns);
            if (fields.Length > 0)
            {
                var aliases = sources.Select(static source => source.Alias)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var existingRows = node is PhysicalCteRefNode;
                var alias = aliases.Length == 1 ? aliases[0] : name;
                var definition = new PhysicalRecursiveCteInvariantDefinition(
                    name,
                    node,
                    alias,
                    aliases,
                    fields,
                    existingRows
                        ? PhysicalRecursiveCteInvariantStorageKind.ExistingRows
                        : PhysicalRecursiveCteInvariantStorageKind.Snapshot,
                    [],
                    [])
                {
                    ExistingCteName = node is PhysicalCteRefNode existingRef
                        ? existingRef.CteName
                        : null
                };
                definitions.Add(definition);
                return new PhysicalCteRefNode(name, alias, definition.OutputSchema);
            }
        }

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => Extract(
                child,
                recursiveName,
                selfAliases,
                transitionCteNames,
                referencedColumns,
                definitions,
                externalCteSchemas));
    }

    private static bool CanExtract(
        PhysicalNode node,
        string recursiveName,
        IReadOnlySet<string> selfAliases,
        IReadOnlySet<string> transitionCteNames)
    {
        if (!IsSupportedInvariantRelation(node) ||
            ContainsRecursiveReference(node, recursiveName) ||
            node is PhysicalCteRefNode cteRef && transitionCteNames.Contains(cteRef.CteName))
            return false;

        foreach (var expression in EnumerateExpressionsRecursively(node))
        foreach (var alias in AliasRefExtractor.Extract(expression))
        {
            if (selfAliases.Contains(alias))
                return false;
        }

        return !ContainsCorrelatedSourceAlias(node, selfAliases);
    }

    private static HashSet<string> CollectTransitionCteNames(PhysicalNode member)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (member is not PhysicalMultiStatementNode multiStatement)
            return names;

        for (var index = 1; index < multiStatement.Statements.Length; index++)
        {
            var current = multiStatement.Statements[index];
            while (current is PhysicalProjectNode project)
                current = project.Input;
            while (current is PhysicalFilterNode filter)
                current = filter.Input;
            if (current is PhysicalCteRefNode cteRef)
                names.Add(cteRef.CteName);
        }

        return names;
    }

    private static bool IsSupportedInvariantRelation(PhysicalNode node)
    {
        return node switch
        {
            PhysicalSchemaScanNode or PhysicalValuesScanNode or PhysicalInterpretSourceNode or
                PhysicalAccessMethodSourceNode or PhysicalPropertySourceNode or PhysicalCteRefNode => true,
            PhysicalProjectNode project when !project.IsDistinct => IsSupportedInvariantRelation(project.Input),
            PhysicalFilterNode filter => IsSupportedInvariantRelation(filter.Input),
            PhysicalHashJoinNode { Kind: JoinKind.Inner } join =>
                IsSupportedInvariantRelation(join.Left) && IsSupportedInvariantRelation(join.Right),
            PhysicalNestedLoopJoinNode { Kind: JoinKind.Inner or JoinKind.Cross } join =>
                IsSupportedInvariantRelation(join.Left) && IsSupportedInvariantRelation(join.Right),
            PhysicalNestedLoopApplyNode { Kind: ApplyKind.Cross or ApplyKind.Outer } apply =>
                IsSupportedInvariantRelation(apply.Left) && IsSupportedInvariantRelation(apply.Right),
            _ => false
        };
    }

    private static bool ContainsRecursiveReference(PhysicalNode node, string recursiveName)
    {
        if (node is PhysicalCteRefNode cteRef &&
            string.Equals(cteRef.CteName, recursiveName, StringComparison.Ordinal))
        {
            return true;
        }

        return node.Children.Any(child => ContainsRecursiveReference(child, recursiveName));
    }

    private static bool ContainsCorrelatedSourceAlias(
        PhysicalNode node,
        IReadOnlySet<string> selfAliases)
    {
        if (node is PhysicalAccessMethodSourceNode access && selfAliases.Contains(access.SourceAlias))
            return true;
        if (node is PhysicalPropertySourceNode property && selfAliases.Contains(property.SourceAlias))
            return true;

        return node.Children.Any(child => ContainsCorrelatedSourceAlias(child, selfAliases));
    }

    private static HashSet<string> CollectSelfAliases(string recursiveName, PhysicalNode member)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectSelfAliases(member, recursiveName, aliases);
        return aliases;
    }

    private static void CollectSelfAliases(PhysicalNode node, string recursiveName, ISet<string> aliases)
    {
        if (node is PhysicalCteRefNode cteRef &&
            string.Equals(cteRef.CteName, recursiveName, StringComparison.Ordinal))
        {
            aliases.Add(cteRef.Alias);
        }

        foreach (var child in node.Children)
            CollectSelfAliases(child, recursiveName, aliases);
    }

    private static Dictionary<string, HashSet<string>> CollectReferencedColumns(PhysicalNode node)
    {
        var columns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var expression in EnumerateExpressionsRecursively(node))
        foreach (var column in ColumnRefExtractor.Extract(expression))
        {
            if (string.IsNullOrWhiteSpace(column.Alias))
                continue;

            if (!columns.TryGetValue(column.Alias, out var names))
            {
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                columns[column.Alias] = names;
            }

            names.Add(NormalizeColumnName(column.ColumnName, column.Alias));
        }

        return columns;
    }

    private static ProjectedField[] CreateFields(
        IReadOnlyList<InvariantSource> sources,
        IReadOnlyDictionary<string, HashSet<string>> referencedColumns)
    {
        var selected = new List<(string Alias, ColumnSchema Column)>();
        foreach (var source in sources)
        {
            var columns = referencedColumns.TryGetValue(source.Alias, out var referenced) && referenced.Count > 0
                ? source.Schema.Columns.Where(column => referenced.Contains(column.Name)).ToArray()
                : source.Schema.Columns;
            selected.AddRange(columns.Select(column => (source.Alias, column)));
        }

        var duplicateNames = selected
            .GroupBy(static item => item.Column.Name, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var qualify = sources.Select(static source => source.Alias)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Skip(1)
            .Any();

        return selected.Select((item, index) => new ProjectedField(
                qualify || duplicateNames.Contains(item.Column.Name)
                    ? $"{item.Alias}.{item.Column.Name}"
                    : item.Column.Name,
                new ColumnRef(item.Alias, item.Column.Name, item.Column.Type),
                index))
            .ToArray();
    }

    private static bool TryCollectSources(
        PhysicalNode node,
        IReadOnlyDictionary<string, OutputSchema> externalCteSchemas,
        out List<InvariantSource> sources)
    {
        sources = [];
        CollectSources(node, externalCteSchemas, sources);
        return sources.Count > 0;
    }

    private static void CollectSources(
        PhysicalNode node,
        IReadOnlyDictionary<string, OutputSchema> externalCteSchemas,
        ICollection<InvariantSource> sources)
    {
        switch (node)
        {
            case PhysicalSchemaScanNode source:
                sources.Add(new InvariantSource(source.Alias, source.OutputSchema));
                return;
            case PhysicalValuesScanNode source:
                sources.Add(new InvariantSource(source.Alias, source.OutputSchema));
                return;
            case PhysicalInterpretSourceNode source:
                sources.Add(new InvariantSource(source.Alias, source.OutputSchema));
                return;
            case PhysicalAccessMethodSourceNode source:
                sources.Add(new InvariantSource(source.Alias, source.OutputSchema));
                return;
            case PhysicalPropertySourceNode source:
                sources.Add(new InvariantSource(source.Alias, source.OutputSchema));
                return;
            case PhysicalCteRefNode source:
                var schema = source.OutputSchema.Columns.Length > 0
                    ? source.OutputSchema
                    : externalCteSchemas.GetValueOrDefault(source.CteName, source.OutputSchema);
                sources.Add(new InvariantSource(source.Alias, schema));
                return;
        }

        foreach (var child in node.Children)
            CollectSources(child, externalCteSchemas, sources);
    }

}
