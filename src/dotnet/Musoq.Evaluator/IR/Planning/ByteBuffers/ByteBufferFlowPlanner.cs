using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

/// <summary>Combines observations of private owned values before selecting stored representations.</summary>
internal sealed partial class ByteBufferFlowPlanner(BinarySchemaReadBounds? bounds, CancellationToken cancellationToken)
{
    private readonly List<ByteBufferFlowValue> _values = [];
    private readonly Dictionary<PhysicalNode, FlowBoundary> _boundaries = new(ReferenceEqualityComparer.Instance);
    private bool _unresolved;

    internal PhysicalNode Plan(PhysicalNode root)
    {
        var output = Analyze(root, new FlowScope(), new ByteBufferFlowBindings());
        output.RequireComplete();
        foreach (var value in _values.AsEnumerable().Reverse())
        {
            if (value.Input is { } source && source.Identity >= value.Identity)
                return root;
            value.Input?.Require(ByteBufferDemandPreimage.For(value.Producer.BufferPlan!.Program!, value.Requirement));
        }
        return _unresolved || _values.All(value => value.Requirement.RequiresCompleteArray) ? root : Rewrite(root);
    }

    private ByteBufferFlowBindings Analyze(PhysicalNode node, FlowScope scope, ByteBufferFlowBindings correlated)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var output = node switch
        {
            PhysicalCteNode cte => AnalyzeCte(cte, scope, correlated),
            PhysicalMultiStatementNode statements => AnalyzeStatements(statements, scope, correlated),
            PhysicalCteRefNode reference => AnalyzeReference(reference, scope),
            PhysicalRecursiveCteNode recursive => AnalyzeConservative(recursive, scope, correlated),
            PhysicalProjectNode project => AnalyzeProject(project, scope, correlated),
            PhysicalNestedLoopApplyNode apply => AnalyzeApply(apply, scope, correlated),
            _ => AnalyzeOrdinary(node, scope, correlated),
        };
        if (!_boundaries.ContainsKey(node))
            _boundaries.Add(node, new(output, output, []));
        return output;
    }

    private ByteBufferFlowBindings AnalyzeCte(PhysicalCteNode cte, FlowScope scope, ByteBufferFlowBindings correlated)
    {
        var local = scope.Copy();
        foreach (var definition in cte.Definitions)
            local.Named[definition.Name] = Analyze(definition.Plan, local, correlated);
        return Analyze(cte.Query, local, correlated);
    }

    private ByteBufferFlowBindings AnalyzeStatements(
        PhysicalMultiStatementNode statements,
        FlowScope scope,
        ByteBufferFlowBindings correlated
    )
    {
        var local = scope.Copy();
        var output = new ByteBufferFlowBindings();
        for (var index = 0; index < statements.Statements.Length; index++)
        {
            output = Analyze(statements.Statements[index], local, correlated);
            var producers = statements
                .IntermediateProducers.Where(producer => producer.StatementIndex == index)
                .ToArray();
            foreach (var producer in producers)
                local.Intermediate[producer.Source.ProducerId] = output;
            if (producers.Length == 0 && index != statements.Statements.Length - 1)
                output.RequireComplete();
        }
        return output;
    }

    private ByteBufferFlowBindings AnalyzeReference(PhysicalCteRefNode reference, FlowScope scope)
    {
        var source = reference.IntermediateProducerId is { } identity
            ? scope.Intermediate.GetValueOrDefault(identity)
            : scope.Named.GetValueOrDefault(reference.CteName);
        if (source is null)
            _unresolved = true;
        return ByteBufferFlowBindings.Source(reference.OutputSchema, reference.Alias, source);
    }

    private ByteBufferFlowBindings AnalyzeProject(
        PhysicalProjectNode project,
        FlowScope scope,
        ByteBufferFlowBindings correlated
    )
    {
        var input = Analyze(project.Input, scope, correlated);
        var fields = new ByteBufferFlowValue?[project.Fields.Length];
        for (var index = 0; index < fields.Length; index++)
        {
            var expression = project.Fields[index].Expression;
            if (expression is MethodCall { BufferPlan.Program.Result: ByteBufferResultKind.Snapshot } producer)
            {
                fields[index] = new(_values.Count, producer);
                _values.Add(fields[index]!);
                var program = producer.BufferPlan!.Program!;
                fields[index]!.Input = ResolveInput(program, input);
                foreach (var operand in program.Inputs.Skip(fields[index]!.Input is null ? 0 : 1))
                    Observe(operand, input);
                continue;
            }
            if (expression is ColumnRef { ReturnType: var type } column && type == typeof(byte[]))
            {
                fields[index] = input.Resolve(column);
                if (fields[index] is null)
                    input.RequireComplete();
                continue;
            }
            Observe(expression, input);
        }
        var output = ByteBufferFlowBindings.Project(project.Fields, fields, input);
        if (project.IsDistinct)
            output.RequireComplete();
        _boundaries.Add(project, new(input, output, fields));
        return output;
    }

    private ByteBufferFlowBindings AnalyzeApply(
        PhysicalNestedLoopApplyNode apply,
        FlowScope scope,
        ByteBufferFlowBindings correlated
    )
    {
        var left = Analyze(apply.Left, scope, correlated);
        var right = Analyze(apply.Right, scope, left);
        return ByteBufferFlowBindings.Merge([left, right]);
    }

    private ByteBufferFlowBindings AnalyzeConservative(
        PhysicalNode node,
        FlowScope scope,
        ByteBufferFlowBindings correlated
    )
    {
        foreach (var child in node.Children)
            Analyze(child, scope, correlated).RequireComplete();
        correlated.RequireComplete();
        return new ByteBufferFlowBindings();
    }

    private void Observe(IrExpression expression, ByteBufferFlowBindings input)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expression is CteTableRef or CteCollectionInput or SubqueryExpression)
        {
            _unresolved = true;
            return;
        }
        if (expression is ColumnRef { ReturnType: var type } column && type == typeof(byte[]))
        {
            Require(column, input, ByteBufferRequirement.Complete);
            return;
        }
        if (expression is MethodCall call)
        {
            if (call.BufferPlan?.Program is { } program && ResolveInput(program, input) is { } source)
            {
                source.Require(ByteBufferDemandPreimage.For(program, program.Requirement));
                foreach (var operand in program.Inputs.Skip(1))
                    Observe(operand, input);
                return;
            }
            if (
                call.BufferSemantics
                    is { Kind: ByteBufferCallableKind.Length or ByteBufferCallableKind.FixedWidthRead } consumer
                && call.Arguments[0] is ColumnRef buffer
            )
            {
                Require(
                    buffer,
                    input,
                    consumer.Kind == ByteBufferCallableKind.Length
                        ? ByteBufferRequirement.Length
                        : ByteBufferRequirement.Prefix(consumer.ReadWidth)
                );
                foreach (var argument in call.Arguments.Skip(1))
                    Observe(argument, input);
                return;
            }
            if (call.BufferSemantics is null)
                input.RequireComplete();
        }
        foreach (var child in IrExpressionTraversal.Children(expression))
            Observe(child, input);
    }

    private static void Require(ColumnRef column, ByteBufferFlowBindings input, ByteBufferRequirement requirement)
    {
        if (input.Resolve(column) is { } value)
            value.Require(requirement);
        else
            input.RequireComplete();
    }

    private static ByteBufferFlowValue? ResolveInput(ByteBufferProgram program, ByteBufferFlowBindings input) =>
        program.SegmentCount == 0
        && !program.Steps.Any(step => step.Kind == ByteBufferStepKind.CheckedRange)
        && program.Inputs[0] is ColumnRef column
            ? input.Resolve(column)
            : null;

    private sealed record FlowBoundary(
        ByteBufferFlowBindings Input,
        ByteBufferFlowBindings Output,
        IReadOnlyList<ByteBufferFlowValue?> Fields
    );

    private sealed class FlowScope
    {
        internal Dictionary<string, ByteBufferFlowBindings> Named { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<IntermediateProducerId, ByteBufferFlowBindings> Intermediate { get; } = [];

        internal FlowScope Copy()
        {
            var copy = new FlowScope();
            foreach (var (name, binding) in Named)
                copy.Named.Add(name, binding);
            foreach (var (identity, binding) in Intermediate)
                copy.Intermediate.Add(identity, binding);
            return copy;
        }
    }
}
