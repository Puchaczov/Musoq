using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ExecutionStateTests
{
    [TestMethod]
    public void Capture_WhenParametersAreMutatedAfterCapture_ShouldKeepOriginalValues()
    {
        var parameters = new TypedParameterValues();
        parameters.Set("country", "PL");
        parameters.Set("limit", 10);
        var state = ExecutionState.Capture(parameters.Capture());
        parameters.Set("country", "DE");
        parameters.Set("limit", 20);
        Assert.AreEqual("PL", state.TypedParameters.GetRequired<string>("country"));
        Assert.AreEqual(10, state.TypedParameters.GetRequired<int>("limit"));
    }

    [TestMethod]
    public void Capture_WhenParametersAreEmpty_ShouldReuseEmptyState()
    {
        var parameters = new TypedParameterValues();

        var state = ExecutionState.Capture(parameters.Capture());

        Assert.AreSame(ExecutionState.Empty, state);
        Assert.AreEqual(0, state.TypedParameters.Count);
    }

    [TestMethod]
    public void Capture_WhenParametersAreNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => ExecutionState.Capture(null!));
    }
}
