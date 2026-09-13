using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static JoinSource PrepareAsOfProbeSource(JoinSource rightSource)
    {
        if (rightSource.Shape is not ExpandoAdapterShape expando)
            return rightSource;

        var buffer = new ExecutionVariable(
            CreateIdentifierCandidate($"{rightSource.Variable.Name}AsOfRows", 0),
            typeof(object),
            expando.TypeName);
        rightSource.Setup.Add(CreateMaterializeExpandoListNode(rightSource.Rows, buffer, expando, null));

        return rightSource with
        {
            Variable = new ExecutionVariable(rightSource.Variable.Name, typeof(object), expando.TypeName),
            Rows = new ExecutionVariableRead(buffer)
        };
    }
}
