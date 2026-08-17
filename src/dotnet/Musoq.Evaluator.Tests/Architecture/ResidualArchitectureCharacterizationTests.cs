using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureCharacterizationTests
{
    [TestMethod]
    public void AggregateAndWindowLowerers_ShouldUseTypedServicesInsteadOfKernelDelegates()
    {
        var kernel = RuntimeHelpers.GetUninitializedObject(typeof(PhysicalLoweringImplementation));
        var aggregateLowerer = InvokeFactory(kernel, "CreateAggregatePlanLowerer");
        var windowLowerer = InvokeFactory(kernel, "CreateWindowPlanLowerer");

        Assert.IsEmpty(EnumerateDelegates(aggregateLowerer).Concat(EnumerateDelegates(windowLowerer)));
        Assert.AreEqual(
            typeof(IAggregateLoweringService),
            typeof(AggregatePlanLowerer).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType);
        Assert.AreEqual(
            typeof(IWindowLoweringService),
            typeof(WindowPlanLowerer).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType);
    }

    [TestMethod]
    public void AggregateAndWindowServices_ShouldBeTopLevelAndAcceptOperationsContracts()
    {
        Assert.IsFalse(typeof(AggregateLoweringService).IsNested);
        Assert.IsFalse(typeof(WindowLoweringService).IsNested);
        Assert.AreEqual(
            typeof(IAggregateLoweringOperations),
            typeof(AggregateLoweringService).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType);
        Assert.AreEqual(
            typeof(IWindowLoweringOperations),
            typeof(WindowLoweringService).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType);
        Assert.IsEmpty(EnumerateDelegates(RuntimeHelpers.GetUninitializedObject(typeof(AggregateLoweringService))));
    }

    [TestMethod]
    public void JoinAndApplyServices_ShouldBeTopLevelAndAcceptOperationsContracts()
    {
        Assert.IsFalse(typeof(JoinLoweringService).IsNested);
        Assert.IsFalse(typeof(ApplyLoweringService).IsNested);
        Assert.AreEqual(
            typeof(IJoinLoweringOperations),
            typeof(JoinLoweringService).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType);
        Assert.AreEqual(
            typeof(IApplyLoweringOperations),
            typeof(ApplyLoweringService).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType);
    }

    [TestMethod]
    public void LoweringDispatch_ShouldCarryBuiltProductsInsteadOfStatusModels()
    {
        var registryPlanResult = typeof(PhysicalLoweringRegistry)
            .GetMethod(nameof(PhysicalLoweringRegistry.TryBuildPlan))!
            .ReturnType
            .GetGenericArguments()[0];
        var registryTableResult = typeof(PhysicalLoweringRegistry)
            .GetMethod(nameof(PhysicalLoweringRegistry.TryBuildTable))!
            .ReturnType
            .GetGenericArguments()[0];

        Assert.AreEqual(typeof(ExecutionPlan), registryPlanResult);
        Assert.AreEqual(typeof(LoweredTable), registryTableResult);
        Assert.IsFalse(typeof(LoweredTable).GetProperties().Any(static property =>
            property.Name is "IsBuilt" or "Supported" or "UnsupportedReason"));

        // The public compatibility adapter retains its documented shape at the
        // outer builder boundary; it is no longer the internal dispatch value.
        Assert.IsNotNull(typeof(ExecutionPlanBuildResult).GetProperty("Supported"));
        Assert.IsNotNull(typeof(TableBuildResult).GetProperty("IsBuilt"));
        Assert.IsNotNull(typeof(TableBuildResult).GetProperty("UnsupportedReason"));
    }

    [TestMethod]
    public void SemanticVisitors_ShouldCurrentlyBeSplitAcrossPartialFiles()
    {
        var files = RepositorySourceScan.FilesUnder(
            RepositorySourceScan.RepositoryRoot(),
            "src/dotnet/Musoq.Evaluator/Visitors",
            "*.cs");

        var partialFiles = files.Count(file =>
        {
            var text = System.IO.File.ReadAllText(file);
            return text.Contains("partial class BuildMetadataAndInferTypesVisitor", StringComparison.Ordinal) ||
                   text.Contains("partial class RewriteQueryVisitor", StringComparison.Ordinal);
        });

        Assert.IsGreaterThan(1, partialFiles);
    }

    [TestMethod]
    public void OperatorCatalog_ShouldUseRegisteredNodeBehavior()
    {
        var plan = new ExecutionPlan(
            "characterization",
            [],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(
                    new ExecutionVariable("results", typeof(Table)),
                    new GeneratedRowShape("ResultRow", [])),
                new ExecutionReturnTable(new ExecutionVariable("results", typeof(Table)))
            ]));

        var catalog = ExecutionPlanOperatorCatalog.Create(plan);

        Assert.AreEqual("ExecutionPlan", catalog.Operators[0].NodeKind);
        StringAssert.Contains(catalog.AnnotatedExecutionPlanText, "[op1]");
    }

    [TestMethod]
    public void RuntimeBindingSnapshot_ShouldIsolateNestedSourcePlanState()
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["value"] = "before"
        };
        var sourcePlan = new SourceExecutionPlan
        {
            Identity = SourceIdentity.Empty,
            Properties = properties
        };
        var runnable = new CharacterizationRunnable
        {
            SourceExecutionPlans = new Dictionary<string, SourceExecutionPlan>(StringComparer.Ordinal)
            {
                ["source"] = sourcePlan
            }
        };

        var snapshot = QueryExecutionContext.Capture(runnable, [], CancellationToken.None);
        properties["value"] = "after";

        Assert.AreEqual(
            "before",
            snapshot.Binding.SourceExecutionPlans["source"].Properties["value"]);
    }

    [TestMethod]
    public async Task CompiledQuery_ShouldCurrentlySerializeRunsThroughOneGate()
    {
        using var query = new CompiledQuery(new ConcurrencyCharacterizationRunnable());

        await Task.WhenAll(
            Task.Run(() => query.Run()),
            Task.Run(() => query.Run()));

        var runnable = (ConcurrencyCharacterizationRunnable)GetRunnable(query);
        Assert.AreEqual(1, runnable.MaximumConcurrentRuns);
    }

    [TestMethod]
    public void TypeInspectionCaches_ShouldUseWeakTypeKeys()
    {
        var fields = new[]
        {
            typeof(EvaluationHelper).GetField("CastableTypeCache", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(EvaluationHelper).GetField("ObjectChunkAdapters", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField("HasIndexerCache", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField("IsIndexableCache", BindingFlags.Static | BindingFlags.NonPublic),
            typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField("TypeHintAttributeCache", BindingFlags.Static | BindingFlags.NonPublic)
        };

        Assert.IsTrue(fields.All(static field =>
            field is not null &&
            field.FieldType.IsGenericType &&
            field.FieldType.GetGenericTypeDefinition() == typeof(WeakTypeRuntimeCache<>)));
    }

    [TestMethod]
    public void GeneratedAssemblyActivation_ShouldUseA_CollectibleLoadPath()
    {
        var files = RepositorySourceScan.ProductionSourceFiles(
            RepositorySourceScan.RepositoryRoot(),
            "Musoq.Targets.CSharpClr");

        var source = string.Join(
            Environment.NewLine,
            files.Select(System.IO.File.ReadAllText));

        Assert.IsFalse(source.Contains("Assembly.Load(", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("AssemblyLoadContext", StringComparison.Ordinal));
    }

    private static object InvokeFactory(object target, string methodName)
    {
        var method = typeof(PhysicalLoweringImplementation).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return method!.Invoke(target, null)!;
    }

    private static IEnumerable<Delegate> EnumerateDelegates(object value)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return EnumerateDelegates(value, visited);
    }

    private static IEnumerable<Delegate> EnumerateDelegates(object? value, HashSet<object> visited)
    {
        if (value is null || value is string || value.GetType().IsPrimitive || !visited.Add(value))
            yield break;

        if (value is Delegate handler)
        {
            yield return handler;
            yield break;
        }

        foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var nested in EnumerateDelegates(field.GetValue(value), visited))
                yield return nested;
        }
    }

    private static ITableRunnable GetRunnable(CompiledQuery query)
    {
        var field = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (ITableRunnable)field!.GetValue(query)!;
    }

    private class CharacterizationRunnable : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = new NullLogger<object>();

        public event QueryPhaseEventHandler? PhaseChanged
        {
            add { }
            remove { }
        }

        public event DataSourceEventHandler? DataSourceProgress
        {
            add { }
            remove { }
        }

        public virtual Table Run(CancellationToken token) => new("empty", []);
    }

    private sealed class ConcurrencyCharacterizationRunnable : CharacterizationRunnable
    {
        private int _activeRuns;

        public override Table Run(CancellationToken token)
        {
            var active = Interlocked.Increment(ref _activeRuns);
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrentRuns);
                if (active <= current ||
                    Interlocked.CompareExchange(ref _maximumConcurrentRuns, active, current) == current)
                {
                    break;
                }
            }
            Thread.Sleep(25);
            Interlocked.Decrement(ref _activeRuns);
            return new Table("empty", []);
        }

        private int _maximumConcurrentRuns;

        public int MaximumConcurrentRuns => Volatile.Read(ref _maximumConcurrentRuns);
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) =>
            throw new NotSupportedException("Characterization runnable has no schema.");
    }
}
