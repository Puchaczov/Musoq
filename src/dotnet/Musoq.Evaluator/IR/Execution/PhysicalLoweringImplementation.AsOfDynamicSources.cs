using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private JoinSource PrepareAsOfProbeSource(JoinSource rightSource, string resultTableName)
    {
        if (rightSource.Shape is GeneratedRowShape ||
            rightSource.Shape is SourceEntityShape && rightSource.Variable.Type.ResolveClrType().IsValueType)
            return MaterializeAsOfStructSource(rightSource, resultTableName);

        return rightSource;
    }
}
