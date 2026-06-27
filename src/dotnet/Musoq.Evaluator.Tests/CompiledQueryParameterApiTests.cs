using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CompiledQueryParameterApiTests
{
    [TestMethod]
    public void Parameters_ShouldExposeParameterizedRunnableDictionary()
    {
        var runnable = new ParameterizedRunnable();
        var query = new CompiledQuery(runnable) { Parameters = { ["author"] = "Ada" } };

        Assert.AreSame(runnable.Parameters, query.Parameters);
        Assert.AreEqual("Ada", runnable.Parameters["author"]);
    }

    [TestMethod]
    public void ParameterDefinitions_ShouldExposeRequiredParameters()
    {
        var runnable = new ParameterizedRunnable();
        runnable.ParameterDefinitions.Add(new ScriptParameterDefinition("author", typeof(string), false, null));
        runnable.ParameterDefinitions.Add(new ScriptParameterDefinition("limit", typeof(int), true, 100));

        var query = new CompiledQuery(runnable);

        Assert.HasCount(2, query.ParameterDefinitions);
        Assert.HasCount(1, query.RequiredParameters);
        Assert.AreEqual("author", query.RequiredParameters[0].Name);
        Assert.AreSame(query.ParameterDefinitions, query.ParameterDefinitions);
        Assert.AreSame(query.RequiredParameters, query.RequiredParameters);
    }

    [TestMethod]
    public void ParameterDefinitions_WhenRunnableMetadataChangesAfterConstruction_ShouldExposeSnapshot()
    {
        var runnable = new ParameterizedRunnable();
        runnable.ParameterDefinitions.Add(new ScriptParameterDefinition("author", typeof(string), false, null));
        var query = new CompiledQuery(runnable);

        runnable.ParameterDefinitions.Add(new ScriptParameterDefinition("limit", typeof(int), true, 100));

        Assert.HasCount(1, query.ParameterDefinitions);
        Assert.HasCount(1, query.RequiredParameters);
        Assert.AreEqual("author", query.RequiredParameters[0].Name);
    }

    [TestMethod]
    public void ParameterDefinitions_WhenRunnableIsNotParameterized_ShouldExposeEmptyMetadata()
    {
        var query = new CompiledQuery(new PlainRunnable());

        Assert.IsEmpty(query.ParameterDefinitions);
        Assert.IsEmpty(query.RequiredParameters);
    }

    private sealed class ParameterizedRunnable : ITableRunnable, IParameterizedRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = new NullLogger<object>();

        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        public List<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        IReadOnlyList<ScriptParameterDefinition> IParameterizedRunnable.ParameterDefinitions => ParameterDefinitions;

        public event QueryPhaseEventHandler PhaseChanged
        {
            add { }
            remove { }
        }

        public event DataSourceEventHandler DataSourceProgress
        {
            add { }
            remove { }
        }

        public Table Run(CancellationToken token)
        {
            return new Table("empty", []);
        }
    }

    private sealed class PlainRunnable : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = new NullLogger<object>();

        public event QueryPhaseEventHandler PhaseChanged
        {
            add { }
            remove { }
        }

        public event DataSourceEventHandler DataSourceProgress
        {
            add { }
            remove { }
        }

        public Table Run(CancellationToken token)
        {
            return new Table("empty", []);
        }
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new NotSupportedException("This runnable is metadata-only in these tests.");
        }
    }
}
