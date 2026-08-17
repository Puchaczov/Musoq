using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git.Tests;

[TestClass]
public sealed class GitPlanningTests : GitExampleTestBase
{
    [TestMethod]
    public void Planning_WhenColumnExistsWithDifferentCasing_ShouldAcceptColumn()
    {
        Assert.IsTrue(GitCommitPlan.CanReadColumn("authorname"));
    }

    [TestMethod]
    public void Planning_WhenStatsColumnIsProjected_ShouldAcceptProjectionOnly()
    {
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            RequiredColumns =
            [
                new SourceColumnRef(nameof(GitCommitRow.Additions))
            ]
        };

        var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

        Assert.AreEqual(1, result.AcceptedColumns.Count);
        Assert.AreEqual(nameof(GitCommitRow.Additions), result.AcceptedColumns[0].Name);
        Assert.IsNull(result.ResidualPredicate);
        Assert.AreEqual(0, result.ResidualOrderBy.Count);
    }

    [TestMethod]
    public void Planning_WhenPredicateOrderingAndWindowAreSupported_ShouldAcceptThem()
    {
        var predicate = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
            new SourcePredicateLiteral("Bob Evaluator"));
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            RequiredColumns =
            [
                new SourceColumnRef(nameof(GitCommitRow.ShortSha)),
                new SourceColumnRef(nameof(GitCommitRow.AuthorName))
            ],
            Predicate = predicate,
            OrderBy =
            [
                new OrderByExpression(
                    new SourceColumnRef(nameof(GitCommitRow.AuthoredAt)),
                    OrderDirection.Descending)
            ],
            Skip = 0,
            Take = 1
        };

        var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

        Assert.AreEqual(2, result.AcceptedColumns.Count);
        Assert.AreEqual(predicate, result.AcceptedPredicate);
        Assert.IsNull(result.ResidualPredicate);
        Assert.AreEqual(1, result.AcceptedOrderBy.Count);
        Assert.AreEqual(0, result.ResidualOrderBy.Count);
        Assert.AreEqual(0, result.AcceptedSkip);
        Assert.AreEqual(1, result.AcceptedTake);
        Assert.IsNull(result.ResidualSkip);
        Assert.IsNull(result.ResidualTake);
    }

    [TestMethod]
    public void Planning_WhenPredicateIsUnsupported_ShouldLeaveItResidual()
    {
        var predicate = new UnsupportedPredicateExpression();
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = predicate,
            Take = 2
        };

        var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

        Assert.IsNull(result.AcceptedPredicate);
        Assert.AreEqual(predicate, result.ResidualPredicate);
        Assert.IsNull(result.AcceptedTake);
        Assert.AreEqual(2, result.ResidualTake);
    }

    [TestMethod]
    public void Planning_WhenAndPredicateHasCheapAndStatsParts_ShouldPartiallyAcceptCheapPredicate()
    {
        var cheapPredicate = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
            new SourcePredicateLiteral("Bob Evaluator"));
        var statsPredicate = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.GreaterThan,
            new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.Additions))),
            new SourcePredicateLiteral(100));
        var predicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.And,
            cheapPredicate,
            statsPredicate);
        var request = SourcePlanRequest.Empty(new SourceIdentity("#git", GitSchema.Commits, "source-id", "g")) with
        {
            Predicate = predicate,
            Take = 1
        };

        var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

        Assert.AreEqual(cheapPredicate, result.AcceptedPredicate);
        Assert.AreEqual(statsPredicate, result.ResidualPredicate);
        Assert.IsNull(result.AcceptedTake);
        Assert.AreEqual(1, result.ResidualTake);
        Assert.IsTrue(result.Diagnostics.Any(item => item.Optimization == "GitPredicatePushdown"));
        Assert.IsTrue(result.Diagnostics.Any(item => item.Optimization == "GitSlicePushdown"));
    }

    [TestMethod]
    public void Planning_WhenOrPredicateIsFullySupported_ShouldAcceptWholePredicate()
    {
        var predicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.Or,
            new SourcePredicateComparison(
                SourcePredicateComparisonOperator.Equal,
                new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
                new SourcePredicateLiteral("Bob Evaluator")),
            new SourcePredicateComparison(
                SourcePredicateComparisonOperator.Equal,
                new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
                new SourcePredicateLiteral("Alice Runtime")));
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = predicate
        };

        var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

        Assert.AreEqual(predicate, result.AcceptedPredicate);
        Assert.IsNull(result.ResidualPredicate);
    }

    [TestMethod]
    public void Planning_WhenOrPredicateContainsStatsColumn_ShouldLeaveWholePredicateResidual()
    {
        var predicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.Or,
            new SourcePredicateComparison(
                SourcePredicateComparisonOperator.Equal,
                new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
                new SourcePredicateLiteral("Bob Evaluator")),
            new SourcePredicateComparison(
                SourcePredicateComparisonOperator.GreaterThan,
                new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.Additions))),
                new SourcePredicateLiteral(100)));
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = predicate
        };

        var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

        Assert.IsNull(result.AcceptedPredicate);
        Assert.AreEqual(predicate, result.ResidualPredicate);
    }

    [TestMethod]
    public void Planning_WhenPredicateUsesInNotInAndNullChecks_ShouldAcceptSupportedForms()
    {
        SourcePredicateExpression[] predicates =
        [
            new SourcePredicateIn(
                new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.Branch))),
                [new SourcePredicateLiteral("main")]),
            new SourcePredicateIn(
                new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
                [new SourcePredicateLiteral("Alice Runtime")],
                IsNegated: true),
            new SourcePredicateNullCheck(
                new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.Message))),
                IsNegated: true)
        ];

        foreach (var predicate in predicates)
        {
            var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
            {
                Predicate = predicate
            };

            var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

            Assert.AreEqual(predicate, result.AcceptedPredicate);
            Assert.IsNull(result.ResidualPredicate);
        }
    }

    [TestMethod]
    public void Planning_WhenPredicateUsesStatsColumn_ShouldLeaveItResidualAndNotLoadStats()
    {
        var store = new TrackingGitHistoryStore();
        var predicate = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.GreaterThan,
            new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.Additions))),
            new SourcePredicateLiteral(100));
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = predicate
        };

        var result = new GitSchema(store).TryPlanSource(GitSchema.Commits, request);

        Assert.IsNull(result.AcceptedPredicate);
        Assert.AreEqual(predicate, result.ResidualPredicate);
        Assert.AreEqual(0, store.StatsLoadCount);
        Assert.IsNotNull(result.Cardinality);
        Assert.AreEqual(CardinalityKind.Exact, result.Cardinality.Kind);
        Assert.AreEqual(5, result.Cardinality.ExactRows);
    }

    [TestMethod]
    public void Planning_WhenOrderUsesStatsColumn_ShouldLeaveItResidualAndNotLoadStats()
    {
        var store = new TrackingGitHistoryStore();
        var orderBy = new[]
        {
            new OrderByExpression(
                new SourceColumnRef(nameof(GitCommitRow.Churn)),
                OrderDirection.Descending)
        };
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            OrderBy = orderBy,
            Take = 2
        };

        var result = new GitSchema(store).TryPlanSource(GitSchema.Commits, request);

        Assert.AreEqual(0, result.AcceptedOrderBy.Count);
        Assert.AreEqual(1, result.ResidualOrderBy.Count);
        Assert.IsNull(result.AcceptedTake);
        Assert.AreEqual(2, result.ResidualTake);
        Assert.IsTrue(result.Diagnostics.Any(item => item.Optimization == "GitOrderPushdown"));
        Assert.IsTrue(result.Diagnostics.Any(item => item.Optimization == "GitSlicePushdown"));
        Assert.AreEqual(0, store.StatsLoadCount);
    }

    [TestMethod]
    public void Planning_WhenAcceptedOperationsAreKnown_ShouldReturnExactCardinality()
    {
        var predicate = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
            new SourcePredicateLiteral("Bob Evaluator"));
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = predicate,
            OrderBy =
            [
                new OrderByExpression(
                    new SourceColumnRef(nameof(GitCommitRow.AuthoredAt)),
                    OrderDirection.Descending)
            ],
            Take = 1
        };

        var result = new GitSchema().TryPlanSource(GitSchema.Commits, request);

        Assert.IsNotNull(result.Cardinality);
        Assert.AreEqual(CardinalityKind.Exact, result.Cardinality.Kind);
        Assert.AreEqual(1, result.Cardinality.ExactRows);
        Assert.IsTrue(result.ExecutionPlan.Properties.TryGetValue(
            GitSourcePlanProperties.PlanningNotes,
            out var planningNotes));
        StringAssert.Contains((string)planningNotes!, "accepted predicate");
        StringAssert.Contains((string)planningNotes!, "accepted order");
        StringAssert.Contains((string)planningNotes!, "accepted slice");
    }

    [TestMethod]
    public void Planning_WhenRepositoryIsResolved_ShouldStoreRepositoryInExecutionPlanProperties()
    {
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            SourceRuntimeSettings = new Dictionary<string, string>
            {
                [GitSchema.RepositoryRuntimeSetting] = "docs"
            }
        };

        var runtimeResult = new GitSchema().TryPlanSource(GitSchema.Commits, request);
        var staticResult = new GitSchema().TryPlanSource(GitSchema.Commits, request, "musoq");

        Assert.AreEqual("docs", runtimeResult.ExecutionPlan.Properties[GitSourcePlanProperties.Repository]);
        Assert.AreEqual("musoq", staticResult.ExecutionPlan.Properties[GitSourcePlanProperties.Repository]);
    }
}
