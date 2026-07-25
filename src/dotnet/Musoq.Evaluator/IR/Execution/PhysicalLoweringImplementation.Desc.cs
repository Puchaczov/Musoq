using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionPlanBuildResult BuildDesc(PhysicalDescNode desc, string identifier)
    {
        var runtimeContextId = string.IsNullOrWhiteSpace(desc.SourceContextId)
            ? identifier
            : desc.SourceContextId;
        var node = new ExecutionReturnDesc(
            desc.SchemaName,
            desc.MethodName,
            desc.Type,
            desc.Column,
            desc.Arguments.Select(argument => ExecutionExpressionConverter.Convert(argument)).ToArray(),
            runtimeContextId,
            DefaultSchemaFromIndex,
            CreateQueryColumnMetadata(desc));

        return ExecutionPlanBuildResult.CreateSupported(new ExecutionPlan(
            identifier,
            [],
            new ExecutionBlock([node])));
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
