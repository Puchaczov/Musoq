using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private BuildResult<ProjectedField[]> CreateNestedSourceProjectionFields(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName)
    {
        var fields = new List<ProjectedField>();
        var collection = CollectNestedSourceProjectionFields(source, cteIndexes, cteShapesByName, fields);
        if (!collection.Supported)
            return BuildResult<ProjectedField[]>.Unsupported(collection.UnsupportedReason);

        return BuildResult<ProjectedField[]>.Success(fields.ToArray());
    }

    private ProjectionFieldCollectionResult CollectNestedSourceProjectionFields(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        List<ProjectedField> fields)
    {
        if (source is PhysicalNestedLoopApplyNode apply)
        {
            var left = CollectNestedSourceProjectionFields(apply.Left, cteIndexes, cteShapesByName, fields);
            if (!left.Supported)
                return left;

            return CollectNestedSourceProjectionFields(apply.Right, cteIndexes, cteShapesByName, fields);
        }

        if (source is PhysicalNestedLoopJoinNode nestedLoopJoin)
        {
            var left = CollectNestedSourceProjectionFields(nestedLoopJoin.Left, cteIndexes, cteShapesByName, fields);
            if (!left.Supported)
                return left;

            if (JoinKindSemantics.ProducesLeftOnly(nestedLoopJoin.Kind))
                return ProjectionFieldCollectionResult.Success();

            return CollectNestedSourceProjectionFields(nestedLoopJoin.Right, cteIndexes, cteShapesByName, fields);
        }

        if (source is PhysicalHashJoinNode hashJoin)
        {
            var left = CollectNestedSourceProjectionFields(hashJoin.Left, cteIndexes, cteShapesByName, fields);
            if (!left.Supported)
                return left;

            if (JoinKindSemantics.ProducesLeftOnly(hashJoin.Kind))
                return ProjectionFieldCollectionResult.Success();

            return CollectNestedSourceProjectionFields(hashJoin.Right, cteIndexes, cteShapesByName, fields);
        }

        if (source is PhysicalSortMergeJoinNode sortMergeJoin)
        {
            var left = CollectNestedSourceProjectionFields(sortMergeJoin.Left, cteIndexes, cteShapesByName, fields);
            if (!left.Supported)
                return left;

            if (JoinKindSemantics.ProducesLeftOnly(sortMergeJoin.Kind))
                return ProjectionFieldCollectionResult.Success();

            return CollectNestedSourceProjectionFields(sortMergeJoin.Right, cteIndexes, cteShapesByName, fields);
        }

        if (source is PhysicalInterpretSourceNode interpret)
        {
            var interpretSource = ValidateInterpretSource(interpret);
            if (!interpretSource.Supported)
                return ProjectionFieldCollectionResult.Unsupported(interpretSource.UnsupportedReason);
        }

        var shape = ResolveSourceShape(source, cteIndexes, cteShapesByName);
        if (shape == null)
        {
            return ProjectionFieldCollectionResult.Unsupported(
                $"Execution IR apply lowering cannot resolve nested apply source shape for {source.GetType().Name}.");
        }

        foreach (var field in shape.Fields)
        {
            fields.Add(new ProjectedField(
                CreateMaterializedSourceProjectionFieldName(field),
                CreateMaterializedSourceProjectionColumnRef(field),
                fields.Count));
        }

        return ProjectionFieldCollectionResult.Success();
    }

    private static string CreateMaterializedSourceProjectionFieldName(FieldBinding field)
    {
        return string.IsNullOrWhiteSpace(field.QualifiedName)
            ? field.Name
            : field.QualifiedName;
    }

    private static ColumnRef CreateMaterializedSourceProjectionColumnRef(FieldBinding field)
    {
        var qualifiedName = CreateMaterializedSourceProjectionFieldName(field);
        var separatorIndex = qualifiedName.IndexOf('.', StringComparison.Ordinal);

        return separatorIndex > 0
            ? new ColumnRef(
                qualifiedName[..separatorIndex],
                qualifiedName[(separatorIndex + 1)..],
                field.Type.ClrType)
            : new ColumnRef(string.Empty, qualifiedName, field.Type.ClrType);
    }

    private static string CreateNestedJoinSourceAlias(PhysicalNode join, int schemaFromIndex)
    {
        return CreateIdentifierCandidate(
            $"join_{schemaFromIndex.ToString(CultureInfo.InvariantCulture)}_{CreateMaterializedSourceAliasFragment(join)}",
            schemaFromIndex);
    }

    private static string CreateNestedApplySourceAlias(PhysicalNestedLoopApplyNode apply, int schemaFromIndex)
    {
        return CreateIdentifierCandidate(
            $"apply_{schemaFromIndex.ToString(CultureInfo.InvariantCulture)}_{CreateMaterializedSourceAliasFragment(apply)}",
            schemaFromIndex);
    }

    private static string CreateNestedSourceShapeName(string sourceAlias)
    {
        return CreateIdentifierCandidate($"{sourceAlias}Row0", 0);
    }

    private static string CreateMaterializedSourceAliasFragment(PhysicalNode source)
    {
        return source switch
        {
            PhysicalSchemaScanNode scan => scan.Alias,
            PhysicalCteRefNode cteRef => cteRef.Alias,
            PhysicalInterpretSourceNode interpret => interpret.Alias,
            PhysicalPropertySourceNode property => property.Alias,
            PhysicalAccessMethodSourceNode accessMethod => accessMethod.Alias,
            PhysicalValuesScanNode values => values.Alias,
            PhysicalNestedLoopApplyNode apply => $"{CreateMaterializedSourceAliasFragment(apply.Left)}_{CreateMaterializedSourceAliasFragment(apply.Right)}",
            PhysicalNestedLoopJoinNode join => $"{CreateMaterializedSourceAliasFragment(join.Left)}_{CreateMaterializedSourceAliasFragment(join.Right)}",
            PhysicalHashJoinNode join => $"{CreateMaterializedSourceAliasFragment(join.Left)}_{CreateMaterializedSourceAliasFragment(join.Right)}",
            PhysicalSortMergeJoinNode join => $"{CreateMaterializedSourceAliasFragment(join.Left)}_{CreateMaterializedSourceAliasFragment(join.Right)}",
            _ => source.GetType().Name
        };
    }
}
