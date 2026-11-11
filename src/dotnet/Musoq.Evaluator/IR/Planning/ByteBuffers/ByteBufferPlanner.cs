using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Planning;

/// <summary>Selects byte-buffer execution before physical expressions enter executable lowering.</summary>
internal static class ByteBufferPlanner
{
    internal static PhysicalNode Plan(PhysicalNode root, SchemaRegistry? registry, CancellationToken cancellationToken)
    {
        var expressions = new ProducerPlanner(cancellationToken);
        var bounds = registry is null ? null : new BinarySchemaReadBounds(registry, cancellationToken);
        var hasOwnedProjection = false;
        var local = Rewrite(root);
        if (!hasOwnedProjection)
            return local;
        return new ByteBufferFlowPlanner(bounds, cancellationToken).Plan(local);

        PhysicalNode Rewrite(PhysicalNode node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rewritten = PhysicalPlanRewriter.RewriteChildren(node, Rewrite);
            return rewritten switch
            {
                PhysicalProjectNode project => RewriteProject(project),
                PhysicalInterpretSourceNode source => RewriteInterpret(source),
                PhysicalFilterNode filter => RewriteFilter(filter),
                _ => rewritten,
            };
        }

        PhysicalNode RewriteProject(PhysicalProjectNode project)
        {
            ProjectedField[]? fields = null;
            for (var index = 0; index < project.Fields.Length; index++)
            {
                var field = project.Fields[index];
                var expression = expressions.Visit(field.Expression);
                hasOwnedProjection |=
                    expression is MethodCall { BufferPlan.Program.Result: ByteBufferResultKind.Snapshot };
                if (ReferenceEquals(expression, field.Expression))
                    continue;
                fields ??= (ProjectedField[])project.Fields.Clone();
                fields[index] = field with { Expression = expression };
            }
            return fields is null ? project : project with { Fields = fields };
        }

        PhysicalNode RewriteInterpret(PhysicalInterpretSourceNode source)
        {
            IrExpression[]? arguments = null;
            for (var index = 0; index < source.Arguments.Length; index++)
            {
                var argument = expressions.Visit(source.Arguments[index]);
                if (
                    index == 0
                    && source.Kind
                        is not (
                            InterpretSourceKind.Parse
                            or InterpretSourceKind.TryParse
                            or InterpretSourceKind.PartialParse
                        )
                )
                {
                    var maximum =
                        source.Kind == InterpretSourceKind.InterpretAt ? null : bounds?.MaximumBytes(source.SchemaName);
                    var requirement = maximum.HasValue
                        ? ByteBufferRequirement.Prefix(maximum.Value)
                        : ByteBufferRequirement.Complete;
                    argument = expressions.Producer(argument, requirement);
                }
                if (ReferenceEquals(argument, source.Arguments[index]))
                    continue;
                arguments ??= (IrExpression[])source.Arguments.Clone();
                arguments[index] = argument;
            }
            return arguments is null ? source : source with { Arguments = arguments };
        }

        PhysicalNode RewriteFilter(PhysicalFilterNode filter)
        {
            var predicate = expressions.Visit(filter.Predicate);
            return ReferenceEquals(predicate, filter.Predicate) ? filter : filter with { Predicate = predicate };
        }
    }

    private sealed class ProducerPlanner(CancellationToken cancellationToken) : IrExpressionRewriter
    {
        private int _nextValue;
        private readonly ScalarEvaluationRegion _region = new(
            "byte-buffers",
            ScalarEvaluationRegionKind.Materialization,
            false,
            false
        );

        protected override IrExpression VisitMethodCall(MethodCall node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var call = (MethodCall)base.VisitMethodCall(node);
            if (call.BufferSemantics is { Kind: ByteBufferCallableKind.FixedWidthRead } reader)
            {
                var input = Producer(call.Arguments[0], ByteBufferRequirement.Prefix(reader.ReadWidth));
                return ReferenceEquals(input, call.Arguments[0])
                    ? call
                    : call with
                    {
                        Arguments = [input, .. call.Arguments.Skip(1)],
                    };
            }
            if (
                call.BufferSemantics is { Kind: ByteBufferCallableKind.Length }
                && call.Arguments[0] is MethodCall producer
            )
            {
                var lengthProgram = ByteBufferProgramBuilder.Build(
                    producer,
                    ByteBufferRequirement.Length,
                    ByteBufferResultKind.Length
                );
                if (lengthProgram is not null)
                    return call with { BufferPlan = ProgramPlan(call, lengthProgram) };
            }
            var optimized = Producer(call, ByteBufferRequirement.Complete);
            if (!ReferenceEquals(optimized, call))
                return optimized;
            var operation = call.BufferSemantics?.Kind switch
            {
                ByteBufferCallableKind.Materialize => ByteBufferOperationKind.Materialize,
                ByteBufferCallableKind.Concatenate => ByteBufferOperationKind.Concatenate,
                ByteBufferCallableKind.Skip or ByteBufferCallableKind.Take or ByteBufferCallableKind.SkipAndTake =>
                    ByteBufferOperationKind.Range,
                _ => (ByteBufferOperationKind?)null,
            };
            if (!operation.HasValue)
                return call;
            var arguments = ImmutableArray.CreateBuilder<ByteBufferValuePlan?>(call.Arguments.Count);
            foreach (var argument in call.Arguments)
                arguments.Add(
                    argument.ReturnType == typeof(byte[]) && argument is not MethodCall { BufferPlan: not null }
                        ? Borrow(argument)
                        : null
                );
            return call with
            {
                BufferPlan = CreatePlan(operation.Value, call.Arguments) with
                {
                    ArgumentPlans = arguments.ToImmutable(),
                },
            };
        }

        internal IrExpression Producer(IrExpression expression, ByteBufferRequirement requirement)
        {
            if (expression is not MethodCall call)
                return expression;
            var program = ByteBufferProgramBuilder.Build(call, requirement);
            return program is null ? call : call with { BufferPlan = ProgramPlan(call, program) };
        }

        private ByteBufferValuePlan ProgramPlan(MethodCall call, ByteBufferProgram program) =>
            CreatePlan(ByteBufferOperationKind.Snapshot, call.Arguments) with
            {
                Program = program,
                Requirement = program.Requirement,
                Materialization = ByteBufferMaterialization.PlannedOperations,
            };

        private ByteBufferValuePlan Borrow(IrExpression argument)
        {
            var plan = CreatePlan(ByteBufferOperationKind.BorrowArray, [argument]);
            return argument is MethodCall
                ? plan with
                {
                    Facts = plan.Facts with { Provenance = ByteBufferProvenance.OpaqueProducer },
                }
                : plan;
        }

        private ByteBufferValuePlan CreatePlan(
            ByteBufferOperationKind operation,
            System.Collections.Generic.IEnumerable<IrExpression> arguments
        )
        {
            var identity = _nextValue++;
            var borrowed = operation is ByteBufferOperationKind.BorrowArray or ByteBufferOperationKind.Range;
            return new(
                operation,
                new(
                    identity,
                    borrowed ? ByteBufferOwnership.Borrowed : ByteBufferOwnership.Owned,
                    ByteBufferNullState.Unknown,
                    _region,
                    identity
                )
                {
                    Provenance = operation switch
                    {
                        ByteBufferOperationKind.Concatenate => ByteBufferProvenance.Concatenation,
                        ByteBufferOperationKind.BorrowArray or ByteBufferOperationKind.Range =>
                            ByteBufferProvenance.OriginalArray,
                        _ => ByteBufferProvenance.Snapshot,
                    },
                    LogicalLength = new(identity, null),
                },
                arguments,
                ByteBufferRequirement.Complete
            );
        }
    }
}
