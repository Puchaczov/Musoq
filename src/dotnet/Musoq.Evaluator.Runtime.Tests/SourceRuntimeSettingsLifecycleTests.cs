using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using static Musoq.Evaluator.Tests.SourceRuntimeSettingsLifecycleTestsFixtures;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourceRuntimeSettingsLifecycleTests
{
    [TestMethod]
    public void CompileForExecution_WhenSameSourceAppearsTwice_UsesSourceContextSettings()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: true);
        var resolver = new ContextTokenResolver(contextId =>
            contextId.StartsWith("l:", StringComparison.Ordinal) ? "left-token" : "right-token"
        );
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            "select l.Token, r.Token from #settings.items() l inner join #settings.items() r on 1 = 1",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("left-token", table[0].ReadValue(0));
        Assert.AreEqual("right-token", table[0].ReadValue(1));
        Assert.IsTrue(provider.Schema.MetadataSettings.Any(settings => settings["TOKEN"] == "left-token"));
        Assert.IsTrue(provider.Schema.MetadataSettings.Any(settings => settings["TOKEN"] == "right-token"));
        Assert.IsTrue(provider.Schema.PlanSettings.Any(settings => settings["TOKEN"] == "left-token"));
        Assert.IsTrue(provider.Schema.PlanSettings.Any(settings => settings["TOKEN"] == "right-token"));
        Assert.IsTrue(provider.Schema.ExecutionSettings.Any(settings => settings["TOKEN"] == "left-token"));
        Assert.IsTrue(provider.Schema.ExecutionSettings.Any(settings => settings["TOKEN"] == "right-token"));
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenRequiredSettingIsMissing_ReportsRedactedDiagnostic()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select Token from #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions()
        );

        var diagnostic = result.Diagnostics.Single(item =>
            item.Code == DiagnosticCode.MQ3067_MissingSourceRuntimeSetting
        );

        Assert.IsFalse(result.Succeeded);
        Assert.Contains("TOKEN", diagnostic.Message);
        Assert.DoesNotContain("left-token", diagnostic.Message);
        Assert.DoesNotContain("right-token", diagnostic.Message);
    }

    [TestMethod]
    public void CompileForExecution_WhenResolverIsNonDefault_DoesNotReuseCachedSettings()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: false);
        var resolver = new MutableTokenResolver("first-token");
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);
        const string query = "select Token from #settings.items()";

        var first = InstanceCreator.CompileForExecution(
            query,
            "RuntimeSettingsCacheProbe",
            provider,
            new TestsLoggerResolver(),
            options
        );
        resolver.Token = "second-token";
        var second = InstanceCreator.CompileForExecution(
            query,
            "RuntimeSettingsCacheProbe",
            provider,
            new TestsLoggerResolver(),
            options
        );

        Assert.AreEqual("first-token", first.Run()[0].ReadValue(0));
        Assert.AreEqual("second-token", second.Run()[0].ReadValue(0));
        Assert.IsGreaterThanOrEqualTo(2, resolver.ResolveCount);
    }

    [TestMethod]
    public void CompileForExecution_WhenSourceDeclaresSettings_DoesNotReuseCachedCompilation()
    {
        var provider = new SettingsSchemaProvider(SettingsDeclarationMode.OptionalOnly);
        const string query = "select Token from #settings.items()";

        InstanceCreator
            .CompileForExecution(query, "DeclaredRuntimeSettingsCacheProbe", provider, new TestsLoggerResolver())
            .Run();
        InstanceCreator
            .CompileForExecution(query, "DeclaredRuntimeSettingsCacheProbe", provider, new TestsLoggerResolver())
            .Run();

        Assert.IsGreaterThanOrEqualTo(2, provider.Schema.DescribeRuntimeSettingsCount);
    }

    [TestMethod]
    public void CompileForExecution_WhenCachedQueryLaterDeclaresSettings_BypassesStaleCachedCompilation()
    {
        var provider = new SettingsSchemaProvider(SettingsDeclarationMode.None);
        const string query = "select Token from #settings.items()";

        InstanceCreator
            .CompileForExecution(query, "RuntimeSettingsStaleCacheProbe", provider, new TestsLoggerResolver())
            .Run();
        provider.Schema.DeclarationMode = SettingsDeclarationMode.OptionalOnly;
        InstanceCreator
            .CompileForExecution(query, "RuntimeSettingsStaleCacheProbe", provider, new TestsLoggerResolver())
            .Run();

        Assert.IsGreaterThanOrEqualTo(3, provider.Schema.DescribeRuntimeSettingsCount);
    }

    [TestMethod]
    public void CreateForAnalyze_WhenSettingsAreDeclared_StoresResolvedDescriptionSnapshot()
    {
        var items = InstanceCreator.CreateForAnalyze(
            "select Token from #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(SettingsDeclarationMode.OptionalOnly),
            new TestsLoggerResolver()
        );

        var descriptions = items.SourceRuntimeSettingDescriptionsBySourceContextId.Values.Single();
        var description = descriptions.Single();

        Assert.AreEqual("OPTIONAL_TOKEN", description.Name);
        Assert.AreEqual(SourceRuntimeSettingResolutionStatus.Default, description.Status);
        Assert.IsFalse(items.HasSourceRuntimeSettingValues);
    }

    [TestMethod]
    public void CompileForExecution_WhenInitialHostSettingsChange_UsesLatestInitialSettings()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: false);
        var initialSettings = new Dictionary<string, string>(StringComparer.Ordinal) { ["TOKEN"] = "first-token" };
        const string query = "select Token from #settings.items()";

        var first = CompileWithInitialSettings(query, provider, initialSettings);
        initialSettings["TOKEN"] = "second-token";
        var second = CompileWithInitialSettings(query, provider, initialSettings);

        Assert.AreEqual("first-token", first.Run()[0].ReadValue(0));
        Assert.AreEqual("second-token", second.Run()[0].ReadValue(0));
    }

    [TestMethod]
    public void CoupleWithSettingsOnly_WhenProfileIsSelected_PassesProfileToResolver()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: true);
        var resolver = new ProfileTokenResolver();
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            "couple #settings.items with settings blue as Source; select Token from Source();",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Token", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["blue-token"]);
        Assert.AreEqual("blue", resolver.ProfileNames.Single());
    }

    [TestMethod]
    public void CoupleWithTableAndSettings_WhenOptionsAreReversed_PassesProfileToResolver()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: true);
        var resolver = new ProfileTokenResolver();
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            "table T { Token: string }; couple #settings.items with settings red and table T as Source; select Token from Source();",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Token", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["red-token"]);
        Assert.AreEqual("red", resolver.ProfileNames.Single());
    }

    [TestMethod]
    public void DescSettings_WhenRequiredSettingIsMissing_ShowsRedactedStatus()
    {
        var compiled = InstanceCreator.CompileForExecution(
            "desc settings #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver()
        );

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Default", "Optional token override."],
            ["TOKEN", true, true, "All", "Missing", "Token used by the settings source."]
        );
        Assert.IsFalse(table.Rows.SelectMany(row => row.ReadValues()).Any(value => Equals(value, "blue-token")));
    }

    [TestMethod]
    public void DescSettings_WhenOptionalSettingIsProvided_ShowsProvidedStatus()
    {
        var resolver = new StaticSettingsResolver(
            new Dictionary<string, string> { ["TOKEN"] = "token-value", ["OPTIONAL_TOKEN"] = "optional-value" }
        );
        var compiled = InstanceCreator.CompileForExecution(
            "desc settings #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver)
        );

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Provided", "Optional token override."],
            ["TOKEN", true, true, "All", "Provided", "Token used by the settings source."]
        );
        Assert.IsFalse(table.Rows.SelectMany(row => row.ReadValues()).Any(value => Equals(value, "optional-value")));
        Assert.IsFalse(table.Rows.SelectMany(row => row.ReadValues()).Any(value => Equals(value, "token-value")));
    }

    [TestMethod]
    public void CompileForExecution_WhenResolverReturnsExtraKeys_FlowsExtrasButDescListsDeclaredRequirementsOnly()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: true);
        var resolver = new StaticSettingsResolver(
            new Dictionary<string, string> { ["TOKEN"] = "token-value", ["EXTRA_TOKEN"] = "extra-value" }
        );
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            "select Token from #settings.items()",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );
        var desc = InstanceCreator.CompileForExecution(
            "desc settings #settings.items()",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );

        Assert.AreEqual("token-value", compiled.Run()[0].ReadValue(0));
        Assert.IsTrue(provider.Schema.MetadataSettings.Any(settings => settings.ContainsKey("EXTRA_TOKEN")));
        Assert.IsTrue(provider.Schema.PlanSettings.Any(settings => settings.ContainsKey("EXTRA_TOKEN")));
        Assert.IsTrue(provider.Schema.ExecutionSettings.Any(settings => settings.ContainsKey("EXTRA_TOKEN")));
        Assert.IsFalse(desc.Run().Rows.Any(row => Equals(row.GetValue<string>(0), "EXTRA_TOKEN")));
    }

    [TestMethod]
    public void CompileForExecution_WhenRequiredSettingIsEmptyString_TreatsItAsProvided()
    {
        var resolver = new StaticSettingsResolver(new Dictionary<string, string> { ["TOKEN"] = string.Empty });

        var compiled = InstanceCreator.CompileForExecution(
            "select Token from #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver)
        );

        Assert.AreEqual(string.Empty, compiled.Run()[0].ReadValue(0));
    }

    [TestMethod]
    public void DescSettings_WhenSecretSettingIsProvided_DoesNotExposeValue()
    {
        var resolver = new StaticSettingsResolver(new Dictionary<string, string> { ["TOKEN"] = "super-secret-token" });
        var compiled = InstanceCreator.CompileForExecution(
            "desc settings #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver)
        );

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Default", "Optional token override."],
            ["TOKEN", true, true, "All", "Provided", "Token used by the settings source."]
        );
        Assert.IsFalse(
            table.Rows.SelectMany(row => row.ReadValues()).Any(value => Equals(value, "super-secret-token"))
        );
    }

    [TestMethod]
    public void DescSettings_WhenCoupledAliasSelectsProfile_ShowsProvidedStatus()
    {
        var resolver = new ProfileTokenResolver();
        var compiled = InstanceCreator.CompileForExecution(
            "couple #settings.items with settings blue as Source; desc settings Source;",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver)
        );

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Default", "Optional token override."],
            ["TOKEN", true, true, "All", "Provided", "Token used by the settings source."]
        );
        Assert.AreEqual("blue", resolver.ProfileNames.Single());
        Assert.IsFalse(table.Rows.SelectMany(row => row.ReadValues()).Any(value => Equals(value, "blue-token")));
    }
}
