using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Physical;
public sealed partial class PhysicalPlanBuilder
{
    private readonly Dictionary<JoinNode, PredicateMovementPlan[]> _predicateMovementsByJoin;
    private readonly PhysicalStrategyPlan? _strategyPlan;

    public PhysicalPlanBuilder()
        : this(null, null)
    { }

    internal PhysicalPlanBuilder(IReadOnlyList<PredicateMovementPlan>? predicateMovementPlans, PhysicalStrategyPlan? strategyPlan)
    {
        _predicateMovementsByJoin = CreatePredicateMovementsByJoin(predicateMovementPlans);
        _strategyPlan = strategyPlan;
    }

    public PhysicalNode Lower(LogicalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_strategyPlan == null)
            throw new InvalidOperationException("Physical plan lowering requires an explicit PhysicalStrategyPlan. Run PhysicalStrategyPlanner before constructing PhysicalPlanBuilder.");

        return Lower(node, _strategyPlan);
    }

    private PhysicalNode Lower(LogicalNode node, PhysicalStrategyPlan strategyPlan)
    {
        return node switch
        {
            SchemaScanNode scan => new PhysicalSchemaScanNode(
                scan.SchemaName,
                scan.MethodName,
                scan.Arguments,
                scan.Alias,
                [],
                [],
                scan.OutputSchema,
                scan.SourceContextId),
            ValuesScanNode values => new PhysicalValuesScanNode(values.Alias, values.Rows, values.OutputSchema),
            UnpivotNode unpivot => LowerUnpivot(unpivot, strategyPlan),
            InterpretSourceNode interpret => new PhysicalInterpretSourceNode(
                interpret.SchemaName,
                interpret.Kind,
                interpret.Arguments,
                interpret.Alias,
                interpret.ResultType,
                interpret.ApplyKind,
                interpret.OutputSchema),
            CteRefNode cteRef => new PhysicalCteRefNode(cteRef.CteName, cteRef.Alias, cteRef.OutputSchema),
            PropertySourceNode property => new PhysicalPropertySourceNode(
                property.SourceAlias,
                property.PropertiesChain,
                property.Alias,
                property.ColumnIndex,
                property.ResultType,
                property.ApplyKind,
                property.OutputSchema),
            AccessMethodSourceNode accessMethod => new PhysicalAccessMethodSourceNode(
                accessMethod.SourceAlias,
                accessMethod.MethodCallExpression,
                accessMethod.Alias,
                accessMethod.ResultType,
                accessMethod.ApplyKind,
                accessMethod.OutputSchema),
            FilterNode filter => new PhysicalFilterNode(filter.Predicate, Lower(filter.Input, strategyPlan)),
            ProjectNode project => new PhysicalProjectNode(project.Fields, Lower(project.Input, strategyPlan)) { IsDistinct = project.IsDistinct },
            HavingFilterNode having => new PhysicalHavingFilterNode(having.Predicate, Lower(having.Input, strategyPlan)),
            QualifyFilterNode qualify => new PhysicalQualifyFilterNode(qualify.Predicate, Lower(qualify.Input, strategyPlan)),
            TakeNode take => LowerTake(take, strategyPlan),
            SortNode sort => new PhysicalSortNode(sort.Keys, Lower(sort.Input, strategyPlan)),
            SkipNode skip => new PhysicalSkipNode(skip.Count, Lower(skip.Input, strategyPlan)),
            WindowNode window => LowerWindow(window, strategyPlan),
            DescNode desc => new PhysicalDescNode(desc.SchemaName, desc.MethodName, desc.Type, desc.Column, desc.Arguments, desc.SourceContextId, desc.OutputSchema, desc.QueryOutputSchema),
            AggregateNode aggregate => LowerAggregate(aggregate, strategyPlan),
            JoinNode join => LowerJoin(join, strategyPlan),
            ApplyNode apply => new PhysicalNestedLoopApplyNode(apply.Kind, Lower(apply.Left, strategyPlan), Lower(apply.Right, strategyPlan), apply.WithOrdinality),
            SetOperationNode setOperation => LowerSetOperation(setOperation, strategyPlan),
            CteNode cte => LowerCte(cte, strategyPlan),
            MultiStatementNode multiStatement => LowerMultiStatement(multiStatement, strategyPlan),
            _ => throw UnsupportedShape.Of($"Logical node type '{node.GetType().Name}'", "the physical plan builder")
        };
    }
}
