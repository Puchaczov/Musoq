using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
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
        var lowererFields = CompilerContractSymbols
            .For(typeof(AggregatePlanLowerer))
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Concat(
                CompilerContractSymbols
                    .For(typeof(WindowPlanLowerer))
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            );
        Assert.IsEmpty(
            lowererFields.Where(field =>
                CompilerContractSymbols.For(typeof(Delegate)).IsAssignableFrom(field.FieldType)
            )
        );
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(IAggregateLoweringService)),
                CompilerContractSymbols
                    .For(typeof(AggregatePlanLowerer))
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single()
                    .GetParameters()
                    .Single()
                    .ParameterType
            )
        );
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(IWindowLoweringService)),
                CompilerContractSymbols
                    .For(typeof(WindowPlanLowerer))
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single()
                    .GetParameters()
                    .Single()
                    .ParameterType
            )
        );
    }

    [TestMethod]
    public void AggregateAndWindowServices_ShouldBeTopLevelAndAcceptOperationsContracts()
    {
        Assert.IsFalse(CompilerContractSymbols.For(typeof(AggregateLoweringService)).IsNested);
        Assert.IsFalse(CompilerContractSymbols.For(typeof(WindowLoweringService)).IsNested);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(IAggregateLoweringOperations)),
                CompilerContractSymbols
                    .For(typeof(AggregateLoweringService))
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single()
                    .GetParameters()
                    .Single()
                    .ParameterType
            )
        );
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(IWindowLoweringOperations)),
                CompilerContractSymbols
                    .For(typeof(WindowLoweringService))
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single()
                    .GetParameters()
                    .Single()
                    .ParameterType
            )
        );
        Assert.IsEmpty(
            CompilerContractSymbols
                .For(typeof(AggregateLoweringService))
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => CompilerContractSymbols.For(typeof(Delegate)).IsAssignableFrom(field.FieldType))
        );
    }

    [TestMethod]
    public void JoinAndApplyServices_ShouldBeTopLevelAndAcceptOperationsContracts()
    {
        Assert.IsFalse(CompilerContractSymbols.For(typeof(JoinLoweringService)).IsNested);
        Assert.IsFalse(CompilerContractSymbols.For(typeof(ApplyLoweringService)).IsNested);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(IJoinLoweringOperations)),
                CompilerContractSymbols
                    .For(typeof(JoinLoweringService))
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single()
                    .GetParameters()
                    .Single()
                    .ParameterType
            )
        );
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(IApplyLoweringOperations)),
                CompilerContractSymbols
                    .For(typeof(ApplyLoweringService))
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single()
                    .GetParameters()
                    .Single()
                    .ParameterType
            )
        );
    }

    [TestMethod]
    public void LoweringDispatch_ShouldCarryBuiltProductsInsteadOfStatusModels()
    {
        var registryPlanResult = CompilerContractSymbols
            .For(typeof(PhysicalLoweringRegistry))
            .GetMethod(nameof(PhysicalLoweringRegistry.TryBuildPlan))!
            .ReturnType.GetGenericArguments()[0];
        var registryTableResult = CompilerContractSymbols
            .For(typeof(PhysicalLoweringRegistry))
            .GetMethod(nameof(PhysicalLoweringRegistry.TryBuildTable))!
            .ReturnType.GetGenericArguments()[0];

        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(ExecutionPlan)),
                registryPlanResult
            )
        );
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(LoweredTable)),
                registryTableResult
            )
        );
        Assert.IsFalse(
            CompilerContractSymbols
                .For(typeof(LoweredTable))
                .GetProperties()
                .Any(static property => property.MetadataName is "IsBuilt" or "Supported" or "UnsupportedReason")
        );

        // The public compatibility adapter retains its documented shape at the
        // outer builder boundary; it is no longer the internal dispatch value.
        Assert.IsNotNull(CompilerContractSymbols.For(typeof(ExecutionPlanBuildResult)).GetProperty("Supported"));
        Assert.IsNotNull(CompilerContractSymbols.For(typeof(TableBuildResult)).GetProperty("IsBuilt"));
        Assert.IsNotNull(CompilerContractSymbols.For(typeof(TableBuildResult)).GetProperty("UnsupportedReason"));
    }

    [TestMethod]
    public void OperatorCatalog_ShouldUseRegisteredNodeBehavior()
    {
        var plan = new ExecutionPlan(
            "characterization",
            [],
            new ExecutionBlock([
                new ExecutionCreateTable(
                    new ExecutionVariable("results", typeof(Table)),
                    new GeneratedRowShape("ResultRow", [])
                ),
                new ExecutionReturnTable(new ExecutionVariable("results", typeof(Table))),
            ])
        );

        var catalog = ExecutionPlanOperatorCatalog.Create(plan);

        Assert.AreEqual("ExecutionPlan", catalog.Operators[0].NodeKind);
        StringAssert.Contains(catalog.AnnotatedExecutionPlanText, "[op1]");
    }

    [TestMethod]
    public void RuntimeBindingSnapshot_ShouldIsolateNestedSourcePlanState()
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["value"] = "before" };
        var sourcePlan = new SourceExecutionPlan { Identity = SourceIdentity.Empty, Properties = properties };
        var runnable = new CharacterizationRunnable
        {
            SourceExecutionPlans = new Dictionary<string, SourceExecutionPlan>(StringComparer.Ordinal)
            {
                ["source"] = sourcePlan,
            },
        };

        var snapshot = QueryExecutionContext.Capture(runnable, TypedParameterSnapshot.Empty, CancellationToken.None);
        properties["value"] = "after";

        Assert.AreEqual("before", snapshot.Binding.SourceExecutionPlans["source"].Properties["value"]);
    }

    [TestMethod]
    public void CompiledQuery_ShouldCurrentlySerializeRunsThroughOneGate()
    {
        using var contention = new global::Musoq.Tests.Common.SynchronousContention();
        var runnable = new ConcurrencyCharacterizationRunnable(contention);
        using var query = new CompiledQuery(runnable);
        _ = contention.Run(() =>
        {
            using var table = query.Run();
            return table.Count;
        });
        Assert.AreEqual(1, runnable.MaximumConcurrentRuns);
    }

    [TestMethod]
    public void TypeInspectionCaches_ShouldUseWeakTypeKeys()
    {
        var fields = new[]
        {
            CompilerContractSymbols
                .For(typeof(EvaluationHelper))
                .GetField("CastableTypeCache", BindingFlags.Static | BindingFlags.NonPublic),
            CompilerContractSymbols
                .For(typeof(BuildMetadataAndInferTypesVisitorUtilities))
                .GetField("HasIndexerCache", BindingFlags.Static | BindingFlags.NonPublic),
            CompilerContractSymbols
                .For(typeof(BuildMetadataAndInferTypesVisitorUtilities))
                .GetField("IsIndexableCache", BindingFlags.Static | BindingFlags.NonPublic),
            CompilerContractSymbols
                .For(typeof(BuildMetadataAndInferTypesVisitorUtilities))
                .GetField("TypeHintAttributeCache", BindingFlags.Static | BindingFlags.NonPublic),
        };

        Assert.IsTrue(
            fields.All(static field =>
                field is not null
                && field.FieldType.IsGenericType
                && SymbolEqualityComparer.Default.Equals(
                    field.FieldType.GetGenericTypeDefinition(),
                    CompilerContractSymbols.For(typeof(WeakTypeRuntimeCache<>))
                )
            )
        );
    }

    [TestMethod]
    public void GeneratedAssemblyActivation_ShouldUseA_CollectibleLoadPath()
    {
        var files = RepositorySourceScan.ProductionSourceFiles(
            RepositorySourceScan.RepositoryRoot(),
            "Musoq.Targets.CSharpClr"
        );

        var source = string.Join(Environment.NewLine, files.Select(System.IO.File.ReadAllText));

        Assert.IsFalse(source.Contains("Assembly.Load(", StringComparison.Ordinal));
        Assert.IsTrue(source.Contains("AssemblyLoadContext", StringComparison.Ordinal));
    }

    private class CharacterizationRunnable : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<
            string,
            IReadOnlyDictionary<string, string>
        > SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<
            string,
            IReadOnlyList<SourceRuntimeSettingDescription>
        > SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
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

    private sealed class ConcurrencyCharacterizationRunnable(
        global::Musoq.Tests.Common.SynchronousContention contention
    ) : CharacterizationRunnable
    {
        private int _activeRuns;

        public override Table Run(CancellationToken token)
        {
            var active = Interlocked.Increment(ref _activeRuns);
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrentRuns);
                if (
                    active <= current
                    || Interlocked.CompareExchange(ref _maximumConcurrentRuns, active, current) == current
                )
                {
                    break;
                }
            }
            contention.HoldOwner();
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
