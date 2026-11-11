namespace Musoq.Evaluator.IR.Execution;

internal static class ByteBufferRuntimeRequirements
{
    internal static bool RequiresSnapshotService(ExecutionExpression expression) =>
        expression switch
        {
            ExecutionByteBufferSnapshot { Program: { } program } => program.RequiresSnapshotService,
            ExecutionByteBufferSnapshot => true,
            ExecutionByteBufferOperation { Program: { } program } => program.RequiresSnapshotService,
            _ => false,
        };
}
