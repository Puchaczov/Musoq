using System.Collections.Immutable;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ByteBufferFlowPlanner
{
    private PhysicalNode Rewrite(PhysicalNode node)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rewritten = PhysicalPlanRewriter.RewriteChildren(node, Rewrite);
        var boundary = _boundaries[node];
        var expressions = new ViewFieldRewriter(boundary.Input);
        if (rewritten is PhysicalProjectNode project)
        {
            var fields = project
                .Fields.Select(
                    (field, index) =>
                    {
                        var expression = expressions.Visit(field.Expression);
                        if (boundary.Fields[index] is { } value && ReferenceEquals(value.Producer, field.Expression))
                            expression = Refine(value);
                        return field with { Expression = expression };
                    }
                )
                .ToArray();
            rewritten = new PhysicalProjectNode(fields, project.Input) { IsDistinct = project.IsDistinct };
        }
        if (rewritten is PhysicalInterpretSourceNode source)
            rewritten = source with
            {
                Arguments = source.Arguments.Select(expressions.Visit).ToArray(),
                FieldReader = CreateReader((PhysicalInterpretSourceNode)node),
            };
        if (rewritten is PhysicalFilterNode filter)
            rewritten = filter with { Predicate = expressions.Visit(filter.Predicate) };
        var columns = rewritten
            .OutputSchema.Columns.Select(
                (column, index) =>
                {
                    var value =
                        index < boundary.Output.Columns.Count && boundary.Output.Columns[index].Name == column.Name
                            ? boundary.Output.Columns[index].Value
                            : boundary.Output.ResolveName(column.Name);
                    if (value is { StoresLength: true })
                        return column with
                        {
                            Type = typeof(int?),
                            SourceReadType = typeof(int?),
                            IntendedTypeName = null,
                        };
                    return value is { UsesView: true }
                        ? column with
                        {
                            IntendedTypeName = ByteBufferStorageTypes.OwnedView,
                        }
                        : column;
                }
            )
            .ToArray();
        return rewritten with { OutputSchema = new OutputSchema(columns) };
    }

    private BinaryFieldReaderPlan? CreateReader(PhysicalInterpretSourceNode source)
    {
        if (!_parsedFields.TryGetValue(source, out var fields))
            return null;
        var demands = fields
            .Where(field => !field.Value.Requirement.RequiresCompleteArray)
            .ToImmutableDictionary(
                field => field.Key,
                field => field.Value.StoresLength ? BinaryFieldDemand.Length : BinaryFieldDemand.Discard,
                StringComparer.OrdinalIgnoreCase
            );
        if (demands.Count == 0)
            return null;
        string container;
        do
        {
            container = $"__MusoqFieldReader{_nextReaderId++}";
        } while (registry!.ContainsSchema(container));
        return new(source.SchemaName, container, demands);
    }

    private static MethodCall Refine(ByteBufferFlowValue value)
    {
        var plan = value.Producer!.BufferPlan!;
        var program = plan.Program! with
        {
            Requirement = value.Requirement,
            Result = value.UsesView ? ByteBufferResultKind.View : ByteBufferResultKind.Snapshot,
            InputIsView = value.Input?.UsesView == true,
        };
        return value.Producer with { BufferPlan = plan with { Requirement = value.Requirement, Program = program } };
    }

    private sealed class ViewFieldRewriter(ByteBufferFlowBindings input) : IrExpressionRewriter
    {
        protected override IrExpression VisitMethodCall(MethodCall node)
        {
            if (
                node.BufferSemantics?.Kind == ByteBufferCallableKind.Length
                && node.Arguments[0] is ColumnRef field
                && input.Resolve(field) is { StoresLength: true }
            )
                return VisitColumnRef(field);
            var call = (MethodCall)base.VisitMethodCall(node);
            if (call.BufferPlan is { Program: { } program } plan && ResolveInput(program, input) is { UsesView: true })
                return call with { BufferPlan = plan with { Program = program with { InputIsView = true } } };
            return call;
        }

        protected override IrExpression VisitColumnRef(ColumnRef node) =>
            input.Resolve(node) switch
            {
                { StoresLength: true } => node with { ReturnType = typeof(int?), GeneratedTypeName = null },
                { UsesView: true } => node with { GeneratedTypeName = ByteBufferStorageTypes.OwnedView },
                _ => node,
            };
    }
}
