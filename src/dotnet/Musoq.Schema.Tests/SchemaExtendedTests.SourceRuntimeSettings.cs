using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public void DescribeSourceRuntimeSettings_WhenConstructorsDeclareSettings_ReturnsRequirements()
    {
        var schema = new RuntimeSettingsSchema();
        var metadataContext = new SourceMetadataContext(
            "items:1",
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);
        var context = new SourceRuntimeSettingsDescribeContext(
            new SourceIdentity("#runtime", "items", "items:1", "items"),
            metadataContext);

        var requirements = schema.DescribeSourceRuntimeSettings("items", context, "profile");

        Assert.HasCount(2, requirements);
        var apiToken = requirements.Single(requirement => requirement.Name == "API_TOKEN");
        var endpoint = requirements.Single(requirement => requirement.Name == "ENDPOINT");

        Assert.IsTrue(apiToken.Required);
        Assert.IsTrue(apiToken.Secret);
        Assert.AreEqual(SourceRuntimeSettingPhase.All, apiToken.Phases);
        Assert.AreEqual("Token used by the metadata and execution constructors.", apiToken.Description);

        Assert.IsFalse(endpoint.Required);
        Assert.IsFalse(endpoint.Secret);
        Assert.AreEqual(SourceRuntimeSettingPhase.Execution, endpoint.Phases);
    }

    [TestMethod]
    public void DescribeSourceRuntimeSettings_WhenConstructorsDoNotMatchParameters_ReturnsEmpty()
    {
        var schema = new RuntimeSettingsSchema();
        var metadataContext = new SourceMetadataContext(
            "items:1",
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);
        var context = new SourceRuntimeSettingsDescribeContext(
            new SourceIdentity("#runtime", "items", "items:1", "items"),
            metadataContext);

        var requirements = schema.DescribeSourceRuntimeSettings("items", context, 123);

        Assert.IsEmpty(requirements);
    }

    private sealed class RuntimeSettingsSchema : SchemaBase
    {
        public RuntimeSettingsSchema()
            : base("runtime", new MethodsAggregator(new MethodsManager()))
        {
            AddTable<RuntimeSettingsTable>("items");
            AddSource<RuntimeSettingsSource>("items");
        }
    }

    private sealed class RuntimeSettingsTable : ISchemaTable
    {
        [SourceRuntimeSetting(
            "API_TOKEN",
            Secret = true,
            Description = "Token used by the metadata and execution constructors.")]
        public RuntimeSettingsTable(string profile)
        {
            _ = profile;
        }

        public ISchemaColumn[] Columns { get; } = [new SchemaColumn("Name", 0, typeof(string))];

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(RuntimeSettingsEntity));
    }

    private sealed class RuntimeSettingsSource : RowSource<RuntimeSettingsEntity>
    {
        [SourceRuntimeSetting(
            "ENDPOINT",
            Required = false,
            Phases = SourceRuntimeSettingPhase.Execution)]
        public RuntimeSettingsSource(SourceExecutionContext context, string profile)
        {
            _ = context;
            _ = profile;
        }

        public override IEnumerable<IReadOnlyList<RuntimeSettingsEntity>> Chunks => [];
    }

    private sealed record RuntimeSettingsEntity(string Name);
}
