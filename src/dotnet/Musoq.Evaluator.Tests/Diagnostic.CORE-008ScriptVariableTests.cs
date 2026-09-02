using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCore008ScriptVariableTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    public void ScriptVariableNames_ShouldRemainCaseSensitive()
    {
        const string query =
            "let key: string = 'lower'; " +
            "let Key: string = 'upper'; " +
            "select $key, $Key from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());

        Assert.IsEmpty(vm.ParameterDefinitions);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("lower", table[0][0]);
        Assert.AreEqual("upper", table[0][1]);
    }

    [TestMethod]
    public void ScriptVariable_ShouldNotBeOverridableThroughRuntimeParameterDictionary()
    {
        const string query = "let key: string = 'constant'; select $key from #EnvironmentVariables.All()";
        var vm = CreateAndRunVirtualMachine(query, CreateEnvironmentVariableSources());
        vm.Parameters["key"] = "override";

        var exception = Assert.Throws<QueryExecutionException>(
            () => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeError(exception, DiagnosticCode.MQ7006_UnknownScriptParameter);
        StringAssert.Contains(
            exception.Envelope!.Message,
            "Script parameter 'key' was provided but is not declared.");
    }

    [TestMethod]
    [DataRow(
        "let key: string = ToUpper('key_1'); select 1 from #EnvironmentVariables.All()",
        DisplayName = "function call")]
    [DataRow(
        "let key: string = Key; select 1 from #EnvironmentVariables.All()",
        DisplayName = "data-source column")]
    [DataRow(
        "let key: string = (select Key from #EnvironmentVariables.All()); select 1 from #EnvironmentVariables.All()",
        DisplayName = "scalar subquery")]
    public void RuntimeDependentInitializer_ShouldReportSpecificBindDiagnostic(string query)
    {
        var exception = CompileFails(query);

        AssertSingleError(
            exception,
            DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
            DiagnosticPhase.Bind,
            "only literals");
        AssertHasGuidance(exception);
    }

    private MusoqQueryException CompileFails(string query)
    {
        return Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                new EnvironmentVariablesSchemaProvider(),
                LoggerResolver));
    }

    private static System.Collections.Generic.Dictionary<
        string,
        System.Collections.Generic.IEnumerable<EnvironmentVariableEntity>> CreateEnvironmentVariableSources()
    {
        return new System.Collections.Generic.Dictionary<
            string,
            System.Collections.Generic.IEnumerable<EnvironmentVariableEntity>>
        {
            {
                "*",
                [
                    new EnvironmentVariableEntity("KEY_1", "VALUE_1"),
                    new EnvironmentVariableEntity("KEY_2", "VALUE_2")
                ]
            }
        };
    }
}
