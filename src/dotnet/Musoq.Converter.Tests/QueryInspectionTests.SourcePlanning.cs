using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenSourceAcceptsSourceLocalOrderSkipTake_ShouldReportAcceptedPlan()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.AcceptAll(request));
        var result = Inspect(
            "select p.Name from #planning.items() p order by p.Name desc skip 1 take 2",
            provider);

        var requests = provider.Requests.ToArray();
        Assert.AreEqual(1, requests.Length);
        var request = requests[0];

        Assert.AreEqual(1, request.OrderBy.Count);
        Assert.AreEqual(OrderDirection.Descending, request.OrderBy[0].Direction);
        Assert.AreEqual("Name", request.OrderBy[0].Column.Name);
        Assert.AreEqual(1, request.Skip);
        Assert.AreEqual(2, request.Take);
        Assert.AreEqual(1, provider.DescribeCount);
        Assert.Contains("source plan requested: columns=[Name], orderBy=1, skip=1, take=2", result.PlanningText);
        Assert.Contains("source plan accepted: columns=[Name], orderBy=1, skip=1, take=2", result.PlanningText);
        Assert.Contains("source plan residual: orderBy=0, skip=null, take=null", result.PlanningText);
        Assert.Contains("SourcePlanning [SourcePlan]", result.PlanningText);
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceReportsCardinalityAndDiagnostics_ShouldReportThem()
    {
        var provider = new PlanningSchemaProvider(static request =>
        {
            var rejected = SourcePlanResult.RejectAll(request);
            return rejected with
            {
                Cardinality = CardinalityEstimate.Exact(42, "test source knows its row count"),
                Diagnostics =
                [
                    OptimizationDiagnostic.Info("source estimate is exact"),
                    OptimizationDiagnostic.Warning("source declined ordering")
                ]
            };
        });
        var result = Inspect("select p.Name from #planning.items() p", provider);

        Assert.Contains("source plan cardinality: Exact, exact=42, lower=42, upper=42, confidence=1, reason=test source knows its row count", result.PlanningText);
        Assert.Contains("source plan diagnostic [TryPlanSource]: Info - source estimate is exact", result.PlanningText);
        Assert.Contains("source plan diagnostic [TryPlanSource]: Warning - source declined ordering", result.PlanningText);
        Assert.IsTrue(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5013_SourceContractWarning));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenSourceReportsOptimizationWarning_ShouldExposeSourceWarning()
    {
        var provider = new PlanningSchemaProvider(static request =>
        {
            var rejected = SourcePlanResult.RejectAll(request);
            return rejected with
            {
                Diagnostics =
                [
                    OptimizationDiagnostic.Warning("source declined ordering")
                ]
            };
        });
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name from #planning.items() p",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning);

        Assert.IsTrue(result.Succeeded);
        Assert.Contains("Source optimization warning", warning.Message);
        Assert.Contains("source declined ordering", warning.Message);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenSourceReportsWarningAndLeavesResidualWork_ShouldExposeSourceWarningOnly()
    {
        var provider = new PlanningSchemaProvider(static request =>
        {
            var rejected = SourcePlanResult.RejectAll(request);
            return rejected with
            {
                Diagnostics =
                [
                    OptimizationDiagnostic.Warning("source-specific reason for declined ordering")
                ]
            };
        });
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name from #planning.items() p order by p.Name take 1",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning);

        Assert.IsTrue(result.Succeeded);
        Assert.Contains("source-specific reason for declined ordering", warning.Message);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenSourceReportsOnlyOptimizationInfo_ShouldNotExposeFallbackWarning()
    {
        var provider = new PlanningSchemaProvider(static request =>
        {
            var rejected = SourcePlanResult.RejectAll(request);
            return rejected with
            {
                Diagnostics =
                [
                    OptimizationDiagnostic.Info("source estimate is exact")
                ]
            };
        });
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name from #planning.items() p",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForInspection_WhenFinalOrderTakeCrossesJoin_ShouldKeepSourceRequestsEmpty()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.AcceptAll(request));
        var result = Inspect(
            "select l.Name from #planning.items() l inner join #planning.items() r on l.Name = r.Name order by l.Name take 1",
            provider);

        Assert.AreEqual(2, provider.Requests.Length);
        Assert.IsTrue(provider.Requests.All(static request => request.OrderBy.Count == 0));
        Assert.IsTrue(provider.Requests.All(static request => !request.Skip.HasValue));
        Assert.IsTrue(provider.Requests.All(static request => !request.Take.HasValue));
        Assert.Contains("SourcePlanning [SourcePlan]", result.PlanningText);
        Assert.DoesNotContain("source plan accepted: orderBy=1", result.PlanningText);
        Assert.DoesNotContain("source plan accepted: orderBy=0, skip=null, take=1", result.PlanningText);
        Assert.Contains("PhysicalTopN", result.PhysicalPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceAcceptsTake_ShouldRemoveRuntimeTake()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.AcceptAll(request));
        var result = Inspect("select p.Name from #planning.items() p take 2", provider);

        Assert.Contains("source plan accepted: columns=[Name], orderBy=0, skip=null, take=2", result.PlanningText);
        Assert.DoesNotContain("PhysicalTake", result.PhysicalPlanText);
    }

    [TestMethod]
    public void CompileForExecution_WhenSourceAcceptsSkipTake_ShouldPassPlanAndUseSourceSlice()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.AcceptAll(request), CreatePlanningRows());
        var compiled = CompileForExecution("select p.Name from #planning.items() p skip 1 take 2", provider);

        var table = compiled.Run();
        Assert.AreEqual(2, table.Count);
        var executionPlans = provider.ExecutionPlans.ToArray();
        Assert.AreEqual(1, executionPlans.Length);
        var plan = executionPlans[0];

        Assert.AreEqual("bravo", table[0][0]);
        Assert.AreEqual("alpha", table[1][0]);
        Assert.AreEqual(1, plan.AcceptedSkip);
        Assert.AreEqual(2, plan.AcceptedTake);
    }

    [TestMethod]
    public void CompileForExecution_WhenSourceAcceptsFullOrderSkipTake_ShouldUseSourcePlanOnly()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.AcceptAll(request), CreatePlanningRows());
        var inspection = Inspect(
            "select p.Name from #planning.items() p order by p.Name skip 1 take 2",
            provider);
        var compiled = CompileForExecution(
            "select p.Name from #planning.items() p order by p.Name skip 1 take 2",
            provider);

        var table = compiled.Run();
        Assert.AreEqual(2, table.Count);
        var executionPlans = provider.ExecutionPlans.ToArray();
        Assert.AreEqual(1, executionPlans.Length);
        var plan = executionPlans[0];

        Assert.DoesNotContain("PhysicalTopOffset", inspection.PhysicalPlanText);
        Assert.AreEqual("bravo", table[0][0]);
        Assert.AreEqual("charlie", table[1][0]);
        Assert.AreEqual(1, plan.AcceptedOrderBy.Count);
        Assert.AreEqual(1, plan.AcceptedSkip);
        Assert.AreEqual(2, plan.AcceptedTake);
    }

    [TestMethod]
    public void CompileForExecution_WhenSourceRejectsOrdering_ShouldKeepResidualSort()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.RejectAll(request), CreatePlanningRows());
        var inspection = Inspect(
            "select p.Name from #planning.items() p order by p.Name take 2",
            provider);
        var compiled = CompileForExecution(
            "select p.Name from #planning.items() p order by p.Name take 2",
            provider);

        var table = compiled.Run();

        Assert.Contains("PhysicalTopN", inspection.PhysicalPlanText);
        Assert.Contains("source plan residual: orderBy=1, skip=null, take=2", inspection.PlanningText);
        Assert.IsFalse(inspection.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5012_OptimizationFallback));
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("bravo", table[1][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceRejectsPredicate_ShouldKeepRuntimeFilterAndWarn()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.RejectAll(request));
        var inspection = Inspect("select p.Name from #planning.items() p where p.Value > 1", provider);

        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains("source plan residual: orderBy=0, skip=null, take=null, predicate=yes", inspection.PlanningText);
        Assert.IsFalse(inspection.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceHasNoRequestedFallbackWork_ShouldNotWarn()
    {
        var provider = new PlanningSchemaProvider(static request => SourcePlanResult.RejectAll(request));
        var inspection = Inspect("select p.Name from #planning.items() p", provider);

        Assert.IsFalse(inspection.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForInspection_WhenMovedPredicateCannotBeSourcePredicateDto_ShouldKeepRuntimeFilterWithoutFallbackWarning()
    {
        var inspection = Inspect(
            "select d.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy and ToUpper(d.Dummy) = 'SINGLE'");

        Assert.Contains("SourcePredicateMovementExpansion", inspection.PlanningText);
        Assert.Contains("cannot be represented by the source predicate DTO", inspection.PlanningText);
        Assert.Contains("PhysicalFilter [(ToUpper(d.Dummy) = 'SINGLE')]", inspection.PhysicalPlanText);
        Assert.IsFalse(inspection.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenCachedCompilationIsReused_ShouldKeepNoSourceFallbackWarning()
    {
        const string query = "select d.Dummy from #system.dual() d order by d.Dummy take 1";
        var assemblyName = Guid.NewGuid().ToString();
        var provider = new SystemSchemaProvider();

        var first = InstanceCreator.CompileWithDiagnostics(query, assemblyName, provider, _loggerResolver);
        var second = InstanceCreator.CompileWithDiagnostics(query, assemblyName, provider, _loggerResolver);

        Assert.IsTrue(first.Succeeded);
        Assert.IsTrue(second.Succeeded);
        Assert.IsFalse(first.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
        Assert.IsFalse(second.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));

        var firstItems = GetBuildItems(first);
        var secondItems = GetBuildItems(second);
        Assert.IsTrue(firstItems.ContainsKey("COMPILATION"));
        Assert.IsTrue(secondItems.ContainsKey("PLANNING_RESULT"));
        Assert.IsTrue(secondItems.TryGetValue("STOP_AFTER_PLANNING", out var stopAfterPlanning) && stopAfterPlanning is true);
        Assert.IsFalse(secondItems.ContainsKey("COMPILATION"));
    }

    [TestMethod]
    public void CompileForInspection_WhenDescribeSourceReportsOptimizationDiagnostics_ShouldReportAndWarn()
    {
        var provider = new PlanningSchemaProvider(
            static request => SourcePlanResult.RejectAll(request),
            descriptorDiagnostics:
            [
                OptimizationDiagnostic.Info("descriptor found exact shape"),
                OptimizationDiagnostic.Warning("descriptor declined source-local optimization")
            ]);
        var inspection = Inspect("select p.Name from #planning.items() p", provider);
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name from #planning.items() p",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        Assert.Contains("source plan diagnostic [DescribeSource]: Info - descriptor found exact shape", inspection.PlanningText);
        Assert.Contains("source plan diagnostic [DescribeSource]: Warning - descriptor declined source-local optimization", inspection.PlanningText);
        Assert.IsTrue(inspection.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning));
        Assert.IsTrue(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning));
        Assert.IsFalse(inspection.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenDescribeSourceReportsOnlyOptimizationInfo_ShouldNotWarn()
    {
        var provider = new PlanningSchemaProvider(
            static request => SourcePlanResult.RejectAll(request),
            descriptorDiagnostics:
            [
                OptimizationDiagnostic.Info("descriptor found exact shape")
            ]);
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name from #planning.items() p",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    [TestMethod]
    public void CompileForExecution_WhenSourceAcceptsOnlyOrdering_ShouldLowerResidualTake()
    {
        var provider = new PlanningSchemaProvider(AcceptOnlyOrdering, CreatePlanningRows());
        var inspection = Inspect(
            "select p.Name from #planning.items() p order by p.Name take 2",
            provider);
        var compiled = CompileForExecution(
            "select p.Name from #planning.items() p order by p.Name take 2",
            provider);

        var table = compiled.Run();
        Assert.AreEqual(2, table.Count);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.DoesNotContain("PhysicalTopN", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalTake", inspection.PhysicalPlanText);
        Assert.Contains("source plan accepted: columns=[], orderBy=1, skip=null, take=null", inspection.PlanningText);
        Assert.Contains("source plan residual: orderBy=0, skip=null, take=2", inspection.PlanningText);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.IsFalse(executionPlan.AcceptedTake.HasValue);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("bravo", table[1][0]);
    }

    private static SourcePlanResult AcceptOnlyOrdering(SourcePlanRequest request)
    {
        return new SourcePlanResult
        {
            ExecutionPlan = new SourceExecutionPlan
            {
                Identity = request.Identity,
                AcceptedOrderBy = request.OrderBy
            },
            AcceptedOrderBy = request.OrderBy,
            ResidualSkip = request.Skip,
            ResidualTake = request.Take
        };
    }

    private static IReadOnlyList<PlanningEntity> CreatePlanningRows()
    {
        return
        [
            new PlanningEntity { Name = "delta", Value = 4 },
            new PlanningEntity { Name = "bravo", Value = 2 },
            new PlanningEntity { Name = "alpha", Value = 1 },
            new PlanningEntity { Name = "charlie", Value = 3 }
        ];
    }

    private static IReadOnlyDictionary<string, object> GetBuildItems(BuildResult result)
    {
        var property = typeof(BuildResult).GetProperty("BuildItems", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(property);
        return (IReadOnlyDictionary<string, object>)property.GetValue(result)!;
    }

    private sealed class PlanningSchemaProvider(
        Func<SourcePlanRequest, SourcePlanResult> planner,
        IReadOnlyList<PlanningEntity>? rows = null,
        IReadOnlyList<OptimizationDiagnostic>? descriptorDiagnostics = null) : ISchemaProvider
    {
        private readonly ConcurrentBag<SourcePlanRequest> _requests = [];
        private readonly ConcurrentBag<SourceExecutionPlan> _executionPlans = [];
        private readonly IReadOnlyList<OptimizationDiagnostic> _descriptorDiagnostics = descriptorDiagnostics ?? [];
        private readonly IReadOnlyList<PlanningEntity> _rows = rows ??
        [
            new PlanningEntity { Name = "left", Value = 1 },
            new PlanningEntity { Name = "right", Value = 2 }
        ];

        public int DescribeCount { get; private set; }

        public SourcePlanRequest[] Requests => _requests.ToArray();

        public IReadOnlyCollection<SourceExecutionPlan> ExecutionPlans => _executionPlans.ToArray();

        public ISchema GetSchema(string schema)
        {
            if (!PlanningSchema.MatchesName(schema))
                throw new NotSupportedException(schema);

            return new PlanningSchema(planner, _requests, _executionPlans, _rows, _descriptorDiagnostics, () => DescribeCount++);
        }
    }

    private sealed class PlanningSchema(
        Func<SourcePlanRequest, SourcePlanResult> planner,
        ConcurrentBag<SourcePlanRequest> requests,
        ConcurrentBag<SourceExecutionPlan> executionPlans,
        IReadOnlyList<PlanningEntity> rows,
        IReadOnlyList<OptimizationDiagnostic> descriptorDiagnostics,
        Action onDescribe)
        : SchemaBase(SchemaName, CreateLibrary())
    {
        public const string SchemaName = "planning";

        private const string Items = "items";

        public static bool MatchesName(string schema)
        {
            return string.Equals(schema, SchemaName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(schema, $"#{SchemaName}", StringComparison.OrdinalIgnoreCase);
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
                return new PlanningTable();

            throw new NotSupportedException(name);
        }

        public override SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters)
        {
            onDescribe();
            var descriptor = base.DescribeSource(name, context, parameters);
            return descriptorDiagnostics.Count == 0
                ? descriptor
                : descriptor with { Diagnostics = descriptor.Diagnostics.Concat(descriptorDiagnostics).ToArray() };
        }

        public override SourcePlanResult TryPlanSource(
            string name,
            SourcePlanRequest request,
            params object?[] parameters)
        {
            requests.Add(request);
            return planner(request);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            {
                executionPlans.Add(executionContext.Plan);
                return EnsureSourceType<T, PlanningEntity>(name, new PlanningRowSource(ApplyPlan(rows, executionContext.Plan)));
            }

            throw new NotSupportedException(name);
        }

        private static PlanningEntity[] ApplyPlan(
            IReadOnlyList<PlanningEntity> sourceRows,
            SourceExecutionPlan plan)
        {
            IEnumerable<PlanningEntity> query = sourceRows;
            IOrderedEnumerable<PlanningEntity>? ordered = null;

            foreach (var order in plan.AcceptedOrderBy)
            {
                var keySelector = CreateKeySelector(order.Column.Name);
                ordered = ordered == null
                    ? ApplyFirstOrdering(query, keySelector, order.Direction)
                    : ApplyNextOrdering(ordered, keySelector, order.Direction);
            }

            if (ordered != null)
                query = ordered;

            if (plan.AcceptedSkip.HasValue)
                query = query.Skip((int)plan.AcceptedSkip.Value);

            if (plan.AcceptedTake.HasValue)
                query = query.Take((int)plan.AcceptedTake.Value);

            return query.ToArray();
        }

        private static Func<PlanningEntity, object> CreateKeySelector(string columnName)
        {
            return columnName switch
            {
                nameof(PlanningEntity.Name) => static entity => entity.Name,
                nameof(PlanningEntity.Value) => static entity => entity.Value,
                _ => static entity => entity.Name
            };
        }

        private static IOrderedEnumerable<PlanningEntity> ApplyFirstOrdering(
            IEnumerable<PlanningEntity> rows,
            Func<PlanningEntity, object> keySelector,
            OrderDirection direction)
        {
            return direction == OrderDirection.Descending
                ? rows.OrderByDescending(keySelector)
                : rows.OrderBy(keySelector);
        }

        private static IOrderedEnumerable<PlanningEntity> ApplyNextOrdering(
            IOrderedEnumerable<PlanningEntity> rows,
            Func<PlanningEntity, object> keySelector,
            OrderDirection direction)
        {
            return direction == OrderDirection.Descending
                ? rows.ThenByDescending(keySelector)
                : rows.ThenBy(keySelector);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new EmptyLibrary());
            return new MethodsAggregator(methodsManager);
        }
    }

    private sealed class PlanningTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(PlanningEntity.Name), 0, typeof(string)),
            new SchemaColumn(nameof(PlanningEntity.Value), 1, typeof(int))
        ];

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(PlanningEntity));
    }

    private sealed class PlanningRowSource(IReadOnlyList<PlanningEntity> rows) : RowSourceBase<PlanningEntity>
    {
        protected override void CollectChunks(IChunkWriter<PlanningEntity> writer)
        {
            writer.Write(rows);
        }
    }

    public sealed class PlanningEntity
    {
        public string Name { get; init; } = string.Empty;

        public int Value { get; init; }
    }
}
