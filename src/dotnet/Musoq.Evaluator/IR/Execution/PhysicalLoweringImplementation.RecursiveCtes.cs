using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildRecursiveCteDefinitionTable(
        PhysicalCteNode owningCte,
        PhysicalRecursiveCteNode recursive,
        int index,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        var cteTableName = CreateCteTableName(index, cteDefinitionNames);
        var rowShapeName = $"Cte{index.ToString(CultureInfo.InvariantCulture)}Row0";
        var currentFrontier = new ExecutionVariable(
            $"{cteTableName}CurrentFrontier",
            typeof(IReadOnlyList<Row>));
        var nextFrontier = new ExecutionVariable($"{cteTableName}NextFrontier", typeof(object));
        var snapshotRows = new ExecutionVariable($"__{cteTableName}SnapshotRows", typeof(int));
        var result = new ExecutionVariable(cteTableName, typeof(object));

        if (UnwrapSingleStatement(recursive.Anchor) is not PhysicalProjectNode anchorProject)
        {
            return TableBuildResult.Unsupported(
                $"Recursive CTE '{recursive.Name}' branches must end in direct projections.");
        }

        var canonicalShape = CreateGeneratedShape(rowShapeName, anchorProject.Fields) with
        {
            Contexts = [],
            RequiresRowBase = false,
            EmitAsValueType = true
        };
        result = result with { GeneratedRowTypeName = $"List<{canonicalShape.TypeName}>" };
        currentFrontier = currentFrontier with { GeneratedRowTypeName = $"List<{canonicalShape.TypeName}>" };
        nextFrontier = nextFrontier with { GeneratedRowTypeName = $"List<{canonicalShape.TypeName}>" };

        if (!TryResolveRecursiveIdentity(
                recursive,
                canonicalShape,
                cteTableName,
                out var identityMode,
                out var identityFieldIndexes,
                out var seen,
                out var identityReason))
        {
            return TableBuildResult.Unsupported(identityReason);
        }

        var limits = _compilationOptions.RecursiveCteLimits;
        var anchorSink = new RecursiveCteTableSink(
            recursive.Name,
            index,
            result,
            currentFrontier,
            currentFrontier,
            seen,
            identityFieldIndexes,
            canonicalShape,
            limits.MaxRows);

        var anchor = BuildPlanTable(
            recursive.Anchor,
            currentFrontier.Name,
            rowShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scopeAggregateVariables: true,
            scope: scope.WithRecursiveCteSink(anchorSink));
        if (!anchor.IsBuilt)
            return anchor;

        var memberCteShapes = new Dictionary<string, GeneratedRowShape>(cteShapesByName, StringComparer.OrdinalIgnoreCase)
        {
            [recursive.Name] = canonicalShape
        };
        var invariantInputs = BuildRecursiveInvariantInputs(
            owningCte,
            recursive,
            index,
            cteTableName,
            snapshotRows,
            cteIndexes,
            memberCteShapes,
            schemaFromIndex + CountSchemaScans(recursive.Anchor),
            scope);
        if (!invariantInputs.IsBuilt)
            return TableBuildResult.Unsupported(invariantInputs.UnsupportedReason);

        var memberSink = anchorSink with { InputFrontier = currentFrontier, Frontier = nextFrontier };
        var recursiveMember = BuildPlanTable(
            recursive.RecursiveMember,
            nextFrontier.Name,
            rowShapeName,
            cteIndexes,
            memberCteShapes,
            invariantInputs.NextSchemaFromIndex,
            scopeAggregateVariables: true,
            scope: scope
                .WithRecursiveCteInvariantInputs(invariantInputs.Inputs)
                .WithRecursiveCteSink(memberSink));
        if (!recursiveMember.IsBuilt)
            return recursiveMember;

        var recursiveNode = new ExecutionRecursiveCte(
            recursive.Name,
            index,
            result,
            currentFrontier,
            nextFrontier,
            snapshotRows,
            seen,
            identityMode,
            identityFieldIndexes,
            canonicalShape,
            new ExecutionBlock(anchor.Nodes),
            new ExecutionBlock(invariantInputs.Nodes),
            new ExecutionBlock(recursiveMember.Nodes),
            limits.MaxIterations,
            limits.MaxRows,
            limits.MaxSnapshotRows);
        var shapes = anchor.Shapes
            .Concat(invariantInputs.Shapes)
            .Concat(recursiveMember.Shapes.Where(shape =>
                shape is not GeneratedRowShape generated ||
                !string.Equals(generated.TypeName, canonicalShape.TypeName, StringComparison.Ordinal)))
            .Select(shape => shape is GeneratedRowShape generated &&
                             string.Equals(generated.TypeName, canonicalShape.TypeName, StringComparison.Ordinal)
                ? canonicalShape
                : shape)
            .DistinctBy(static shape => shape.Name, StringComparer.Ordinal)
            .ToArray();

        return TableBuildResult.Success(shapes, [recursiveNode], result, canonicalShape);
    }

    private static bool TryResolveRecursiveIdentity(
        PhysicalRecursiveCteNode recursive,
        GeneratedRowShape rowShape,
        string tableName,
        out ExecutionRecursiveCteIdentityMode mode,
        out int[] fieldIndexes,
        out ExecutionVariable? seen,
        out string reason)
    {
        switch (recursive.UnionKind)
        {
            case RecursiveCteUnionKind.All:
                mode = ExecutionRecursiveCteIdentityMode.None;
                fieldIndexes = [];
                seen = null;
                reason = string.Empty;
                return true;
            case RecursiveCteUnionKind.FullRow:
                mode = ExecutionRecursiveCteIdentityMode.FullRow;
                fieldIndexes = Enumerable.Range(0, rowShape.Fields.Count).ToArray();
                break;
            case RecursiveCteUnionKind.Keyed:
                mode = ExecutionRecursiveCteIdentityMode.Keyed;
                fieldIndexes = recursive.IdentityFieldIndexes;
                if (fieldIndexes.Length != recursive.Keys.Length)
                {
                    throw new InvalidOperationException(
                        $"Recursive CTE '{recursive.Name}' has {recursive.Keys.Length} identity keys but " +
                        $"{fieldIndexes.Length} resolved identity fields.");
                }

                for (var keyIndex = 0; keyIndex < fieldIndexes.Length; keyIndex++)
                {
                    var fieldIndex = fieldIndexes[keyIndex];
                    if (fieldIndex < 0 || fieldIndex >= rowShape.Fields.Count ||
                        !string.Equals(
                            rowShape.Fields[fieldIndex].Name,
                            recursive.Keys[keyIndex],
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Recursive CTE '{recursive.Name}' identity field {fieldIndex} does not match " +
                            $"exported key '{recursive.Keys[keyIndex]}'.");
                    }
                }

                break;
            default:
                mode = default;
                fieldIndexes = [];
                seen = null;
                reason = $"Recursive CTE '{recursive.Name}' has unknown identity mode '{recursive.UnionKind}'.";
                return false;
        }

        if (fieldIndexes.Length == 0)
        {
            seen = null;
            reason = $"Recursive CTE '{recursive.Name}' identity requires at least one output column.";
            return false;
        }

        seen = new ExecutionVariable($"{tableName}Seen", typeof(object));
        reason = string.Empty;
        return true;
    }
}
