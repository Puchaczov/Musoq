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

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourceRuntimeSettingsLifecycleTests
{
    [TestMethod]
    public void CompileForExecution_WhenSameSourceAppearsTwice_UsesSourceContextSettings()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: true);
        var resolver = new ContextTokenResolver(contextId =>
            contextId.StartsWith("l:", StringComparison.Ordinal) ? "left-token" : "right-token");
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            "select l.Token, r.Token from #settings.items() l inner join #settings.items() r on 1 = 1",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options);

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("left-token", table[0][0]);
        Assert.AreEqual("right-token", table[0][1]);
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
            new CompilationOptions());

        var diagnostic = result.Diagnostics.Single(item =>
            item.Code == DiagnosticCode.MQ3067_MissingSourceRuntimeSetting);

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
            options);
        resolver.Token = "second-token";
        var second = InstanceCreator.CompileForExecution(
            query,
            "RuntimeSettingsCacheProbe",
            provider,
            new TestsLoggerResolver(),
            options);

        Assert.AreEqual("first-token", first.Run()[0][0]);
        Assert.AreEqual("second-token", second.Run()[0][0]);
        Assert.IsGreaterThanOrEqualTo(2, resolver.ResolveCount);
    }

    [TestMethod]
    public void CompileForExecution_WhenSourceDeclaresSettings_DoesNotReuseCachedCompilation()
    {
        var provider = new SettingsSchemaProvider(SettingsDeclarationMode.OptionalOnly);
        const string query = "select Token from #settings.items()";

        InstanceCreator.CompileForExecution(
            query,
            "DeclaredRuntimeSettingsCacheProbe",
            provider,
            new TestsLoggerResolver()).Run();
        InstanceCreator.CompileForExecution(
            query,
            "DeclaredRuntimeSettingsCacheProbe",
            provider,
            new TestsLoggerResolver()).Run();

        Assert.IsGreaterThanOrEqualTo(2, provider.Schema.DescribeRuntimeSettingsCount);
    }

    [TestMethod]
    public void CompileForExecution_WhenCachedQueryLaterDeclaresSettings_BypassesStaleCachedCompilation()
    {
        var provider = new SettingsSchemaProvider(SettingsDeclarationMode.None);
        const string query = "select Token from #settings.items()";

        InstanceCreator.CompileForExecution(
            query,
            "RuntimeSettingsStaleCacheProbe",
            provider,
            new TestsLoggerResolver()).Run();
        provider.Schema.DeclarationMode = SettingsDeclarationMode.OptionalOnly;
        InstanceCreator.CompileForExecution(
            query,
            "RuntimeSettingsStaleCacheProbe",
            provider,
            new TestsLoggerResolver()).Run();

        Assert.IsGreaterThanOrEqualTo(3, provider.Schema.DescribeRuntimeSettingsCount);
    }

    [TestMethod]
    public void CreateForAnalyze_WhenSettingsAreDeclared_StoresResolvedDescriptionSnapshot()
    {
        var items = InstanceCreator.CreateForAnalyze(
            "select Token from #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(SettingsDeclarationMode.OptionalOnly),
            new TestsLoggerResolver());

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
        var initialSettings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TOKEN"] = "first-token"
        };
        const string query = "select Token from #settings.items()";

        var first = CompileWithInitialSettings(query, provider, initialSettings);
        initialSettings["TOKEN"] = "second-token";
        var second = CompileWithInitialSettings(query, provider, initialSettings);

        Assert.AreEqual("first-token", first.Run()[0][0]);
        Assert.AreEqual("second-token", second.Run()[0][0]);
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
            options);

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
            options);

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
            new TestsLoggerResolver());

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Default", "Optional token override."],
            ["TOKEN", true, true, "All", "Missing", "Token used by the settings source."]);
        Assert.IsFalse(table.Rows.SelectMany(row => row.Values).Any(value => Equals(value, "blue-token")));
    }

    [TestMethod]
    public void DescSettings_WhenOptionalSettingIsProvided_ShowsProvidedStatus()
    {
        var resolver = new StaticSettingsResolver(new Dictionary<string, string>
        {
            ["TOKEN"] = "token-value",
            ["OPTIONAL_TOKEN"] = "optional-value"
        });
        var compiled = InstanceCreator.CompileForExecution(
            "desc settings #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Provided", "Optional token override."],
            ["TOKEN", true, true, "All", "Provided", "Token used by the settings source."]);
        Assert.IsFalse(table.Rows.SelectMany(row => row.Values).Any(value => Equals(value, "optional-value")));
        Assert.IsFalse(table.Rows.SelectMany(row => row.Values).Any(value => Equals(value, "token-value")));
    }

    [TestMethod]
    public void CompileForExecution_WhenResolverReturnsExtraKeys_FlowsExtrasButDescListsDeclaredRequirementsOnly()
    {
        var provider = new SettingsSchemaProvider(declareRequirement: true);
        var resolver = new StaticSettingsResolver(new Dictionary<string, string>
        {
            ["TOKEN"] = "token-value",
            ["EXTRA_TOKEN"] = "extra-value"
        });
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            "select Token from #settings.items()",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options);
        var desc = InstanceCreator.CompileForExecution(
            "desc settings #settings.items()",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options);

        Assert.AreEqual("token-value", compiled.Run()[0][0]);
        Assert.IsTrue(provider.Schema.MetadataSettings.Any(settings => settings.ContainsKey("EXTRA_TOKEN")));
        Assert.IsTrue(provider.Schema.PlanSettings.Any(settings => settings.ContainsKey("EXTRA_TOKEN")));
        Assert.IsTrue(provider.Schema.ExecutionSettings.Any(settings => settings.ContainsKey("EXTRA_TOKEN")));
        Assert.IsFalse(desc.Run().Rows.Any(row => Equals(row["Name"], "EXTRA_TOKEN")));
    }

    [TestMethod]
    public void CompileForExecution_WhenRequiredSettingIsEmptyString_TreatsItAsProvided()
    {
        var resolver = new StaticSettingsResolver(new Dictionary<string, string>
        {
            ["TOKEN"] = string.Empty
        });

        var compiled = InstanceCreator.CompileForExecution(
            "select Token from #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        Assert.AreEqual(string.Empty, compiled.Run()[0][0]);
    }

    [TestMethod]
    public void DescSettings_WhenSecretSettingIsProvided_DoesNotExposeValue()
    {
        var resolver = new StaticSettingsResolver(new Dictionary<string, string>
        {
            ["TOKEN"] = "super-secret-token"
        });
        var compiled = InstanceCreator.CompileForExecution(
            "desc settings #settings.items()",
            Guid.NewGuid().ToString(),
            new SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Default", "Optional token override."],
            ["TOKEN", true, true, "All", "Provided", "Token used by the settings source."]);
        Assert.IsFalse(table.Rows.SelectMany(row => row.Values).Any(value => Equals(value, "super-secret-token")));
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
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        var table = compiled.Run();

        AssertDescSettingsRows(
            table,
            ["OPTIONAL_TOKEN", false, false, "All", "Default", "Optional token override."],
            ["TOKEN", true, true, "All", "Provided", "Token used by the settings source."]);
        Assert.AreEqual("blue", resolver.ProfileNames.Single());
        Assert.IsFalse(table.Rows.SelectMany(row => row.Values).Any(value => Equals(value, "blue-token")));
    }

    private static void AssertDescSettingsRows(Table table, params object?[][] rows)
    {
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Required", typeof(bool)),
            ("Secret", typeof(bool)),
            ("Phases", typeof(string)),
            ("Status", typeof(string)),
            ("Description", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, rows);
    }

    private static CompiledQuery CompileWithInitialSettings(
        string query,
        ISchemaProvider provider,
        IReadOnlyDictionary<string, string> initialSettings)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            () => new CreateTree(
                new TransformTree(
                    new TurnQueryIntoRunnableCode(null),
                    new TestsLoggerResolver())),
            items =>
            {
                items.CreateBuildMetadataAndInferTypesVisitor =
                    (schemaProvider, columns, compilationOptions, schemaRegistry, logger) =>
                        new InitialSourceRuntimeSettingsVisitor(
                            schemaProvider,
                            columns,
                            logger,
                            initialSettings,
                            compilationOptions,
                            schemaRegistry);
            });
    }

    public sealed class SettingsSchemaProvider : ISchemaProvider
    {
        public SettingsSchemaProvider(bool declareRequirement)
            : this(declareRequirement ? SettingsDeclarationMode.RequiredAndOptional : SettingsDeclarationMode.None)
        {
        }

        public SettingsSchemaProvider(SettingsDeclarationMode declarationMode)
        {
            Schema = new SettingsSchema(declarationMode);
        }

        public SettingsSchema Schema { get; }

        public ISchema GetSchema(string schema)
        {
            if (!string.Equals(schema, "settings", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(schema, "#settings", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(schema);
            }

            return Schema;
        }
    }

    public enum SettingsDeclarationMode
    {
        None,
        RequiredAndOptional,
        OptionalOnly
    }

    public sealed class SettingsSchema : SchemaBase
    {
        public SettingsSchema(SettingsDeclarationMode declarationMode)
            : base("settings", CreateLibrary())
        {
            DeclarationMode = declarationMode;
            AddTable<SettingsTable>("items");
            AddSource<SettingsSource>("items");
        }

        public SettingsDeclarationMode DeclarationMode { get; set; }

        public ConcurrentBag<IReadOnlyDictionary<string, string>> MetadataSettings { get; } = [];

        public ConcurrentBag<IReadOnlyDictionary<string, string>> PlanSettings { get; } = [];

        public ConcurrentBag<IReadOnlyDictionary<string, string>> ExecutionSettings { get; } = [];

        public int DescribeRuntimeSettingsCount { get; private set; }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            MetadataSettings.Add(metadataContext.SourceRuntimeSettings);
            return base.GetTableByName(name, metadataContext, parameters);
        }

        public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
            string name,
            SourceRuntimeSettingsDescribeContext context,
            params object?[] parameters)
        {
            DescribeRuntimeSettingsCount += 1;
            var requirements = base.DescribeSourceRuntimeSettings(name, context, parameters);
            return DeclarationMode switch
            {
                SettingsDeclarationMode.RequiredAndOptional => requirements,
                SettingsDeclarationMode.OptionalOnly => requirements
                    .Where(static requirement => requirement.Name == "OPTIONAL_TOKEN")
                    .ToArray(),
                _ => []
            };
        }

        public override SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters)
        {
            MetadataSettings.Add(context.MetadataContext.SourceRuntimeSettings);
            return base.DescribeSource(name, context, parameters);
        }

        public override SourcePlanResult TryPlanSource(
            string name,
            SourcePlanRequest request,
            params object?[] parameters)
        {
            PlanSettings.Add(request.SourceRuntimeSettings);
            return SourcePlanResult.RejectAll(request);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            ExecutionSettings.Add(executionContext.SourceRuntimeSettings);
            return EnsureSourceType<T, SettingsEntity>(
                name,
                new SettingsSource(executionContext));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class InitialSourceRuntimeSettingsVisitor(
        ISchemaProvider provider,
        IReadOnlyDictionary<string, string[]> columns,
        Microsoft.Extensions.Logging.ILogger<BuildMetadataAndInferTypesVisitor> logger,
        IReadOnlyDictionary<string, string> initialSettings,
        CompilationOptions compilationOptions,
        SchemaRegistry? schemaRegistry)
        : BuildMetadataAndInferTypesVisitor(provider, columns, logger, compilationOptions, schemaRegistry)
    {
        protected override IReadOnlyDictionary<string, string> RetrieveInitialSourceRuntimeSettings(
            string sourceContextId,
            Musoq.Parser.Nodes.From.SchemaFromNode node)
        {
            var settings = new Dictionary<string, string>(initialSettings, StringComparer.Ordinal);
            InternalSourceRuntimeSettingsBySourceContextId[sourceContextId] = settings;
            return settings;
        }
    }

    private sealed class SettingsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new Musoq.Schema.DataSources.SchemaColumn(nameof(SettingsEntity.Token), 0, typeof(string))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(SettingsEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }

    private sealed class SettingsSource : RowSource<SettingsEntity>
    {
        [SourceRuntimeSetting("TOKEN", Secret = true, Description = "Token used by the settings source.")]
        [SourceRuntimeSetting("OPTIONAL_TOKEN", Required = false, Description = "Optional token override.")]
        public SettingsSource(SourceExecutionContext context)
        {
            Context = context;
        }

        private SourceExecutionContext Context { get; }

        public override IEnumerable<IReadOnlyList<SettingsEntity>> Chunks =>
        [
            [
                new SettingsEntity(Context.SourceRuntimeSettings.TryGetValue("TOKEN", out var token) ? token : string.Empty)
            ]
        ];
    }

    private sealed class ContextTokenResolver(Func<string, string> resolveToken) : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Assert.IsTrue(request.Requirements.Any(requirement => requirement.Name == "TOKEN"));
            return new Dictionary<string, string>
            {
                ["TOKEN"] = resolveToken(request.Identity.SourceContextId)
            };
        }
    }

    private sealed class MutableTokenResolver(string token) : ISourceRuntimeSettingsResolver
    {
        public int ResolveCount { get; private set; }

        public string Token { get; set; } = token;

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            ResolveCount += 1;
            return new Dictionary<string, string>
            {
                ["TOKEN"] = Token
            };
        }
    }

    private sealed class StaticSettingsResolver(IReadOnlyDictionary<string, string> settings)
        : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            return settings;
        }
    }

    private sealed class ProfileTokenResolver : ISourceRuntimeSettingsResolver
    {
        public ConcurrentBag<string?> ProfileNames { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            ProfileNames.Add(request.ProfileName);
            return new Dictionary<string, string>
            {
                ["TOKEN"] = $"{request.ProfileName}-token"
            };
        }
    }

    public sealed record SettingsEntity(string Token);
}
