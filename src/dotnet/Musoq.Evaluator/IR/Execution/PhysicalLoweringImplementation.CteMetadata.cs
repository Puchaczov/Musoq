using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private List<ParallelCteLevel>? TryCreateParallelCteLevels(PhysicalCteNode cte)
    {
        var plannedLevels = ExecutionStrategies.GetParallelCteLevels(cte);
        if (plannedLevels.Count == 0)
        {
            return null;
        }

        var definitionsByName = cte.Definitions.ToDictionary(
            static definition => definition.Name,
            StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var levels = new List<ParallelCteLevel>();

        foreach (var level in plannedLevels)
        {
            var definitions = new List<PhysicalCteDefinition>();

            foreach (var definitionName in level.DefinitionNames)
            {
                if (!definitionsByName.TryGetValue(definitionName, out var definition))
                    return null;

                definitions.Add(definition);
                seen.Add(definition.Name);
            }

            if (definitions.Count > 0)
                levels.Add(new ParallelCteLevel(level.Level, definitions));
        }

        return seen.Count == cte.Definitions.Length ? levels : null;
    }

    private static int ResolveMaxDegreeOfParallelism(int taskCount)
    {
        return Math.Max(1, taskCount);
    }

    private static string CreateRelatedCtePhaseQueryIdentifier(string queryIdentifier, int tableIndex)
    {
        return $"{queryIdentifier}:cte{tableIndex.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string CreateCteTableName(int index, IReadOnlyCollection<string> cteDefinitionNames)
    {
        var baseName = $"cte{index.ToString(CultureInfo.InvariantCulture)}";
        var tableName = baseName;

        while (cteDefinitionNames.Contains(tableName, StringComparer.Ordinal))
            tableName = $"{tableName}Table";

        return tableName;
    }

    private static MultiStatementIndexes CreateMultiStatementIndexes(
        PhysicalMultiStatementNode multiStatement,
        IReadOnlyDictionary<string, int>? existingCteIndexes = null,
        IReadOnlyDictionary<string, GeneratedRowShape>? existingCteShapesByName = null,
        string? statementNamePrefix = null)
    {
        var producerIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var cteIndexes = existingCteIndexes == null
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, int>(existingCteIndexes, StringComparer.OrdinalIgnoreCase);
        var cteShapesByName = existingCteShapesByName == null
            ? new Dictionary<string, GeneratedRowShape>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, GeneratedRowShape>(existingCteShapesByName, StringComparer.OrdinalIgnoreCase);
        var nextProducerIndex = 0;
        var nextTableIndex = cteIndexes.Count == 0 ? 0 : cteIndexes.Values.Max() + 1;

        foreach (var statement in multiStatement.Statements)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectCteRefNames(statement, names);

            foreach (var name in names)
            {
                if (cteIndexes.ContainsKey(name))
                    continue;

                producerIndexByName[name] = nextProducerIndex++;
                cteIndexes[name] = nextTableIndex++;
            }
        }

        return new MultiStatementIndexes(cteIndexes, producerIndexByName, cteShapesByName, statementNamePrefix);
    }

    private static Dictionary<string, CteReferenceClassification> ClassifyMultiStatementCteReferences(
        PhysicalMultiStatementNode multiStatement,
        MultiStatementIndexes indexes)
    {
        var referenceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var statement in multiStatement.Statements)
            CountCteReferences(statement, referenceCounts);

        var classifications = new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, producerIndex) in indexes.ProducerIndexByName)
        {
            var flags = producerIndex < multiStatement.Statements.Length
                ? ClassifyCteOutput(UnwrapSingleStatement(multiStatement.Statements[producerIndex]))
                : CteOutputFlags.None;
            classifications[name] = new CteReferenceClassification(
                name,
                referenceCounts.TryGetValue(name, out var count) ? count : 0,
                flags);
        }

        return classifications;
    }

    private static Dictionary<string, CteReferenceClassification> ClassifyCteReferences(
        PhysicalCteNode cte)
    {
        var referenceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in cte.Definitions)
            CountCteReferences(definition.Plan, referenceCounts);

        CountCteReferences(cte.Query, referenceCounts);

        return cte.Definitions.ToDictionary(
            static definition => definition.Name,
            definition => new CteReferenceClassification(
                definition.Name,
                referenceCounts.TryGetValue(definition.Name, out var count) ? count : 0,
                ClassifyCteOutput(UnwrapSingleStatement(definition.Plan))),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool CanFuseReadOnceCte(
        string cteName,
        IReadOnlyDictionary<string, CteReferenceClassification> classifications)
    {
        return classifications.TryGetValue(cteName, out var classification) &&
               classification.ReferenceCount == 1 &&
               !classification.Flags.HasFlag(CteOutputFlags.OrderSensitive) &&
               !classification.Flags.HasFlag(CteOutputFlags.Window) &&
               !classification.Flags.HasFlag(CteOutputFlags.SetOperation) &&
               !classification.Flags.HasFlag(CteOutputFlags.SideEffectSensitive);
    }

    private static void CountCteReferences(
        PhysicalNode node,
        IDictionary<string, int> counts)
    {
        if (node is PhysicalCteRefNode cteRef)
        {
            counts.TryGetValue(cteRef.CteName, out var count);
            counts[cteRef.CteName] = count + 1;
            return;
        }

        foreach (var child in node.Children)
            CountCteReferences(child, counts);
    }

    private static CteOutputFlags ClassifyCteOutput(PhysicalNode node)
    {
        var flags = CteOutputFlags.None;
        ClassifyCteOutput(node, ref flags);
        return flags;
    }

    private static void ClassifyCteOutput(PhysicalNode node, ref CteOutputFlags flags)
    {
        flags |= node switch
        {
            PhysicalSortNode or PhysicalSkipNode or PhysicalTakeNode or PhysicalTopNNode or PhysicalTopOffsetNode => CteOutputFlags.OrderSensitive,
            PhysicalAggregateOnlyNode or PhysicalSingleKeyAggregateNode or PhysicalValueTupleAggregateNode => CteOutputFlags.Aggregate,
            PhysicalWindowNode => CteOutputFlags.Window,
            PhysicalSetOperationNode => CteOutputFlags.SetOperation,
            PhysicalInterpretSourceNode or PhysicalAccessMethodSourceNode or PhysicalPropertySourceNode => CteOutputFlags.SideEffectSensitive,
            _ => CteOutputFlags.None
        };

        foreach (var child in node.Children)
            ClassifyCteOutput(child, ref flags);
    }

    private static string? ResolveStatementNamePrefix(string resultTableName)
    {
        return string.Equals(resultTableName, "result", StringComparison.Ordinal)
            ? null
            : resultTableName;
    }

    private static string CreateStatementTableName(string? namePrefix, int index)
    {
        var indexText = index.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(namePrefix)
            ? $"statement{indexText}"
            : $"{namePrefix}_statement{indexText}";
    }

    private static string CreateStatementShapeName(string? namePrefix, int index)
    {
        var indexText = index.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(namePrefix)
            ? $"Statement{indexText}Row0"
            : $"{CreateGeneratedTypeNamePrefix(namePrefix)}Statement{indexText}Row0";
    }

    private static string CreateGeneratedTypeNamePrefix(string namePrefix)
    {
        var builder = new StringBuilder();
        var capitalizeNext = true;

        foreach (var character in namePrefix)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = true;
                continue;
            }

            builder.Append(capitalizeNext ? char.ToUpperInvariant(character) : character);
            capitalizeNext = false;
        }

        return builder.Length == 0 ? "Scoped" : builder.ToString();
    }

    private static string? ResolveStatementCteName(
        int statementIndex,
        MultiStatementIndexes indexes)
    {
        foreach (var (name, producerIndex) in indexes.ProducerIndexByName)
        {
            if (producerIndex == statementIndex)
                return name;
        }

        return null;
    }

    private static int ResolveStatementTableIndex(
        int statementIndex,
        MultiStatementIndexes indexes)
    {
        foreach (var (name, producerIndex) in indexes.ProducerIndexByName)
        {
            if (producerIndex == statementIndex)
                return indexes.CteIndexes[name];
        }

        return -1;
    }

    private static void CollectCteRefNames(PhysicalNode node, HashSet<string> names)
    {
        if (node is PhysicalCteRefNode cteRef)
        {
            names.Add(cteRef.CteName);
            return;
        }

        foreach (var child in node.Children)
            CollectCteRefNames(child, names);
    }
}
