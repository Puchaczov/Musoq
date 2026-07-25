using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.SetOperations;

internal sealed record SetOperationPipeline(
    PhysicalSetOperationNode SetOperation,
    IReadOnlyList<PostOperation> PostOperations);

internal sealed record StreamingUnionAllArm(
    RowShape SourceShape,
    IReadOnlyList<ExecutionNode> Setup,
    ExecutionSourceLoop Loop);

internal sealed record SetOperationArmNames(
    string LeftTableName,
    string LeftShapeName,
    string RightTableName,
    string RightShapeName);
