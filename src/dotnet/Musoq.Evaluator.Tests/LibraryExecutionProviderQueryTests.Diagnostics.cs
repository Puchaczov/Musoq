using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Schema.Optimization;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    [TestMethod]
    [DataRow("profile select Id from #typedrows.rows()", "Musoq query profile")]
    [DataRow("explain analyze select Id from #typedrows.rows()", "Musoq explain analyze")]
    [DataRow("param(value: int) profile select $value from #typedrows.rows()", "Musoq query profile")]
    public void DiagnosticSql_WhenFullyConsumed_ShouldExposeConcreteColumns(string query, string heading)
    {
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new LibraryExecutionQueryRowProvider(), LoggerResolver);
        if (query.StartsWith("param", StringComparison.Ordinal))
            global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "value", 42);
        var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("LineNumber", typeof(int)), ("Text", typeof(string)));
        Assert.IsGreaterThan(0, rows.Length);
        CollectionAssert.AreEqual(Enumerable.Range(1, rows.Length).ToArray(),
            rows.Select(row => row.GetValue<int>(0)).ToArray());
        var text = string.Join("\n", rows.Select(row => row.GetValue<string>(1)));
        Assert.Contains(heading, text);
        foreach (var row in rows)
        {
            var visitor = new DiagnosticColumnTypes();
            row.VisitColumns(ref visitor);
            Assert.AreEqual(2, visitor.Count);
        }
    }

    [TestMethod]
    public void DescSettings_WhenFullyConsumed_ShouldVisitConcreteValuesAndKeepSecretsRedacted()
    {
        using var compiled = InstanceCreator.CompileForExecution("desc settings #settings.items()",
            Guid.NewGuid().ToString(), new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(true),
            LoggerResolver, new CompilationOptions(sourceRuntimeSettingsResolver: new SecretSettingsResolver()));
        var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Required", typeof(bool)),
            ("Secret", typeof(bool)), ("Phases", typeof(string)), ("Status", typeof(string)), ("Description", typeof(string)));
        Assert.HasCount(2, rows);
        var token = rows.Single(row => row.GetValue<string>(0) == "TOKEN");
        Assert.IsTrue(token.GetValue<bool>(1));
        Assert.IsTrue(token.GetValue<bool>(2));
        Assert.AreEqual("All", token.GetValue<string>(3));
        Assert.AreEqual("Provided", token.GetValue<string>(4));
        var optional = rows.Single(row => row.GetValue<string>(0) == "OPTIONAL_TOKEN");
        Assert.IsFalse(optional.GetValue<bool>(1));
        Assert.IsFalse(optional.GetValue<bool>(2));
        Assert.AreEqual("Default", optional.GetValue<string>(4));
        foreach (var row in rows)
        {
            var visitor = new SettingColumnTypes();
            row.VisitColumns(ref visitor);
            Assert.AreEqual(6, visitor.Count);
            Assert.IsFalse(visitor.ExposedSecret);
        }
    }

    private sealed class SecretSettingsResolver : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request) =>
            new Dictionary<string, string> { ["TOKEN"] = "wave8-secret-token" };
    }

    private struct DiagnosticColumnTypes : IColumnValueVisitor
    {
        public int Count;
        public void Visit<T>(int index, in T value)
        {
            var expectedType = index == 0 ? typeof(int) : typeof(string);
            var actualType = typeof(T);
            Assert.AreEqual(expectedType, actualType);
            Count++;
        }
    }

    private struct SettingColumnTypes : IColumnValueVisitor
    {
        public int Count;
        public bool ExposedSecret;
        public void Visit<T>(int index, in T value)
        {
            var expectedType = index is 1 or 2 ? typeof(bool) : typeof(string);
            var actualType = typeof(T);
            Assert.AreEqual(expectedType, actualType);
            Count++;
            if (typeof(T) == typeof(string))
            {
                var copy = value;
                var text = global::System.Runtime.CompilerServices.Unsafe.As<T, string>(ref copy);
                ExposedSecret |= text.Contains("wave8-secret-token", StringComparison.Ordinal);
            }
        }
    }
}
