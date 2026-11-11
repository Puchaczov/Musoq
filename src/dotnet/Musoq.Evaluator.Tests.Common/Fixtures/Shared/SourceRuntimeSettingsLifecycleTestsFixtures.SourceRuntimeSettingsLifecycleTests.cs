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

public partial class SourceRuntimeSettingsLifecycleTestsFixtures
{
    internal static void AssertDescSettingsRows(Table table, params object?[][] rows)
    {
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Required", typeof(bool)),
            ("Secret", typeof(bool)),
            ("Phases", typeof(string)),
            ("Status", typeof(string)),
            ("Description", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, rows);
    }

    internal static CompiledQuery CompileWithInitialSettings(
        string query,
        ISchemaProvider provider,
        IReadOnlyDictionary<string, string> initialSettings
    )
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            () => new CreateTree(new TransformTree(new TurnQueryIntoRunnableCode(null), new TestsLoggerResolver())),
            items =>
            {
                items.CreateBuildMetadataAndInferTypesVisitor = (
                    schemaProvider,
                    columns,
                    compilationOptions,
                    schemaRegistry,
                    logger
                ) =>
                    new InitialSourceRuntimeSettingsVisitor(
                        schemaProvider,
                        columns,
                        logger,
                        initialSettings,
                        compilationOptions,
                        schemaRegistry
                    );
            }
        );
    }

    public sealed class SettingsSchemaProvider : ISchemaProvider
    {
        public SettingsSchemaProvider(bool declareRequirement)
            : this(declareRequirement ? SettingsDeclarationMode.RequiredAndOptional : SettingsDeclarationMode.None) { }

        public SettingsSchemaProvider(SettingsDeclarationMode declarationMode)
        {
            Schema = new SettingsSchema(declarationMode);
        }

        public SettingsSchema Schema { get; }

        public ISchema GetSchema(string schema)
        {
            if (
                !string.Equals(schema, "settings", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(schema, "#settings", StringComparison.OrdinalIgnoreCase)
            )
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
        OptionalOnly,
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

        public ConcurrentBag<IReadOnlyDictionary<string, string>> RequirementSettings { get; } = [];

        public int DescribeRuntimeSettingsCount { get; private set; }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters
        )
        {
            MetadataSettings.Add(metadataContext.SourceRuntimeSettings);
            return base.GetTableByName(name, metadataContext, parameters);
        }

        public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
            string name,
            SourceRuntimeSettingsDescribeContext context,
            params object?[] parameters
        )
        {
            RequirementSettings.Add(context.MetadataContext.SourceRuntimeSettings);
            DescribeRuntimeSettingsCount += 1;
            var requirements = base.DescribeSourceRuntimeSettings(name, context, parameters);
            return DeclarationMode switch
            {
                SettingsDeclarationMode.RequiredAndOptional => requirements,
                SettingsDeclarationMode.OptionalOnly => requirements
                    .Where(static requirement => requirement.Name == "OPTIONAL_TOKEN")
                    .ToArray(),
                _ => [],
            };
        }

        public override SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters
        )
        {
            MetadataSettings.Add(context.MetadataContext.SourceRuntimeSettings);
            return base.DescribeSource(name, context, parameters);
        }

        public override SourcePlanResult TryPlanSource(
            string name,
            SourcePlanRequest request,
            params object?[] parameters
        )
        {
            PlanSettings.Add(request.SourceRuntimeSettings);
            return SourcePlanResult.RejectAll(request);
        }

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext executionContext,
            TArguments nativeArguments
        )
        {
            ExecutionSettings.Add(executionContext.SourceRuntimeSettings);
            return EnsureSourceType<T, SettingsEntity>(name, new SettingsSource(executionContext));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(methodsManager);
        }
    }

    internal sealed class InitialSourceRuntimeSettingsVisitor(
        ISchemaProvider provider,
        IReadOnlyDictionary<string, string[]> columns,
        Microsoft.Extensions.Logging.ILogger<BuildMetadataAndInferTypesVisitor> logger,
        IReadOnlyDictionary<string, string> initialSettings,
        CompilationOptions compilationOptions,
        SchemaRegistry? schemaRegistry
    ) : BuildMetadataAndInferTypesVisitor(provider, columns, logger, compilationOptions, schemaRegistry)
    {
        protected override IReadOnlyDictionary<string, string> RetrieveInitialSourceRuntimeSettings(
            string sourceContextId,
            Musoq.Parser.Nodes.From.SchemaFromNode node
        )
        {
            var settings = new Dictionary<string, string>(initialSettings, StringComparer.Ordinal);
            InternalSourceRuntimeSettingsBySourceContextId[sourceContextId] = settings;
            return settings;
        }
    }

    internal sealed class SettingsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [new Musoq.Schema.DataSources.SchemaColumn(nameof(SettingsEntity.Token), 0, typeof(string))];

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

    internal sealed class SettingsSource : RowSource<SettingsEntity>
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
                    new SettingsEntity(
                        Context.SourceRuntimeSettings.TryGetValue("TOKEN", out var token) ? token : string.Empty
                    ),
                ],
            ];
    }

    internal sealed class ContextTokenResolver(Func<string, string> resolveToken) : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Assert.IsTrue(request.Requirements.Any(requirement => requirement.Name == "TOKEN"));
            return new Dictionary<string, string> { ["TOKEN"] = resolveToken(request.Identity.SourceContextId) };
        }
    }

    internal sealed class MutableTokenResolver(string token) : ISourceRuntimeSettingsResolver
    {
        public int ResolveCount { get; private set; }

        public string Token { get; set; } = token;

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            ResolveCount += 1;
            return new Dictionary<string, string> { ["TOKEN"] = Token };
        }
    }

    internal sealed class StaticSettingsResolver(IReadOnlyDictionary<string, string> settings)
        : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            return settings;
        }
    }

    internal sealed class ProfileTokenResolver : ISourceRuntimeSettingsResolver
    {
        public ConcurrentBag<string?> ProfileNames { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            ProfileNames.Add(request.ProfileName);
            return new Dictionary<string, string> { ["TOKEN"] = $"{request.ProfileName}-token" };
        }
    }

    public sealed record SettingsEntity(string Token);
}
