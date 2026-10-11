using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TypedRunStateTests
{
    [TestMethod]
    public void Constructor_ShouldExposeParametersAndRequiredDefinitions()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = "Ada"
        };
        var definitions = new[]
        {
            new ScriptParameterDefinition("name", typeof(string), false, null),
            new ScriptParameterDefinition("limit", typeof(int), true, 10)
        };

        var state = new TypedRunState(definitions, global::Musoq.Tests.Common.NativeParameterTestInputs.Capture<global::Musoq.Evaluator.TypedParameterSnapshot>(parameters));

        Assert.AreEqual("Ada", state.TypedParameters.Capture().GetRequired<string>("name"));
        Assert.AreEqual(1, state.TypedParameters.Count);
        Assert.HasCount(2, state.ParameterDefinitions);
        Assert.HasCount(1, state.RequiredParameters);
        Assert.AreEqual("name", state.RequiredParameters[0].Name);
    }

    [TestMethod]
    public void CreateOptions_ShouldSnapshotParameters()
    {
        var state = new TypedRunState();
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(state.TypedParameters, state.ParameterDefinitions, "name", "Ada");

        var options = state.CreateOptions(CancellationToken.None);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(state.TypedParameters, state.ParameterDefinitions, "name", "Grace");

        Assert.AreEqual(1, options.TypedParameters.Count);
        Assert.AreEqual("Ada", options.TypedParameters.GetRequired<string>("name"));
    }

    [TestMethod]
    public void CreateOptions_ShouldSnapshotCallbacks()
    {
        var state = new TypedRunState();
        var firstPhaseCount = 0;
        var secondPhaseCount = 0;
        var firstProgressCount = 0;
        var secondProgressCount = 0;

        state.AddPhaseChanged((_, _) => firstPhaseCount++);
        state.AddDataSourceProgress((_, _) => firstProgressCount++);
        var options = state.CreateOptions(CancellationToken.None);
        state.AddPhaseChanged((_, _) => secondPhaseCount++);
        state.AddDataSourceProgress((_, _) => secondProgressCount++);

        options.PhaseChanged?.Invoke(this, new QueryPhaseEventArgs("typed", QueryPhase.Select));
        options.DataSourceProgress?.Invoke(this, new DataSourceEventArgs("typed", "source", DataSourcePhase.Begin));

        Assert.AreEqual(1, firstPhaseCount);
        Assert.AreEqual(0, secondPhaseCount);
        Assert.AreEqual(1, firstProgressCount);
        Assert.AreEqual(0, secondProgressCount);
    }
}
