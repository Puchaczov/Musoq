using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Expands concrete context values before a target receives the execution plan.</summary>
internal sealed class TypedContextTransportLowering : ExecutionIrRewriter
{
    private IReadOnlyDictionary<string, GeneratedRowShape> _shapes = new Dictionary<string, GeneratedRowShape>();

    private IReadOnlyDictionary<string, HashSet<string>> _variableTypes = new Dictionary<string, HashSet<string>>();

    public override ExecutionPlan RewritePlan(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.Shapes.OfType<GeneratedRowShape>().Any(static shape => shape.Contexts.Count > 0))
            return plan;

        _shapes = plan.Shapes.OfType<GeneratedRowShape>()
            .GroupBy(static shape => shape.TypeName, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        _variableTypes = GeneratedRowVariableTypes.Collect(plan.Body);
        return base.RewritePlan(plan);
    }

    protected override ExecutionNode RewriteAppendRow(ExecutionAppendRow node)
    {
        var rewritten = (ExecutionAppendRow)base.RewriteAppendRow(node);
        var layout = Expand(rewritten.ContextLayout, rewritten.RowShape.Contexts);
        if (layout == null)
            return rewritten;

        return rewritten with { Contexts = layout.Segments.Select(static segment => segment.Value).ToArray(), ContextLayout = layout };
    }

    protected override ExecutionNode RewriteCreateGeneratedRow(ExecutionCreateGeneratedRow node)
    {
        var rewritten = (ExecutionCreateGeneratedRow)base.RewriteCreateGeneratedRow(node);
        var layout = Expand(rewritten.ContextLayout, rewritten.RowShape.Contexts);
        if (layout == null)
            return rewritten;

        return rewritten with { Contexts = layout.Segments.Select(static segment => segment.Value).ToArray(), ContextLayout = layout };
    }

    private ExecutionContextLayout? Expand(ExecutionContextLayout? layout, IReadOnlyList<FieldBinding> fields)
    {
        if (layout == null || layout.Segments.All(static segment =>
                segment is { Kind: ExecutionContextSegmentKind.Single, Count: 1 }))
            return null;

        var segments = new List<ExecutionContextSegment>();
        foreach (var segment in layout.Segments)
        {
            if (!Append(segment, segments, fields))
                throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
                    "Execution source contexts require concrete carrier types and presence information.", Musoq.Parser.TextSpan.Empty);
        }

        return new ExecutionContextLayout(segments);
    }

    private bool Append(ExecutionContextSegment segment, List<ExecutionContextSegment> result, IReadOnlyList<FieldBinding> fields)
    {
        if (segment is { Kind: ExecutionContextSegmentKind.Single, Count: 1 })
        {
            result.Add(segment);
            return true;
        }

        if (segment.Value is ExecutionNullContextArray nulls)
            return AppendNulls(nulls, result, fields);

        if (segment.Kind == ExecutionContextSegmentKind.Row || segment.Value is ExecutionRowContextsRead)
            return AppendRow(segment, result);

        if (segment is not { Kind: ExecutionContextSegmentKind.Array, Value: ExecutionContextArray array })
            return false;

        foreach (var nested in array.Segments)
        {
            if (!Append(nested, result, fields))
                return false;
        }

        return true;
    }

    private bool AppendRow(ExecutionContextSegment segment, List<ExecutionContextSegment> result)
    {
        var row = segment.Value switch
        {
            ExecutionVariableRead read => read.Variable,
            ExecutionRowContextsRead read => read.Row,
            _ => null
        };
        if (row == null)
            return false;
        var typeName = row.GeneratedRowTypeName;
        if (typeName == null && _variableTypes.TryGetValue(row.Name, out var candidates) && candidates.Count == 1)
            typeName = candidates.Single();
        if (typeName == null || !_shapes.TryGetValue(typeName, out var shape) || shape.Contexts.Count != segment.Count)
            return false;

        for (var index = 0; index < shape.Contexts.Count; index++)
        {
            var field = shape.Contexts[index];
            var read = new ExecutionFieldRead(row.Name, field.Name, field.Type,
                new GeneratedRowContextAccess(typeName, index), field.GeneratedTypeName);
            result.Add(new ExecutionContextSegment(ExecutionContextSegmentKind.Single, read, 1));
        }

        return true;
    }

    private static bool AppendNulls(ExecutionNullContextArray nulls, List<ExecutionContextSegment> result,
        IReadOnlyList<FieldBinding> fields)
    {
        if (result.Count + nulls.Count > fields.Count)
            return false;

        for (var index = 0; index < nulls.Count; index++)
        {
            var field = fields[result.Count];
            result.Add(new ExecutionContextSegment(ExecutionContextSegmentKind.Single,
                new ExecutionLiteral((object?)null, field.Type), 1));
        }

        return true;
    }
}
