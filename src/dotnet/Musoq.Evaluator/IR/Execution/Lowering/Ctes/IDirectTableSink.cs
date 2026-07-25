using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal interface IDirectTableSink
{
    ExecutionNode CreateAppend(ExecutionAppendRow append);

    TableBuildResult Complete(
        IReadOnlyList<RowShape> shapes,
        IReadOnlyList<ExecutionNode> nodes,
        GeneratedRowShape workingShape,
        IReadOnlyList<PostOperation> postOperations,
        bool isDistinct,
        TableProjection? finalProjection = null);
}
