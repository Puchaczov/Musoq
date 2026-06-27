using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Planning;
using PhysicalToExecutionPlanBuilder = Musoq.Evaluator.IR.Execution.PhysicalToExecutionPlanBuilder;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    [TestMethod]
    public void Constructor_WhenLoweringLacksPlannerExecutionStrategies_ShouldFailClearly()
    {
        var shapeResolver = new ExecutionShapeResolver(
            entityTypesByAlias: new Dictionary<string, Type>
            {
                ["p"] = typeof(Person)
            });

        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => new PhysicalToExecutionPlanBuilder(
            shapeResolver,
            null,
            new CompilationOptions(),
            null,
            executionArtifacts: null!));

        Assert.AreEqual("executionArtifacts", exception.ParamName);
    }
}
