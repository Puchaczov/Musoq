using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourceRuntimeSettingsSnapshotTests
{
    [TestMethod]
    public void EstablishedSnapshot_ShouldSurviveRetainedProviderAndResolverMutation()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new RetainedSettingsResolver();
        var compiled = InstanceCreator.CompileForExecution(
            "couple settings.items with settings MiXeD as Data; select Token from Data();",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        Assert.IsTrue(resolver.Profiles.All(profile => profile == "MiXeD"));
        var establishedBeforeExecution = provider.Schema.MetadataSettings
            .Concat(provider.Schema.PlanSettings)
            .Where(settings => settings.ContainsKey("TOKEN"))
            .ToArray();
        resolver.Values["TOKEN"] = "resolver-mutated";
        foreach (var settings in provider.Schema.RequirementSettings.OfType<IDictionary<string, string>>())
            settings["TOKEN"] = "provider-mutated";

        Assert.AreEqual("stable-token", compiled.Run()[0].ReadValue(0));
        var established = establishedBeforeExecution
            .Concat(provider.Schema.ExecutionSettings)
            .Where(settings => settings.ContainsKey("TOKEN"))
            .ToArray();
        Assert.IsNotEmpty(established);
        Assert.IsTrue(established.All(settings => settings["TOKEN"] == "stable-token"));
        foreach (var settings in established.OfType<IDictionary<string, string>>())
            Assert.ThrowsExactly<NotSupportedException>(() => settings["TOKEN"] = "attempted-mutation");
    }

    [TestMethod]
    public void RepeatedAliasOccurrences_ShouldKeepSeparateSnapshotsWhenResolverReusesItsDictionary()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new RetainedSettingsResolver(useContext: true);
        var compiled = InstanceCreator.CompileForExecution(
            "couple settings.items with settings MiXeD as Data; " +
            "select l.Token, r.Token from Data() l cross join Data() r;",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver));
        resolver.Values.Clear();

        var table = compiled.Run();
        Assert.AreEqual("left-token", table[0].ReadValue(0));
        Assert.AreEqual("right-token", table[0].ReadValue(1));
        Assert.HasCount(2, resolver.Contexts.Distinct(StringComparer.Ordinal));
    }

    [TestMethod]
    public void SettingKeys_ShouldUseOrdinalComparisonAndTreatEmptyStringsAsProvided()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new RetainedSettingsResolver();
        resolver.Values["TOKEN"] = string.Empty;
        resolver.Values["token"] = "distinct-key";
        var compiled = InstanceCreator.CompileForExecution(
            "select Token from settings.items()", Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        Assert.AreEqual(string.Empty, compiled.Run()[0].ReadValue(0));
        var settings = provider.Schema.ExecutionSettings.Single();
        Assert.AreEqual(string.Empty, settings["TOKEN"]);
        Assert.AreEqual("distinct-key", settings["token"]);
    }

    [TestMethod]
    public void WrongCaseKey_ShouldNotSatisfyARequiredSettingOrOpenTheSource()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new RetainedSettingsResolver();
        resolver.Values.Clear();
        resolver.Values["token"] = "wrong-case";
        var result = InstanceCreator.CompileWithDiagnostics(
            "select Token from settings.items()", Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Errors.Any(error => error.Code == DiagnosticCode.MQ3067_MissingSourceRuntimeSetting));
        Assert.IsEmpty(provider.Schema.ExecutionSettings);
    }

    [TestMethod]
    public void HostProfileRejection_ShouldPreserveSpellingAndPreventSourceOpening()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new RejectingResolver();
        var exception = Assert.ThrowsExactly<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                "couple settings.items with settings MiXeD as Data; select Token from Data();",
                Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(),
                new CompilationOptions(sourceRuntimeSettingsResolver: resolver)));

        Assert.IsNotEmpty(exception.Envelopes);
        Assert.AreEqual("MiXeD", resolver.Profile);
        Assert.IsEmpty(provider.Schema.ExecutionSettings);
    }

    private sealed class RetainedSettingsResolver(bool useContext = false) : ISourceRuntimeSettingsResolver
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal)
        {
            ["TOKEN"] = "stable-token"
        };

        public List<string?> Profiles { get; } = [];
        public List<string> Contexts { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Profiles.Add(request.ProfileName);
            Contexts.Add(request.Identity.SourceContextId);
            if (useContext)
                Values["TOKEN"] = request.Identity.Alias == "l" ? "left-token" : "right-token";
            return Values;
        }
    }

    private sealed class RejectingResolver : ISourceRuntimeSettingsResolver
    {
        public InvalidOperationException Failure { get; } = new("Unknown profile");
        public string? Profile { get; private set; }

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Profile = request.ProfileName;
            throw Failure;
        }
    }
}
