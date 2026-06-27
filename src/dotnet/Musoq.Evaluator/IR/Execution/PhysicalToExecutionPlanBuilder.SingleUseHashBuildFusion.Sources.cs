using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private bool TryCreateFusedCteHashBuildSource(
        PhysicalCteDefinition definition,
        int definitionIndex,
        PhysicalCteRefNode cteRef,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        int schemaFromIndex,
        out FusedCteHashBuildSource fusion)
    {
        fusion = null!;
        var unwrapped = UnwrapSingleStatement(definition.Plan);
        var pipeline = DecomposeSupportedPipeline(unwrapped);
        if (pipeline == null ||
            pipeline.Filter != null ||
            pipeline.PostOperations.Count != 0 ||
            pipeline.Project.IsDistinct ||
            pipeline.Source is not (PhysicalSchemaScanNode or PhysicalValuesScanNode))
        {
            return false;
        }

        var producerShape = ResolveSourceShape(pipeline.Source, cteIndexes, cteShapesByName);
        if (producerShape == null || producerShape is ExpandoAdapterShape)
            return false;

        var cteTableName = CreateCteTableName(definitionIndex, cteDefinitionNames);
        var sourceRowsScope = CreateSourceRowsScope(cteTableName);
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(producerShape);
        var producerVariable = CreateSourceVariable(pipeline.Source, producerShape, cteShapesByName);
        var rowShape = CreateGeneratedShape(
            $"Cte{definitionIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}Row0",
            pipeline.Project.Fields,
            sourceLookup);
        var rowValues = pipeline.Project.Fields
            .Select(field => new ExecutionRowValue(field.OutputName, ConvertProjectedExpression(field, sourceLookup)))
            .ToArray();
        var payloadShape = CreateFusedHashPayloadShape(cteRef.Alias, rowShape);
        var definitionShapes = payloadShape == null
            ? (IReadOnlyList<RowShape>)[producerShape, rowShape]
            : [producerShape, payloadShape];

        fusion = new FusedCteHashBuildSource(
            rowShape,
            definitionShapes,
            producerShape,
            producerVariable,
            CreateSourceSetup(pipeline.Source, producerShape, producerVariable, schemaFromIndex, cteIndexes, sourceRowsScope),
            CreateSourceRowsExpression(pipeline.Source, producerShape, cteIndexes, cteShapesByName, sourceRowsScope),
            rowValues,
            CreateContextValues(sourceLookup),
            CreateContextLayout(sourceLookup),
            pipeline.Source is PhysicalSchemaScanNode ? 1 : 0,
            payloadShape);
        return true;
    }

    private bool TryBuildFusedCteHashBuildJoinSource(
        PhysicalCteRefNode cteRef,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        out JoinSource source)
    {
        source = null!;
        if (_fusedCteHashBuildSources == null ||
            !_fusedCteHashBuildSources.TryGetValue(cteRef.CteName, out var fusion))
        {
            return false;
        }

        var shape = ResolveSourceShape(cteRef, cteIndexes, cteShapesByName);
        if (shape == null)
            return false;

        var variable = CreateSourceVariable(cteRef, shape, cteShapesByName);
        var shapes = new List<RowShape>(fusion.DefinitionShapes.Count + 2);
        shapes.AddRange(fusion.DefinitionShapes);
        shapes.Add(shape);
        FusedHashPayload? payload = null;
        if (shape is TableRowShape tableRow &&
            TryCreateFusedHashPayload(fusion, tableRow, out var payloadShape, out var payloadTableRow, out var payloadValues))
        {
            shape = payloadTableRow;
            variable = new ExecutionVariable(cteRef.Alias, typeof(Row), payloadShape.TypeName);
            payload = new FusedHashPayload(payloadShape, payloadValues);
            shapes.Clear();
            shapes.AddRange(fusion.DefinitionShapes);
            shapes.Add(shape);
        }

        source = new JoinSource(
            cteRef,
            shape,
            variable,
            fusion.ProducerSetup.ToList(),
            fusion.ProducerRows,
            shapes,
            fusion.SchemaSourceCount,
            FusedHashBuild: fusion,
            FusedHashPayload: payload);
        return true;
    }
}
