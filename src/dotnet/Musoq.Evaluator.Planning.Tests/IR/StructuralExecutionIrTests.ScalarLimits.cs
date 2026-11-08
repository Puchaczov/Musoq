using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class StructuralExecutionIrTests
{
    [TestMethod]
    public void ScalarReceiverLimits_ShouldBeBoundDuringExecutionPreparation()
    {
        var read = new ExecutionScriptParameterRead("value", typeof(string));
        var limits = new ExecutionStructuralLimitPlan(32, 100, 4);

        var prepared = StructuralSourcePreparation.PrepareStructuralSourceArguments([read], [limits], "d");
        var guard = (ExecutionStructuralConversion)prepared.Arguments[0];

        Assert.AreSame(read, guard.Input);
        Assert.AreEqual(ExecutionStructuralInputOrigin.Parameter, guard.LimitBinding!.Origin);
        Assert.AreEqual("$value", guard.LimitBinding.Path);
        Assert.AreEqual(4L, guard.LimitBinding.Limits.MaxStringBytes);
    }

    [TestMethod]
    public void ScalarWithoutReceiverContractLimits_ShouldRemainUnwrapped()
    {
        var read = new ExecutionScriptParameterRead("value", typeof(string));

        var prepared = StructuralSourcePreparation.PrepareStructuralSourceArguments([read], null, "d");

        Assert.AreSame(read, prepared.Arguments[0]);
    }
}
