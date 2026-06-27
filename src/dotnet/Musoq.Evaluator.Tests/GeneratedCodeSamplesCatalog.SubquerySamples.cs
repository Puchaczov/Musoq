namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSubquerySamples()
    {
        return
        [
            Basic(
                "Q138_CorrelatedInSubquery",
                "Subquery",
                @"SELECT a.City
              FROM #A.entities() a
              WHERE a.City IN (
                  SELECT b.City
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
              )"),
            Basic(
                "Q139_CorrelatedNotExistsSubquery",
                "Subquery",
                @"SELECT a.City
              FROM #A.entities() a
              WHERE NOT EXISTS (
                  SELECT b.City
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
              )"),
            Basic(
                "Q140_CorrelatedScalarAggregateSubquery",
                "Subquery",
                @"SELECT a.City,
                     (
                         SELECT Sum(b.Population)
                         FROM #B.entities() b
                         WHERE b.Country = a.Country
                     ) AS CountryPopulation
              FROM #A.entities() a"),
            Basic(
                "Q141_ScalarSubqueryJoinOn",
                "Subquery",
                @"SELECT a.City, b.City
              FROM #A.entities() a
              INNER JOIN #B.entities() b ON b.City = (
                  SELECT c.City
                  FROM #C.entities() c
                  WHERE c.Country = a.Country
              )"),
            Basic(
                "Q142_CorrelatedAllSubquery",
                "Subquery",
                @"SELECT a.City
              FROM #A.entities() a
              WHERE a.Population > ALL (
                  SELECT b.Population
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
              )"),
            Basic(
                "Q143_CorrelatedApplyDerivedTable",
                "Subquery",
                @"SELECT a.City, d.City
              FROM #A.entities() a
              CROSS APPLY (
                  SELECT b.City, b.Country
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
              ) d"),
            Basic(
                "Q144_CorrelatedCompositeValueTypeSubquery",
                "Subquery",
                @"SELECT a.City
              FROM #A.entities() a
              WHERE EXISTS (
                  SELECT b.City
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
                    AND b.Population = a.Population
              )"),
            Basic(
                "Q145_CorrelatedApplySelectiveDerivedTable",
                "Subquery",
                @"SELECT a.City, d.City
              FROM #A.entities() a
              CROSS APPLY (
                  SELECT b.City, b.Country
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
                    AND b.City = a.City
              ) d")
        ];
    }
}
