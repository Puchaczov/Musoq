using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static TableRowShape CreateTableRowShape(PhysicalCteRefNode cteRef)
    {
        return new TableRowShape(
            cteRef.Alias,
            cteRef.OutputSchema.Columns.Select(column =>
            {
                var field = new FieldBinding(
                    column.Name,
                    $"{cteRef.Alias}.{column.Name}",
                    column.Index,
                    column.Type,
                    FieldNullability.Unknown,
                    new PositionalAccess(column.Index));
                return column.IntendedTypeName is { Length: > 0 } generatedTypeName
                    ? field with { GeneratedTypeName = generatedTypeName }
                    : field;
            }).ToArray());
    }

    private static TableRowShape CreateTypedTableRowShape(PhysicalCteRefNode cteRef, GeneratedRowShape cteShape)
    {
        return new TableRowShape(
            cteRef.Alias,
            cteShape.Fields.Select(field =>
                FieldBindingRebinder.Rebind(
                    field,
                    CreateTypedStoredGeneratedRowAccess(cteShape, field),
                    $"{cteRef.Alias}.{field.Name}")).ToArray(),
            CreateTypedStoredGeneratedRowContextBindings(cteShape),
            cteShape.TypeName);
    }

    private static ExecutionVariable CreateSourceVariable(
        PhysicalNode source,
        RowShape sourceShape,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null)
    {
        return source switch
        {
            PhysicalSchemaScanNode scan when sourceShape is GeneratedRowShape { IsQueryScopedRow: true } queryRowShape =>
                new ExecutionVariable(scan.Alias, typeof(object), queryRowShape.TypeName),
            PhysicalSchemaScanNode scan => new ExecutionVariable(scan.Alias, RowShapeLookup.ResolveSourceRuntimeType(sourceShape)),
            PhysicalInterpretSourceNode interpret => new ExecutionVariable(interpret.Alias, RowShapeLookup.ResolveSourceRuntimeType(sourceShape)),
            PhysicalPropertySourceNode property => new ExecutionVariable(property.Alias, RowShapeLookup.ResolveSourceRuntimeType(sourceShape)),
            PhysicalAccessMethodSourceNode accessMethod => new ExecutionVariable(accessMethod.Alias, RowShapeLookup.ResolveSourceRuntimeType(sourceShape)),
            PhysicalValuesScanNode values when sourceShape is ValuesRowShape valuesShape => new ExecutionVariable(
                values.Alias,
                typeof(object),
                valuesShape.GeneratedShape.TypeName),
            PhysicalUnpivotNode unpivot when sourceShape is ValuesRowShape unpivotShape => new ExecutionVariable(
                unpivot.Alias,
                typeof(object),
                unpivotShape.GeneratedShape.TypeName),
            PhysicalCteRefNode cteRef => new ExecutionVariable(
                cteRef.Alias,
                typeof(Row),
                ResolveCteGeneratedRowShape(cteRef, cteShapesByName)?.TypeName),
            _ => throw UnsupportedShape.Of($"Source node '{source.GetType().Name}'", "Execution IR lowering")
        };
    }
}
