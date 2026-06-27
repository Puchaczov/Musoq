using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionRowStreamsTests
{
    [TestMethod]
    public void RebindLike_WhenPrototypeIsScalar_ShouldPreserveScalarStreamKind()
    {
        var prototype = new ExecutionScalarRowStream(Var("source"));
        var rebound = ExecutionRowStreams.RebindLike(prototype, Var("rebound"));

        Assert.IsInstanceOfType<ExecutionScalarRowStream>(rebound);
        Assert.IsTrue(ExecutionRowStreams.IsScalar(rebound));
        Assert.IsFalse(ExecutionRowStreams.IsChunked(rebound));
        Assert.AreEqual("rebound", ((ExecutionScalarRowStream)rebound).Variable.Name);
    }

    [TestMethod]
    public void RebindLike_WhenPrototypeIsChunked_ShouldPreserveChunkedStreamKind()
    {
        var prototype = new ExecutionRowStream(
            Var("source"),
            ExecutionRowStreamKind.Chunks,
            ExecutionRowStreamRowsAccess.TableRows);
        var rebound = ExecutionRowStreams.RebindLike(prototype, Var("rebound"));

        Assert.IsInstanceOfType<ExecutionRowStream>(rebound);
        Assert.IsTrue(ExecutionRowStreams.IsChunked(rebound));
        Assert.IsFalse(ExecutionRowStreams.IsScalar(rebound));
        var rowStream = (ExecutionRowStream)rebound;
        Assert.AreEqual("rebound", rowStream.Variable.Name);
        Assert.AreEqual(ExecutionRowStreamKind.Chunks, rowStream.Kind);
        Assert.AreEqual(ExecutionRowStreamRowsAccess.Direct, rowStream.RowsAccess);
    }

    private static ExecutionVariable Var(string name)
    {
        return new ExecutionVariable(name, typeof(object), "Row0");
    }
}
