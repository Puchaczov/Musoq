using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private const string ApplyOrdinalityColumnName = "Ordinal";

    private static JoinSource AddApplyOrdinalityAccess(JoinSource source)
    {
        var alias = RowShapeLookup.ResolveSourceAlias(source.Shape);
        var ordinal = new ExecutionVariable(CreateApplyOrdinalityVariableName(alias), typeof(int));
        var shape = AddApplyOrdinalityAccess(source.Shape, ordinal);
        var shapes = source.Shapes
            .Select(current => ReferenceEquals(current, source.Shape) ? shape : current)
            .ToArray();

        return source with
        {
            Shape = shape,
            Shapes = shapes,
            OrdinalityVariable = ordinal
        };
    }

    private static RowShape AddApplyOrdinalityAccess(RowShape shape, ExecutionVariable ordinal)
    {
        var fields = shape.Fields
            .Select(field => string.Equals(field.Name, ApplyOrdinalityColumnName, StringComparison.OrdinalIgnoreCase)
                ? field with { AccessStrategy = new ApplyOrdinalityAccess(ordinal.Name) }
                : field)
            .ToArray();

        return shape switch
        {
            SourceEntityShape source => source with { Fields = fields },
            GeneratedRowShape generated => generated with { Fields = fields },
            TableRowShape tableRow => tableRow with { Fields = fields },
            ExpandoAdapterShape expando => expando with { Fields = fields },
            ValuesRowShape values => new ValuesRowShape(
                values.Alias,
                values.GeneratedShape with { Fields = fields }),
            _ => shape
        };
    }

    private static string CreateApplyOrdinalityVariableName(string alias)
    {
        return CreateIdentifierCandidate($"{alias}Ordinal", 0);
    }
}
