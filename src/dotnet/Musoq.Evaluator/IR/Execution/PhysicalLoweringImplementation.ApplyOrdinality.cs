using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
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
        var fields = shape.Fields.ToList();
        var ordinalIndex = fields.FindIndex(field =>
            string.Equals(field.Name, ApplyOrdinalityColumnName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.QualifiedName, ApplyOrdinalityColumnName, StringComparison.OrdinalIgnoreCase));
        if (ordinalIndex >= 0)
        {
            fields[ordinalIndex] = fields[ordinalIndex] with
            {
                AccessStrategy = new ApplyOrdinalityAccess(ordinal.Name)
            };
        }
        else
        {
            var alias = RowShapeLookup.ResolveSourceAlias(shape);
            var outputIndex = fields.Count == 0
                ? 0
                : fields.Max(field => field.OutputIndex) + 1;
            fields.Add(new FieldBinding(
                ApplyOrdinalityColumnName,
                $"{alias}.{ApplyOrdinalityColumnName}",
                outputIndex,
                typeof(int),
                FieldNullability.Unknown,
                new ApplyOrdinalityAccess(ordinal.Name)));
        }

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

    private static ExecutionNode CreateApplySourceLoop(
        JoinSource source,
        ExecutionBlock loopBody,
        ExecutionExpression? ordinalityIncrementCondition = null)
    {
        return source.OrdinalityVariable == null
            ? CreateSourceLoop(source.Shape, source.Rows, source.Variable, loopBody)
            : ordinalityIncrementCondition == null
                ? CreateSourceLoopWithOrdinality(source.Shape, source.Rows, source.Variable, source.OrdinalityVariable, loopBody)
                : CreateConditionallyOrdinalApplySourceLoop(source, loopBody, ordinalityIncrementCondition);
    }

    private static ExecutionNode CreateConditionallyOrdinalApplySourceLoop(
        JoinSource source,
        ExecutionBlock loopBody,
        ExecutionExpression ordinalityIncrementCondition)
    {
        var ordinal = source.OrdinalityVariable
            ?? throw new InvalidOperationException("Conditional APPLY ordinality requires an ordinal variable.");
        var increment = new ExecutionAssign(
            ordinal,
            new ExecutionBinary(
                BinaryOpKind.Add,
                new ExecutionVariableRead(ordinal),
                new ExecutionLiteral(1, typeof(int)),
                typeof(int)));
        var body = new ExecutionBlock(
        [
            ..loopBody.Nodes,
            new ExecutionIf(ordinalityIncrementCondition, new ExecutionBlock([increment]))
        ]);

        return new ExecutionScopedBlock(new ExecutionBlock(
        [
            new ExecutionLet(ordinal, new ExecutionLiteral(0, typeof(int))),
            CreateSourceLoop(source.Shape, source.Rows, source.Variable, body)
        ]));
    }
}
