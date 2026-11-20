using System.Collections.Generic;
using System.Linq;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateBoundedRecordList : ExecutionNode
{
    public ExecutionCreateBoundedRecordList(
        ExecutionVariable list,
        GeneratedRecordShape recordShape,
        IReadOnlyList<ExecutionOrderField> keys,
        ExecutionOrderRecordSelection selection
    )
    {
        List = list;
        RecordShape = recordShape;
        Keys = ExecutionIrCollections.Freeze(keys);
        Selection = selection;
    }

    public ExecutionVariable List { get; init; }
    public GeneratedRecordShape RecordShape { get; init; }
    public IReadOnlyList<ExecutionOrderField> Keys { get; init; }
    public ExecutionOrderRecordSelection Selection { get; init; }

    internal bool UsesSmallOrderedBuffer =>
        HasOnlyBuiltInOrderKeys
        && Selection switch
        {
            ExecutionTakeOrderRecordSelection { Count.Constant: >= 2 and <= 32 } => true,
            ExecutionSkipTakeOrderRecordSelection { SkipCount.Constant: int skip, TakeCount.Constant: int take } => skip
                >= 0
                && take > 0
                && (long)skip + take is >= 2 and <= 32,
            _ => false,
        };

    private bool HasOnlyBuiltInOrderKeys =>
        Keys.Count != 0
        && Keys.All(static key =>
            key.Type.Descriptor
                is { Kind: ExecutionPortableTypeKind.Primitive, StableName: not "primitive:void" }
                    or {
                        Kind: ExecutionPortableTypeKind.Nullable,
                        Arguments: [{ Kind: ExecutionPortableTypeKind.Primitive }]
                    }
        );

    internal bool UsesSingleRecordBuffer => Selection is ExecutionTakeOrderRecordSelection { Count.Constant: 1 };
}
