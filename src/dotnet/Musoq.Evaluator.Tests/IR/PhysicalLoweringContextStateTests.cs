using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalLoweringContextStateTests
{
    [TestMethod]
    public void SidecarPayloadState_ShouldExposeAnImmutableSnapshot()
    {
        var state = new CteSidecarHashPayloadState();
        var payload = new HashPayloadShape("PayloadRow", []);

        var updated = state.WithPayload(3, payload);

        Assert.IsFalse(state.TryGet(3, out _));
        Assert.IsTrue(updated.TryGet(3, out var resolved));
        Assert.AreSame(payload, resolved);

        var snapshot = updated.Snapshot;
        Assert.AreSame(payload, snapshot[3]);
        Assert.ThrowsExactly<System.NotSupportedException>(() =>
            ((IDictionary<int, HashPayloadShape>)snapshot).Add(4, payload));
    }

    [TestMethod]
    public void EmptyCteContext_ShouldOwnCteStateAndKeepSinksSeparate()
    {
        var context = CteLoweringContext.Empty;
        var otherContext = CteLoweringContext.Empty;

        var updatedContext = context with
        {
            SidecarHashPayloads = context.SidecarHashPayloads.WithPayload(
                7,
                new HashPayloadShape("PayloadRow", []))
        };

        Assert.IsNotNull(context.SidecarHashPayloads);
        Assert.IsNotNull(context.ScalarSubqueryEmptyResults);
        Assert.IsNotNull(context.RecursiveCte);
        Assert.IsNull(context.RecursiveCte.Sink);
        Assert.IsFalse(otherContext.SidecarHashPayloads.TryGet(7, out _));
        Assert.IsTrue(updatedContext.SidecarHashPayloads.TryGet(7, out _));
        Assert.IsNull(DirectTableLoweringContext.Empty.Sink);
    }
}
