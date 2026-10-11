using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class ReboundTableColumnMetadata
{
    internal static ExecutionColumnMetadata? Create(ExecutionColumnMetadata? metadata, GeneratedRowShape shape) =>
        metadata is null ? null : new ExecutionColumnMetadata(metadata.ReferenceName,
            shape.Fields.Select(ExecutionColumnMetadataFields.FromFieldBinding).ToArray(), metadata.Kind);
}
