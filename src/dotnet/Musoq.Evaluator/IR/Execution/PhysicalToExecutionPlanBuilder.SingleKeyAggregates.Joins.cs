using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private SingleKeyAggregateExecutionSourceBuildResult BuildSingleKeyAggregateHashJoinSource(
        PhysicalHashJoinNode join,
        string resultTableName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex)
    {
        if (join.BuildKeys.Length != join.ProbeKeys.Length || join.BuildKeys.Length == 0)
        {
            return SingleKeyAggregateExecutionSourceBuildResult.Unsupported(
                "Execution IR hash-join aggregate fusion requires matching equality key counts.");
        }

        var sources = BuildJoinSources(
            join.Left,
            join.Right,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName));
        if (!sources.Supported)
            return SingleKeyAggregateExecutionSourceBuildResult.Unsupported(sources.UnsupportedReason);

        var joinSources = sources.Source;
        if (!TryResolveHashJoinSides(join, joinSources, out var hashSides))
        {
            return SingleKeyAggregateExecutionSourceBuildResult.Unsupported(
                "Execution IR hash-join aggregate fusion cannot map build/probe keys to flat join inputs.");
        }

        if (HasDynamicHashJoinInput(joinSources))
        {
            return SingleKeyAggregateExecutionSourceBuildResult.Unsupported(
                "Execution IR hash-join aggregate fusion does not stream dynamic hash-join inputs. Physical planning must select nested-loop before Execution IR lowering.");
        }

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var keyType = ResolveHashJoinKeyType(join);
        var hash = new ExecutionVariable(CreateScopedHashName(resultTableName, $"{hashSides.Build.Variable.Name}Hash"), typeof(object));
        var matches = new ExecutionVariable($"{hash.Name}Matches", typeof(object));
        var buildLoop = CreateSourceLoop(
            hashSides.Build.Shape,
            hashSides.Build.Rows,
            hashSides.Build.Variable,
            new ExecutionBlock(
            [
                new ExecutionHashAdd(
                    hash,
                    CreateHashJoinKeyExpression(join.BuildKeys, sourceLookup, keyType),
                    hashSides.Build.Variable,
                    keyType,
                        hashSides.Build.Variable.Type,
                        hashSides.Build.Variable.GeneratedRowTypeName)
            ]));
        var setup = new List<ExecutionNode>(
            joinSources.Left.Setup.Count +
            joinSources.Right.Setup.Count +
            2);

        setup.AddRange(joinSources.Left.Setup);
        setup.AddRange(joinSources.Right.Setup);
        setup.Add(new ExecutionCreateHash(
            hash,
            keyType,
            hashSides.Build.Variable.Type,
            CreateHashCapacityCandidate(hash, hashSides.Build),
            hashSides.Build.Variable.GeneratedRowTypeName));
        setup.Add(buildLoop);

        return SingleKeyAggregateExecutionSourceBuildResult.Success(new SingleKeyAggregateExecutionSource(
            sourceLookup,
            [..joinSources.Left.Shapes, ..joinSources.Right.Shapes],
            setup,
            body =>
            {
                var matchedBody = CreateHashJoinAggregateMatchedBody(join.Residual, body, sourceLookup);
                var matchesLoop = new ExecutionForEach(
                    hashSides.Build.Variable,
                    new ExecutionVariableRead(matches),
                    matchedBody);
                var probe = new ExecutionHashProbe(
                    hash,
                    matches,
                    CreateHashJoinKeyExpression(join.ProbeKeys, sourceLookup, keyType),
                    keyType,
                    hashSides.Build.Variable.Type,
                    new ExecutionBlock([matchesLoop]),
                    GeneratedRowTypeName: hashSides.Build.Variable.GeneratedRowTypeName);

                return CreateSourceLoop(
                    hashSides.Probe.Shape,
                    hashSides.Probe.Rows,
                    hashSides.Probe.Variable,
                    new ExecutionBlock([probe]));
            }));
    }

    private static ExecutionBlock CreateHashJoinAggregateMatchedBody(
        IrExpression? joinCondition,
        ExecutionBlock body,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        if (joinCondition == null)
            return body;

        return new ExecutionBlock(
        [
            new ExecutionIf(
                ExecutionExpressionConverter.Convert(joinCondition, sourceLookup),
                body)
        ]);
    }

    private TableBuildResult BuildSingleKeyAggregateTableFromApplyChain(
        SingleKeyAggregatePipeline pipeline,
        ApplyChainSource chain,
        string resultTableName,
        string resultShapeName,
        bool scopeAggregateVariables)
    {
        if (chain.Sources.Any(static source => source.OrdinalityVariable != null))
        {
            return TableBuildResult.Unsupported(
                "Execution IR single-key aggregate apply-chain fusion does not support WITH ORDINALITY inputs.");
        }

        var source = new SingleKeyAggregateExecutionSource(
            chain.SourceLookup,
            chain.Shapes,
            chain.Sources[0].Setup,
            body => CreateCrossApplyAggregateChainLoop(chain.Sources, body));

        return BuildSingleKeyAggregateTableCore(
            pipeline,
            source,
            resultTableName,
            resultShapeName,
            scopeAggregateVariables);
    }

    private static ExecutionSourceLoop CreateCrossApplyAggregateChainLoop(
        IReadOnlyList<JoinSource> sources,
        ExecutionBlock body)
    {
        return CreateCrossApplyChainLoop(sources, 0, body) as ExecutionSourceLoop
               ?? throw new InvalidOperationException(
                   "Aggregate apply-chain fusion expected a source loop after rejecting ordinality inputs.");
    }
}
