using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks.Scenarios;

public sealed partial class FixtureSchema
{
    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters) with
        {
            ExecutionPermissions = SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing
        };
        if (!name.Equals("planned", StringComparison.OrdinalIgnoreCase))
            return descriptor;
        return descriptor with
        {
            PredicateCapabilities = new SourcePredicateCapabilities
            {
                StringMatches = [new(new SourceColumnRef("Name"), SourceStringMatchOperations.Prefix)]
            }
        };
    }

    public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
    {
        var rejected = SourcePlanResult.RejectAll(request);
        if (!name.Equals("planned", StringComparison.OrdinalIgnoreCase) ||
            request.Predicate is not SourcePredicateStringMatch { Kind: SourceStringMatchKind.Prefix, IsNegated: false } predicate)
            return rejected;
        return rejected with
        {
            AcceptedPredicate = predicate,
            ResidualPredicate = null,
            ExecutionPlan = rejected.ExecutionPlan with
            {
                AcceptedPredicate = predicate,
                PredicateApplications = SourcePredicateApplication.ForRowFiltering(predicate)
            }
        };
    }
}
