using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static void BindSharedWindowValues(List<WindowComputationBuildResult> computations)
    {
        if (computations.Count < 2)
            return;

        var captures =
            new Dictionary<(int Group, ExecutionFieldRead Field, ExecutionTypeRef ArrayType), ExecutionVariable>();
        for (var index = 0; index < computations.Count; index++)
        {
            var computation = computations[index];
            if (computation.Registration?.ValueCaptureGroup is not int group)
                continue;

            switch (computation.Node)
            {
                case ExecutionWindowAggregateKernel
                {
                    Value: ExecutionFieldRead { Stability: ColumnStability.Stable } field,
                    Descriptor: { CaptureArguments: true, LibraryKernel.ArgumentTypes: [var element, ..] },
                    FilterPredicate: null
                } kernel:
                    var storage = new ExecutionVariable(
                        kernel.CapturedValuesName,
                        element.ResolveClrType().MakeArrayType()
                    );
                    if (FindEarlierCapture(group, field, storage) is { } earlier)
                        computations[index] = computation with { Node = kernel with { SharedValues = earlier } };
                    break;
                case ExecutionComputePluginWindow
                {
                    Value: ExecutionFieldRead { Stability: ColumnStability.Stable } field,
                    ContextBuffers: { } buffers
                } plugin:
                    if (FindEarlierCapture(group, field, buffers.Values) is { } producer)
                        computations[index] = computation with
                        {
                            Node = plugin with { ContextBuffers = buffers with { SharedValues = producer } },
                        };
                    break;
            }
        }

        ExecutionVariable? FindEarlierCapture(int group, ExecutionFieldRead field, ExecutionVariable storage)
        {
            var key = (group, field, storage.Type);
            if (captures.TryGetValue(key, out var previous))
                return previous;
            captures.Add(key, storage);
            return null;
        }
    }
}
