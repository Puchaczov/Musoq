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
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["country"] = "PL",
            ["limit"] = 10
        };

        var state = ExecutionState.Capture(parameters);
        parameters["country"] = "DE";
        parameters.Remove("limit");

        Assert.AreEqual("PL", state.Parameters["country"]);
        Assert.AreEqual(10, state.Parameters["limit"]);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, object?>)state.Parameters)["country"] = "FR");
    }

    [TestMethod]
    public void Capture_WhenParametersAreEmpty_ShouldReuseEmptyState()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        var state = ExecutionState.Capture(parameters);

        Assert.AreSame(ExecutionState.Empty, state);
        Assert.IsEmpty(state.Parameters);
    }

    [TestMethod]
    public void Capture_WhenParametersAreNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => ExecutionState.Capture(null!));
    }
}
