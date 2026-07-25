using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private RowShape? ResolveSourceShape(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null)
    {
        return source switch
        {
            PhysicalSchemaScanNode scan => _shapeResolver.ResolveSourceShape(scan),
            PhysicalInterpretSourceNode interpret when ValidateInterpretSource(interpret).IsBuilt =>
                _shapeResolver.ResolveInterpretSourceShape(interpret),
            PhysicalPropertySourceNode property => _shapeResolver.ResolvePropertySourceShape(property),
            PhysicalAccessMethodSourceNode accessMethod => _shapeResolver.ResolveAccessMethodSourceShape(accessMethod),
            PhysicalValuesScanNode values => CreateValuesRowShape(values),
            PhysicalUnpivotNode unpivot => CreateUnpivotRowShape(unpivot),
            PhysicalCteRefNode cteRef when ResolveCteGeneratedRowShape(cteRef, cteShapesByName) is { } cteShape =>
                CreateTypedTableRowShape(cteRef, cteShape),
            PhysicalCteRefNode cteRef when ResolveCteStoredRowShape(cteRef, cteShapesByName) is { } cteShape =>
                CreateMaterializedTransitionTableRowShape(cteRef.Alias, cteShape),
            PhysicalCteRefNode cteRef when cteIndexes.ContainsKey(cteRef.CteName) => CreateTableRowShape(cteRef),
            _ => null
        };
    }

    private InterpretSourceValidationResult ValidateInterpretSource(PhysicalInterpretSourceNode interpret)
    {
        if (interpret.Kind is not (
            InterpretSourceKind.Interpret or
            InterpretSourceKind.InterpretAt or
            InterpretSourceKind.Parse or
            InterpretSourceKind.TryInterpret or
            InterpretSourceKind.TryParse or
            InterpretSourceKind.PartialInterpret or
            InterpretSourceKind.PartialParse))
        {
            return InterpretSourceValidationResult.Unsupported(
                $"Execution IR interpret-source lowering currently supports Interpret, InterpretAt, Parse, TryInterpret, TryParse, PartialInterpret, and PartialParse sources. Found {interpret.Kind}.");
        }

        if (_schemaRegistry == null ||
            !_schemaRegistry.TryGetSchema(interpret.SchemaName, out var registration) ||
            registration?.Node == null)
        {
            return InterpretSourceValidationResult.Unsupported(
                $"Execution IR interpret-source lowering requires schema-registry metadata for schema '{interpret.SchemaName}'.");
        }

        return registration.Node switch
        {
            TextSchemaNode text => ValidateTextInterpretSource(text),
            BinarySchemaNode binary => ValidateBinaryInterpretSource(binary),
            _ => InterpretSourceValidationResult.Unsupported(
            $"Execution IR interpret-source lowering does not support schema node '{registration.Node.GetType().Name}'.")
        };
    }
}
