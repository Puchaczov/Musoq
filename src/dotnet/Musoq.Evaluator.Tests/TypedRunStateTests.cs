using System;
using System.Collections.Generic;
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

        var state = new TypedRunState(definitions, parameters);

        Assert.AreSame(parameters, state.Parameters);
        Assert.HasCount(2, state.ParameterDefinitions);
        Assert.HasCount(1, state.RequiredParameters);
        Assert.AreEqual("name", state.RequiredParameters[0].Name);
    }

    [TestMethod]
    public void CreateOptions_ShouldSnapshotParameters()
    {
        var state = new TypedRunState();
        state.Parameters["name"] = "Ada";

        var options = state.CreateOptions(CancellationToken.None);
        state.Parameters["name"] = "Grace";

        Assert.IsNotNull(options.Parameters);
        Assert.AreEqual("Ada", options.Parameters["name"]);
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
