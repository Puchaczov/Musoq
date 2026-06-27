using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class ScriptVariableExecutionTests
{
    [TestMethod]
    [DataRow("decimal", "(10 + 5) * 2 - 4 / 2", "28")]
    [DataRow("long", "(6 & 3) | (8 >> 1)", "6")]
    [DataRow("bool", "true and false or true", "True")]
    [DataRow("bool", "5 > 3 and 3 <= 3", "True")]
    [DataRow("bool", "null is null", "True")]
    [DataRow("bool", "null is not distinct from null", "True")]
    [DataRow("bool", "null is distinct from 1", "True")]
    [DataRow("bool", "5 between 1 and 10", "True")]
    public void WhenScriptVariableInitializerUsesConstantOperators_ShouldEvaluateConsistently(
        string typeName,
        string expression,
        string expectedText)
    {
        var query = $"let result: {typeName} = {expression}; select $result from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(
            expectedText,
            Convert.ToString(table[0][0], CultureInfo.InvariantCulture));
    }
}
