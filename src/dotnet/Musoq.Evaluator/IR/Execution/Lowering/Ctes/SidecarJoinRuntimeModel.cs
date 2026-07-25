using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal abstract record SidecarJoinRuntimeOperation(
    IReadOnlySet<string> RequiredAliases,
    int Ordinal);

internal sealed record SidecarJoinRuntimeStep(
    PhysicalHashJoinNode Join,
    CteSidecarIndexSpec Sidecar,
    JoinSource Build,
    ExecutionVariable Index,
    ExecutionVariable? Matches,
    IrExpression[] ProbeKeys,
    IrExpression? Residual,
    PhysicalFilterNode? Filter,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    IReadOnlySet<string> RequiredAliases,
    IReadOnlySet<string> IntroducedAliases,
    int Ordinal) : SidecarJoinRuntimeOperation(RequiredAliases, Ordinal);

internal sealed record SidecarJoinRuntimeGuard(
    IrExpression Predicate,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    IReadOnlySet<string> RequiredAliases,
    int Ordinal) : SidecarJoinRuntimeOperation(RequiredAliases, Ordinal);
