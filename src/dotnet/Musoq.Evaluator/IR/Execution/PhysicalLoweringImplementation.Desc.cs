using System.Linq;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private ExecutionPlanBuildResult BuildDesc(PhysicalDescNode desc, string identifier)
    {
        var runtimeContextId = string.IsNullOrWhiteSpace(desc.SourceContextId)
            ? identifier
            : desc.SourceContextId;
        var descriptions = CreateNativeQueryDescriptions(desc);
        var node = new ExecutionReturnDesc(
            desc.SchemaName,
            desc.MethodName,
            desc.Type,
            desc.Column,
            desc.Arguments.Select(argument => ExecutionExpressionConverter.Convert(argument)).ToArray(),
            runtimeContextId,
            DefaultSchemaFromIndex,
            descriptions is null ? CreateQueryColumnMetadata(desc) : null,
            desc.ColumnSpan,
            desc.ArgumentDescriptions) { QueryDescriptions = descriptions };

        return ExecutionPlanBuildResult.CreateSupported(new ExecutionPlan(
            identifier,
            [],
            new ExecutionBlock([node])));
    }

    private IReadOnlyList<Musoq.Schema.QueryColumnDescription>? CreateNativeQueryDescriptions(PhysicalDescNode desc)
    {
        if (desc.Type != DescType.Query || desc.QueryOutputSchema is not { } schema ||
            !schema.Columns.Any(static column => GeneratedScalarGenericBinding.RequiresGeneratedSpecialization(column.Type)))
            return null;
        var descriptions = schema.Columns.Select(column =>
        {
            var expression = desc.QueryProjection?.FirstOrDefault(field => field.OutputIndex == column.Index)?.Expression;
            var generated = expression is null ? null : _shapeResolver.ScriptValues.ResolveGeneratedTypeName(expression);
            if (generated is null && GeneratedScalarGenericBinding.RequiresGeneratedSpecialization(column.Type))
                throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
                    $"DESC QUERY column '{column.Name}' has no authoritative native result description.", Musoq.Parser.TextSpan.Empty);
            var label = generated is null ? column.Type.FullName ?? column.Type.Name
                : _shapeResolver.ScriptValues.DescribeGeneratedTypeName(generated);
            return new Musoq.Schema.QueryColumnDescription(column.Name, column.Index, label);
        }).ToArray();
        return ExecutionIrCollections.Freeze(descriptions);
    }

    private static ExecutionColumnMetadata? CreateQueryColumnMetadata(PhysicalDescNode desc)
    {
        if (desc.Type != DescType.Query)
            return null;

        var schema = desc.QueryOutputSchema
            ?? throw UnsupportedShape.Of("DESC QUERY is missing bound query output schema.");

        return new ExecutionColumnMetadata(
            "descQuery",
            schema.Columns
                .Select(static column => new ExecutionColumnMetadataField(column.Name, column.Index, column.Type))
                .ToArray(),
            ExecutionColumnMetadataKind.TableColumns);
    }
}
