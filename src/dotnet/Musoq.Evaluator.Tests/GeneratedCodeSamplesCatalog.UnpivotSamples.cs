namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateUnpivotSamples()
    {
        return
        [
            Basic(
                "Q159_UnpivotBasicStreaming",
                "Unpivot",
                @"unpivot #A.entities() s
                  on Metric in (s.Population as Population, s.Money as Money)
                  using Amount
                  keep s.Country as Country"),
            Basic(
                "Q160_UnpivotCteNullableOrdering",
                "Unpivot",
                @"with u as (
                      unpivot #A.entities() s
                      on Metric in (s.NullableValue as NullableValue, null as ExplicitNull)
                      using Value
                      keep s.Name + ':' + s.Country as Label
                  )
                  select Label, Metric, Value
                  from u
                  order by Label, Metric
                  skip 1
                  take 5"),
            Basic(
                "Q161_UnpivotSetOperator",
                "Unpivot",
                @"unpivot #A.entities() s
                  on Metric in (s.Population as Population)
                  using Amount
                  keep s.Name as Name
                  union all (Name, Metric, Amount)
                  unpivot #B.entities() s
                  on Metric in (s.Money as Money)
                  using Amount
                  keep s.Name as Name
                  order by Name")
        ];
    }
}
