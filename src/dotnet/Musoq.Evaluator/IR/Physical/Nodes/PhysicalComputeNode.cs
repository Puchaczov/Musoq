using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Physical.Nodes;

/// <summary>
/// Adds statically selected scalar fields to an existing row boundary. The
/// node is deliberately target-neutral; lowerers choose ordinary locals,
/// generated row fields, or typed arrays for the carrier.
/// </summary>
public sealed record PhysicalComputeNode(
    PhysicalNode Input,
    IReadOnlyList<ProjectedField> ComputedFields) : PhysicalNode(CreateOutputSchema(Input, ComputedFields))
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Input];

    private static OutputSchema CreateOutputSchema(
        PhysicalNode input,
        IReadOnlyList<ProjectedField> computedFields)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(computedFields);

        var columns = new ColumnSchema[input.OutputSchema.Columns.Length + computedFields.Count];
        Array.Copy(input.OutputSchema.Columns, columns, input.OutputSchema.Columns.Length);
        for (var index = 0; index < computedFields.Count; index++)
        {
            var field = computedFields[index];
            columns[input.OutputSchema.Columns.Length + index] = new ColumnSchema(
                field.OutputName,
                field.Expression.ReturnType,
                input.OutputSchema.Columns.Length + index);
        }

        return new OutputSchema(columns);
    }
}
