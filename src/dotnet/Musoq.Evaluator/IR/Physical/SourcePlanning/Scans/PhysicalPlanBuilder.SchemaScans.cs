using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Physical;

public sealed partial class PhysicalPlanBuilder
{
    private PhysicalSchemaScanNode LowerSchemaScan(SchemaScanNode scan) =>
        new(
            scan.SchemaName,
            scan.MethodName,
            scan.Arguments,
            scan.Alias,
            [],
            [],
            ResolveScanOutput(scan),
            scan.SourceContextId,
            ResolveSourceTransferStrategy(scan.SourceContextId),
            scan.SourceConstructionType,
            scan.SourceConstructionSupportsContext,
            scan.SourceConstructorStableId,
            scan.SourceConstructor,
            scan.StructuralArgumentLimits
        )
        {
            BoundTable = scan.BoundTable,
        };
}
