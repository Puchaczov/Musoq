using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterExecutionTests
{
    [TestMethod]
    public void WhenCollectionScriptParameterIsUsedInIn_ShouldAcceptTypedList()
    {
        const string query =
            "param(keys: string[]) select Key, Value from #Parameterized.Items() where Key in $keys";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        vm.Parameters["keys"] = new List<string> { "KEY_2" };
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("KEY_2", table[0][0]);
        Assert.AreEqual("VALUE_2", table[0][1]);
        Assert.AreEqual(1, provider.OpenCount);
    }

    [TestMethod]
    public void WhenRequiredCollectionParameterIsMissing_ShouldFailBeforeOpeningSource()
    {
        const string query =
            "param(keys: string[]) select Key, Value from #Parameterized.Items() where Key in $keys";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "Required script parameter 'keys' was not provided.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenCollectionScriptParameterIsNull_ShouldFailBeforeOpeningSource()
    {
        const string query =
            "param(keys: string[]) select Key, Value from #Parameterized.Items() where Key in $keys";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["keys"] = null;

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed,
            "Script parameter 'keys' expected a non-null value of type 'IReadOnlyList<string>'.");
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void WhenCollectionScriptParameterHasWrongClrType_ShouldFailBeforeOpeningSource()
    {
        const string query =
            "param(keys: string[]) select Key, Value from #Parameterized.Items() where Key in $keys";
        var provider = new ParameterizedEnvironmentSchemaProvider();
        var vm = CompileWithProvider(query, provider);
        vm.Parameters["keys"] = new[] { 1 };

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run(TestContext.CancellationToken).Count);

        AssertRuntimeEnvelope(
            exception,
            DiagnosticCode.MQ7004_ScriptParameterTypeMismatch,
            "Script parameter 'keys' expected a value of type 'IReadOnlyList<string>' but received 'int[]'.");
        Assert.AreEqual(0, provider.OpenCount);
    }
}
