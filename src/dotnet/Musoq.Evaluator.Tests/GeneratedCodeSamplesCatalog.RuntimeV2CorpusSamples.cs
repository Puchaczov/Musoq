namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateRuntimeV2CorpusSamples()
    {
        return
        [
            RuntimeV2Regression(
                "Q100_RuntimeV2CseNoDuplicateRegression",
                @"SELECT Value * 2, Name
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100"),
            RuntimeV2Regression(
                "Q101_RuntimeV2WindowRunningSum",
                @"SELECT Name, Department,
                     Sum(ToDecimal(Salary)) over (partition by Department order by Salary) as RunningSalary
              FROM #test.entities()"),
            RuntimeV2Regression(
                "Q102_RuntimeV2WindowQualifyRank",
                @"SELECT Name, Department, Salary,
                     Rank() over (partition by Department order by Salary desc) as SalaryRank
              FROM #test.entities()
              QUALIFY Rank() over (partition by Department order by Salary desc) <= 3"),
            RuntimeV2Regression(
                "Q103_RuntimeV2SkipTakeNoOrder",
                @"SELECT FirstName, LastName, Email
              FROM #test.entities()
              SKIP 100 TAKE 100"),
            RuntimeV2Regression(
                "Q104_RuntimeV2StringFilter",
                @"SELECT FirstName, LastName, Email
              FROM #test.entities()
              WHERE Contains(Email, 'gmail') AND StartsWith(FirstName, 'A')"),
            RuntimeV2Regression(
                "Q105_RuntimeV2DeterministicMethodCse",
                @"SELECT ExpensiveCompute(Value) as Computed,
                     ExpensiveCompute(Value) + 10 as PlusTen,
                     CASE WHEN ExpensiveCompute(Value) > 300 THEN 'High' ELSE 'Low' END as Bucket
              FROM #test.entities()
              WHERE ExpensiveCompute(Value) > 50"),
            RuntimeV2RegressionWithOptions(
                "Q105_RuntimeV2DeterministicMethodCseDisabled",
                @"SELECT ExpensiveCompute(Value) as Computed,
                     ExpensiveCompute(Value) + 10 as PlusTen,
                     CASE WHEN ExpensiveCompute(Value) > 300 THEN 'High' ELSE 'Low' END as Bucket
              FROM #test.entities()
              WHERE ExpensiveCompute(Value) > 50",
                new CompilationOptions(useCommonSubexpressionElimination: false)),
            RuntimeV2Regression(
                "Q106_RuntimeV2ParallelFilterProject",
                @"SELECT Id, Name, Value, Category, HeavyComputation(Value) as Heavy
              FROM #test.entities()
              WHERE Value > 100"),
            RuntimeV2Regression(
                "Q107_RuntimeV2LexerManyColumns",
                @"SELECT Id as C01,
                     Name as C02,
                     FirstName as C03,
                     LastName as C04,
                     Email as C05,
                     Value as C06,
                     Category as C07,
                     Department as C08,
                     Salary as C09,
                     Value + 1 as C10,
                     Value + 2 as C11,
                     Value + 3 as C12,
                     Value + 4 as C13,
                     Value + 5 as C14,
                     Value + 6 as C15,
                     Value + 7 as C16,
                     Value + 8 as C17,
                     Value + 9 as C18,
                     Value + 10 as C19,
                     Value + 11 as C20,
                     Salary + 1 as C21,
                     Salary + 2 as C22,
                     Salary + 3 as C23,
                     Salary + 4 as C24,
                     Salary + 5 as C25,
                     Salary + 6 as C26,
                     Salary + 7 as C27,
                     Salary + 8 as C28,
                     Salary + 9 as C29,
                     Salary + 10 as C30,
                     Name + '-' + Category as C31,
                     FirstName + ' ' + LastName as C32,
                     Department + ':' + Category as C33,
                     Email + ':' + Name as C34,
                     Value * 2 as C35,
                     Value * 3 as C36,
                     Value * 4 as C37,
                     Salary * 2 as C38,
                     Salary * 3 as C39,
                     Salary * 4 as C40,
                     Value - Salary as C41,
                     Salary - Value as C42,
                     Value + Salary as C43,
                     Value % 10 as C44,
                     Salary % 10 as C45,
                     Value > 100 as C46,
                     Salary > 1000 as C47,
                     (Category = 'A' or Category = 'B' or Category = 'C') as C48,
                     CASE WHEN Value > 100 THEN 'High' ELSE 'Low' END as C49,
                     CASE WHEN Salary > 1000 THEN 'Large' ELSE 'Small' END as C50
              FROM #test.entities()"),
            RuntimeV2RegressionWithOptions(
                "Q108_RuntimeV2DecimalConversion",
                @"SELECT Id, TryConvertToDecimalComparison(Amount) as AmountDecimal
              FROM #test.entities()
              WHERE TryConvertToDecimalComparison(Amount) > 100.50d",
                new CompilationOptions(
                    useCommonSubexpressionElimination: true,
                    usePrimitiveTypeValidation: false)),
            RuntimeV2Regression(
                "Q109_RuntimeV2CompositeRegressionCanary",
                @"SELECT Name,
                     Department,
                     Salary,
                     ExpensiveCompute(Value) as Computed,
                     Sum(ToDecimal(Salary)) over (partition by Department order by Salary) as RunningSalary,
                     Rank() over (partition by Department order by Salary desc) as SalaryRank
              FROM #test.entities()
              WHERE Contains(Email, 'gmail')
                    AND StartsWith(FirstName, 'A')
                    AND ExpensiveCompute(Value) > 50
              ORDER BY Department, Salary desc, Computed desc
              SKIP 10 TAKE 20"),
            RuntimeV2Regression(
                "Q111_RuntimeV2WindowBenchmarkRowNumberNoPartition",
                @"SELECT Name,
                     RowNumber() over (order by Salary desc) as rn
              FROM #test.entities()"),
            RuntimeV2Regression(
                "Q112_RuntimeV2WindowBenchmarkRowNumberPartitioned",
                @"SELECT Name,
                     Department,
                     RowNumber() over (partition by Department order by Salary desc) as rn
              FROM #test.entities()"),
            RuntimeV2Regression(
                "Q113_RuntimeV2WindowBenchmarkRankPartitioned",
                @"SELECT Name,
                     Department,
                     Rank() over (partition by Department order by Salary desc) as rn
              FROM #test.entities()"),
            RuntimeV2Regression(
                "Q114_RuntimeV2WindowBenchmarkDenseRankPartitioned",
                @"SELECT Name,
                     Department,
                     DenseRank() over (partition by Department order by Salary desc) as rn
              FROM #test.entities()"),
            RuntimeV2Regression(
                "Q115_RuntimeV2WindowBenchmarkCountWholePartition",
                @"SELECT Name,
                     Department,
                     Count(Name) over (partition by Department) as cnt
              FROM #test.entities()"),
            RuntimeV2Regression(
                "Q116_RuntimeV2ParallelTableAddBenchmark",
                @"SELECT Id, Name, Value, Category, HeavyComputation(Value) as Heavy
              FROM #test.entities()
              WHERE Value > 100"),
            RuntimeV2BenchmarkMaterialized(
                "Q176_BenchmarkCseNoDuplicateMaterialized",
                @"SELECT Value * 2, Name
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100"),
            RuntimeV2BenchmarkMaterialized(
                "Q177_BenchmarkCseCaseNoDuplicateMaterialized",
                @"SELECT Name,
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()"),
            RuntimeV2BenchmarkMaterialized(
                "Q178_BenchmarkParallelTableAddMaterialized",
                @"SELECT Id, Name, Value, Category, HeavyComputation(Value) as Heavy
              FROM #test.entities()
              WHERE Value > 100"),
            RuntimeV2BenchmarkMaterializedWithOptions(
                "Q179_BenchmarkOptimizedHeavyMixedMaterialized",
                @"SELECT
                Id,
                Value,
                Value * 2,
                Value + 100,
                ExpensiveCompute(Value),
                ExpensiveCompute(Value) * 2,
                ExpensiveCompute(Value) + Value,
                Name,
                StringTransform(Name),
                Category,
                CASE
                    WHEN Value > 500 AND ExpensiveCompute(Value) > 1000 THEN 'VeryHigh'
                    WHEN Value > 200 AND ExpensiveCompute(Value) > 500 THEN 'High'
                    WHEN Value > 100 THEN 'Medium'
                    ELSE 'Low'
                END as Classification,
                Value + ExpensiveCompute(Value) + Value * 2
            FROM #test.entities()
            WHERE Value > 50
              AND ExpensiveCompute(Value) > 0
              AND Value < 900",
                new CompilationOptions(ParallelizationMode.Full)),
            RuntimeV2BenchmarkMaterializedWithOptions(
                "Q180_BenchmarkOptimizedMixedColumnMethodMaterialized",
                @"SELECT
                Value,
                ExpensiveCompute(Value),
                Value + ExpensiveCompute(Value),
                Value * ExpensiveCompute(Value),
                Name,
                StringTransform(Name),
                Name + '_' + StringTransform(Name)
            FROM #test.entities()
            WHERE Value > 100
              AND ExpensiveCompute(Value) > 50
              AND Name IS NOT NULL",
                new CompilationOptions(ParallelizationMode.Full)),
            RuntimeV2BenchmarkMaterialized(
                "Q181_BenchmarkCompilationSimpleMaterialized",
                @"SELECT City, Country, Population
              FROM #test.entities()
              WHERE Population > 500000"),
            RuntimeV2BenchmarkMaterialized(
                "Q182_BenchmarkCompilationComplexMaterialized",
                @"SELECT City, Country, Population, City + ' (' + Country + ')' as CityCountry
              FROM #test.entities()
              WHERE Population > 500000
              GROUP BY City, Country, Population
              HAVING Count(City) > 0
              ORDER BY Population desc")
        ];
    }
}
