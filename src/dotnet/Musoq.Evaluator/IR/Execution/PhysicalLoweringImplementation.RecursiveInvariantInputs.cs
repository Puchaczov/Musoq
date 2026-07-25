using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private RecursiveInvariantBuildResult BuildRecursiveInvariantInputs(
        PhysicalCteNode owningCte,
        PhysicalRecursiveCteNode recursive,
        int cteIndex,
        string cteTableName,
        ExecutionVariable snapshotRows,
        IReadOnlyDictionary<string, int> cteIndexes,
        Dictionary<string, GeneratedRowShape> cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        var shapes = new List<RowShape>();
        var nodes = new List<ExecutionNode>();
        var inputs = new Dictionary<string, RecursiveCteInvariantInput>(StringComparer.Ordinal);
        var nextSchemaFromIndex = schemaFromIndex;

        for (var ordinal = 0; ordinal < recursive.Invariants.Length; ordinal++)
        {
            var invariant = recursive.Invariants[ordinal];
            if (TryReuseExistingRecursiveInvariantSidecar(
                    owningCte,
                    invariant,
                    ordinal,
                    cteTableName,
                    cteIndexes,
                    cteShapesByName,
                    scope,
                    out var sidecarInput,
                    out var sidecarLoad))
            {
                cteShapesByName[invariant.Name] = sidecarInput.RowShape;
                inputs.Add(invariant.Name, sidecarInput);
                nodes.Add(sidecarLoad);
                continue;
            }

            if (TryReuseExistingRecursiveInvariantRows(
                    invariant,
                    cteIndexes,
                    cteShapesByName,
                    out var existingInput))
            {
                cteShapesByName[invariant.Name] = existingInput.RowShape;
                inputs.Add(invariant.Name, existingInput);
                continue;
            }

            var ordinalText = ordinal.ToString(CultureInfo.InvariantCulture);
            var shape = CreateGeneratedShape($"Cte{cteIndex.ToString(CultureInfo.InvariantCulture)}Invariant{ordinalText}Row0", invariant.Fields) with
            {
                Contexts = [],
                RequiresRowBase = false,
                EmitAsValueType = true
            };
            cteShapesByName[invariant.Name] = shape;

            var projection = new PhysicalProjectNode(invariant.Fields, invariant.Plan);
            var table = new ExecutionVariable(
                $"{cteTableName}Invariant{ordinalText}",
                typeof(object),
                $"List<{shape.TypeName}>");
            TableBuildResult result;
            RecursiveCteInvariantInput input;

            if (invariant.StorageKind is PhysicalRecursiveCteInvariantStorageKind.HashIndex or
                PhysicalRecursiveCteInvariantStorageKind.ExistingHashIndex)
            {
                var keyType = ResolveRecursiveInvariantHashKeyType(invariant);
                var hash = new ExecutionVariable($"{table.Name}Hash", typeof(object));
                var row = new ExecutionVariable(
                    $"{table.Name}Row",
                    typeof(Row),
                    shape.TypeName);
                var carrierRef = new PhysicalCteRefNode(invariant.Name, row.Name, invariant.OutputSchema);
                var carrierShape = CreateTypedTableRowShape(carrierRef, shape);
                var carrierLookup = new Dictionary<string, RowShape>(
                    RowShapeLookup.CreateTransitionAliasLookup(
                        RowShapeLookup.CreateSourceShapeLookup(carrierShape)),
                    StringComparer.OrdinalIgnoreCase)
                {
                    [invariant.Alias] = carrierShape
                };
                foreach (var sourceAlias in invariant.SourceAliases)
                    carrierLookup.TryAdd(sourceAlias, carrierShape);
                var key = CreateHashJoinKeyExpression(invariant.HashKeys, carrierLookup, keyType);
                var sink = new RecursiveCteInvariantHashSink(
                    recursive.Name,
                    hash,
                    row,
                    shape,
                    key,
                    ExecutionClrBindingFactory.FromClr(keyType),
                    snapshotRows,
                    _compilationOptions.RecursiveCteLimits.MaxSnapshotRows);
                result = BuildPlanTable(
                    projection,
                    table.Name,
                    shape.TypeName,
                    cteIndexes,
                    cteShapesByName,
                    nextSchemaFromIndex,
                    scopeAggregateVariables: true,
                    scope: scope.WithDirectTableSink(sink));
                if (!result.IsBuilt)
                    return RecursiveInvariantBuildResult.Unsupported(result.UnsupportedReason);

                nodes.Add(new ExecutionCreateHash(
                    hash,
                    ExecutionClrBindingFactory.FromClr(keyType),
                    ExecutionClrBindingFactory.FromClr(typeof(Row)),
                    GeneratedRowTypeName: shape.TypeName));
                nodes.AddRange(result.Nodes);
                input = new RecursiveCteInvariantInput(
                    invariant.Name,
                    shape,
                    new ExecutionVariableRead(table),
                    hash);
            }
            else
            {
                var sink = new RecursiveCteInvariantSnapshotSink(
                    recursive.Name,
                    table,
                    shape,
                    snapshotRows,
                    _compilationOptions.RecursiveCteLimits.MaxSnapshotRows);
                result = BuildPlanTable(
                    projection,
                    table.Name,
                    shape.TypeName,
                    cteIndexes,
                    cteShapesByName,
                    nextSchemaFromIndex,
                    scopeAggregateVariables: true,
                    scope: scope.WithDirectTableSink(sink));
                if (!result.IsBuilt)
                    return RecursiveInvariantBuildResult.Unsupported(result.UnsupportedReason);

                nodes.Add(CreateTable(table, shape));
                nodes.AddRange(result.Nodes);
                input = new RecursiveCteInvariantInput(
                    invariant.Name,
                    shape,
                    new ExecutionVariableRead(table));
            }

            shapes.AddRange(result.Shapes);
            inputs.Add(invariant.Name, input);
            nextSchemaFromIndex += CountSchemaScans(invariant.Plan);
        }

        return RecursiveInvariantBuildResult.Success(shapes, nodes, inputs, nextSchemaFromIndex);
    }

    private bool TryReuseExistingRecursiveInvariantSidecar(
        PhysicalCteNode owningCte,
        PhysicalRecursiveCteInvariantDefinition invariant,
        int ordinal,
        string cteTableName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        LoweringScope scope,
        out RecursiveCteInvariantInput input,
        out ExecutionNode load)
    {
        input = null!;
        load = null!;
        if (invariant.StorageKind != PhysicalRecursiveCteInvariantStorageKind.ExistingHashIndex ||
            invariant.ExistingCteName == null ||
            !cteIndexes.TryGetValue(invariant.ExistingCteName, out var tableIndex) ||
            !cteShapesByName.TryGetValue(invariant.ExistingCteName, out var storedRowShape))
        {
            return false;
        }

        var keyColumns = invariant.HashKeys
            .OfType<Musoq.Evaluator.IR.Expressions.ColumnRef>()
            .Select(static key => key.ColumnName)
            .ToArray();
        if (keyColumns.Length != invariant.HashKeys.Length)
            return false;

        var sidecar = ExecutionStrategies
            .GetCteSidecarIndexSpecs(owningCte, invariant.ExistingCteName)
            .FirstOrDefault(spec =>
                spec.Kind == CteSidecarIndexKind.Hash &&
                spec.KeyColumns.SequenceEqual(keyColumns, StringComparer.OrdinalIgnoreCase));
        if (sidecar == null)
            return false;

        var rowShape = scope.CteSidecarHashPayloads.TryGet(sidecar.IndexSlot, out var payloadShape)
            ? new GeneratedRowShape(
                payloadShape.TypeName,
                payloadShape.Fields,
                payloadShape.Contexts)
            : storedRowShape;
        var hash = new ExecutionVariable(
            $"{cteTableName}Invariant{ordinal.ToString(CultureInfo.InvariantCulture)}Hash",
            typeof(object));
        load = new ExecutionCteSidecarIndexLoadCandidate(
            hash,
            sidecar.IndexSlot,
            ExecutionCteSidecarIndexKind.Hash,
            ExecutionClrBindingFactory.FromClr(sidecar.KeyType),
            ExecutionClrBindingFactory.FromClr(typeof(Row)),
            rowShape.TypeName);
        input = new RecursiveCteInvariantInput(
            invariant.Name,
            rowShape,
            new ExecutionStoredTableRows(tableIndex, storedRowShape),
            hash);
        return true;
    }

    private static bool TryReuseExistingRecursiveInvariantRows(
        PhysicalRecursiveCteInvariantDefinition invariant,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        out RecursiveCteInvariantInput input)
    {
        input = null!;
        if (invariant.StorageKind != PhysicalRecursiveCteInvariantStorageKind.ExistingRows ||
            invariant.Plan is not PhysicalCteRefNode existingRef ||
            !cteIndexes.TryGetValue(existingRef.CteName, out var tableIndex) ||
            !cteShapesByName.TryGetValue(existingRef.CteName, out var rowShape))
        {
            return false;
        }

        input = new RecursiveCteInvariantInput(
            invariant.Name,
            rowShape,
            new ExecutionStoredTableRows(tableIndex, rowShape));
        return true;
    }

    private static Type ResolveRecursiveInvariantHashKeyType(
        PhysicalRecursiveCteInvariantDefinition invariant)
    {
        if (invariant.HashKeys.Length != invariant.HashProbeKeys.Length || invariant.HashKeys.Length == 0)
            throw new InvalidOperationException($"Recursive invariant '{invariant.Name}' has invalid hash keys.");

        if (invariant.HashKeys.Length == 1)
        {
            return ResolveCommonKeyType(
                invariant.HashKeys[0].ReturnType,
                invariant.HashProbeKeys[0].ReturnType);
        }

        var types = invariant.HashKeys.Select((key, index) =>
                ResolveCommonKeyType(key.ReturnType, invariant.HashProbeKeys[index].ReturnType))
            .ToArray();
        return types.Length <= 7 ? CreateValueTupleType(types) : typeof(object);
    }
}

internal sealed record RecursiveInvariantBuildResult(
    bool IsBuilt,
    string UnsupportedReason,
    IReadOnlyList<RowShape> Shapes,
    IReadOnlyList<ExecutionNode> Nodes,
    IReadOnlyDictionary<string, RecursiveCteInvariantInput> Inputs,
    int NextSchemaFromIndex)
{
    public static RecursiveInvariantBuildResult Success(
        IReadOnlyList<RowShape> shapes,
        IReadOnlyList<ExecutionNode> nodes,
        IReadOnlyDictionary<string, RecursiveCteInvariantInput> inputs,
        int nextSchemaFromIndex) =>
        new(true, string.Empty, shapes, nodes, inputs, nextSchemaFromIndex);

    public static RecursiveInvariantBuildResult Unsupported(string reason) =>
        new(false, reason, [], [], new Dictionary<string, RecursiveCteInvariantInput>(), 0);
}
