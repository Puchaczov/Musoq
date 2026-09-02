using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PostfixCastMethodCallEvaluatorTests : BasicEntityTestBase
{

    [TestMethod]
    public void MethodCallPostfixCast_CSharpAliases_ShouldCoverPrimitiveTargets()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                select
                    DoNothing(true)::bool as BoolValue,
                    DoNothing(255ub)::byte as ByteValue,
                    DoNothing(-12b)::sbyte as SByteValue,
                    DoNothing(-1234s)::short as ShortValue,
                    DoNothing(1234us)::ushort as UShortValue,
                    DoNothing(-123456)::int as IntValue,
                    DoNothing(123456ui)::uint as UIntValue,
                    DoNothing(-1234567890123l)::long as LongValue,
                    DoNothing(1234567890123ul)::ulong as ULongValue,
                    ToFloat(1)::float as FloatValue,
                    ToDouble(2)::double as DoubleValue,
                    DoNothing(123.45d)::decimal as DecimalValue,
                    ToChar('Z')::char as CharValue,
                    DoNothing(42)::string as StringValue
                from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500))).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("BoolValue", typeof(bool?)),
            ("ByteValue", typeof(byte?)),
            ("SByteValue", typeof(sbyte?)),
            ("ShortValue", typeof(short?)),
            ("UShortValue", typeof(ushort?)),
            ("IntValue", typeof(int?)),
            ("UIntValue", typeof(uint?)),
            ("LongValue", typeof(long?)),
            ("ULongValue", typeof(ulong?)),
            ("FloatValue", typeof(float?)),
            ("DoubleValue", typeof(double?)),
            ("DecimalValue", typeof(decimal?)),
            ("CharValue", typeof(char?)),
            ("StringValue", typeof(string)));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [
                true,
                (byte)255,
                (sbyte)-12,
                (short)-1234,
                (ushort)1234,
                -123456,
                (uint)123456,
                -1234567890123L,
                1234567890123UL,
                1f,
                2d,
                123.45m,
                'Z',
                "42"
            ]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_CanonicalTargets_ShouldConvertMethodResults()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                select
                    GetOne()::Int32 as FromDecimal,
                    population2.GetPopulation()::Int64 as FromQualifiedDecimal,
                    GetTwo(GetOne(), 'value')::String as FromNestedMethod,
                    GetOne()::String as InvariantText,
                    DoNothing('2024-06-15T13:45:30')::DateTime as DateTimeValue,
                    DoNothing('2024-06-15T13:45:30+02:00')::DateTimeOffset as DateTimeOffsetValue,
                    DoNothing('01:02:03')::TimeSpan as TimeSpanValue,
                    DoNothing('12345678-1234-1234-1234-123456789012')::Guid as GuidValue
                from #A.entities() population2",
            CreateSingleSource(new BasicEntity("POLAND", 500))).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("FromDecimal", typeof(int?)),
            ("FromQualifiedDecimal", typeof(long?)),
            ("FromNestedMethod", typeof(string)),
            ("InvariantText", typeof(string)),
            ("DateTimeValue", typeof(DateTime?)),
            ("DateTimeOffsetValue", typeof(DateTimeOffset?)),
            ("TimeSpanValue", typeof(TimeSpan?)),
            ("GuidValue", typeof(Guid?)));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [
                1,
                500L,
                "2",
                "1",
                DateTime.Parse("2024-06-15T13:45:30", CultureInfo.InvariantCulture),
                DateTimeOffset.Parse("2024-06-15T13:45:30+02:00", CultureInfo.InvariantCulture),
                TimeSpan.Parse("01:02:03", CultureInfo.InvariantCulture),
                Guid.Parse("12345678-1234-1234-1234-123456789012")
            ]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_NestedAndParenthesized_ShouldExecuteLeftToRight()
    {
        var table = CreateAndRunVirtualMachine(
            "select GetOne()::string::int as ChainedValue, (GetOne())::int + 1 as ParenthesizedValue from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500))).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ChainedValue", typeof(int?)),
            ("ParenthesizedValue", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, 2]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_NullResults_ShouldRemainNull()
    {
        var table = CreateAndRunVirtualMachine(
            "select GetCountry()::string as TextValue, GetCountry()::int as IntValue, NullableMethod(null)::int as NullableValue from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500) { Country = null })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("TextValue", typeof(string)),
            ("IntValue", typeof(int?)),
            ("NullableValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null, null, null });
    }

    [TestMethod]
    public void MethodCallPostfixCast_StringResult_ShouldUseInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var table = CreateAndRunVirtualMachine(
                "select DoNothing(1234.5d)::string as TextValue from #A.entities()",
                CreateSingleSource(new BasicEntity("POLAND", 500))).Run();

            TableMaterializationTestHelper.AssertColumns(table, ("TextValue", typeof(string)));
            TableMaterializationTestHelper.AssertRowsUnordered(table, ["1234.5"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestMethod]
    public void MethodCallPostfixCast_InvalidText_ShouldThrowFormatException()
    {
        var vm = CreateAndRunVirtualMachine(
            "select TestMethodWithInjectEntityAndParameter('not_a_number')::int from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500)));

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
    }

    [TestMethod]
    public void MethodCallPostfixCast_Overflow_ShouldThrowOverflowException()
    {
        var vm = CreateAndRunVirtualMachine(
            "select TestMethodWithInjectEntityAndParameter('256')::byte from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500)));

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
    }

    [TestMethod]
    public void MethodCallPostfixCast_IncompatibleArrayResult_ShouldThrowInvalidCastException()
    {
        var vm = CreateAndRunVirtualMachine(
            "select JustReturnArrayOfString()::int from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500)));

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
    }

    [TestMethod]
    public void MethodCallPostfixCast_IncompatibleEntityResult_ShouldThrowInvalidCastException()
    {
        var vm = CreateAndRunVirtualMachine(
            "select NothingToDo(Self)::Guid from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500)));

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
    }

    [TestMethod]
    public void MethodCallPostfixCast_ThrowingMethod_ShouldPreserveOriginalException()
    {
        var vm = CreateAndRunVirtualMachine(
            "select ThrowException()::int from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500)));

        var exception = Assert.Throws<QueryExecutionException>(() => _ = vm.Run().Count);
        AssertRuntimeError(exception, DiagnosticCode.MQ9002_InternalExecutionError);
        Assert.IsInstanceOfType<MethodCallThrownException>(exception.InnerException);
    }

    [TestMethod]
    public void MethodCallPostfixCast_SqlAlias_ShouldRemainUnsupported()
    {
        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            "select GetOne()::INTEGER from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500))));

        AssertSingleError(
            exception,
            DiagnosticCode.MQ3090_UnsupportedCastTarget,
            DiagnosticPhase.Bind,
            "CLR type names and C# aliases only");
    }

    [TestMethod]
    public void MethodCallPostfixCast_SelectWhereOrderAndCase_ShouldCompose()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Country = "POLAND", City = "WARSAW", Population = 500 },
                new BasicEntity { Country = "POLAND", City = "KRAKOW", Population = 200 },
                new BasicEntity { Country = "GERMANY", City = "BERLIN", Population = 50 }
            ]
        };

        var table = CreateAndRunVirtualMachine(
            @"
                select
                    GetPopulation()::int as Population,
                    case when GetPopulation()::int > 300 then GetCountry()::string else 'small' end as Size
                from #A.entities()
                where GetPopulation()::int > 100
                order by GetPopulation()::int desc",
            sources).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Population", typeof(int?)),
            ("Size", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [500, "POLAND"],
            [200, "small"]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_GroupByHavingAndAggregateInput_ShouldCompose()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Country = "POLAND", City = "WARSAW", Population = 500 },
                new BasicEntity { Country = "POLAND", City = "KRAKOW", Population = 200 },
                new BasicEntity { Country = "GERMANY", City = "BERLIN", Population = 50 }
            ]
        };

        var grouped = CreateAndRunVirtualMachine(
            @"
                select GetCountry()::string as Country, Sum(GetPopulation()::decimal) as Total
                from #A.entities()
                group by GetCountry()::string
                having GetCountry()::string = 'POLAND'
                order by GetCountry()::string",
            sources).Run();

        var aggregate = CreateAndRunVirtualMachine(
            "select Sum(GetPopulation()::decimal)::int as Total from #A.entities()",
            sources).Run();

        TableMaterializationTestHelper.AssertColumns(
            grouped,
            ("Country", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(grouped, ["POLAND", 700m]);

        TableMaterializationTestHelper.AssertColumns(aggregate, ("Total", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(aggregate, [750]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_AggregateResult_ShouldExposeGeneratedShape()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select Sum(Population)::int as Total from #A.entities()",
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(CreateSingleSource(new BasicEntity("POLAND", 500))),
            LoggerResolver);

        Assert.IsNotNull(inspection.ExecutionPlan);
    }

    [TestMethod]
    public void MethodCallPostfixCast_AsNestedMethodArgument_ShouldPreserveOverloadBinding()
    {
        var table = CreateAndRunVirtualMachine(
            "select Inc(GetOne()::int)::string as Value from #A.entities()",
            CreateSingleSource(new BasicEntity("POLAND", 500))).Run();

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["2"]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_InsideCte_ShouldRemainExecutableInOuterQuery()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                with converted as (
                    select GetPopulation()::int as Population, GetCountry()::string as Country
                    from #A.entities()
                )
                select Population::string as PopulationText, Country
                from converted
                where Population > 100
                order by Population",
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
            ["#A"] =
            [
                    new BasicEntity { Country = "POLAND", City = "WARSAW", Population = 500 },
                    new BasicEntity { Country = "POLAND", City = "KRAKOW", Population = 50 }
                ]
            }).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("PopulationText", typeof(string)),
            ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["500", "POLAND"]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_WithCrossApply_ShouldPreserveQualifiedMethodExecution()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                select r.GetPopulation()::int as Population, b::string as Value
                from #A.entities() r
                cross apply r.MethodArrayOfStrings(r.GetCountry()::string, r.GetCity()::string) b",
            CreateSingleSource(new BasicEntity { Country = "POLAND", City = "WARSAW", Population = 500 })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Population", typeof(int?)),
            ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [500, "POLAND"],
            [500, "WARSAW"]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_AggregateResults_ShouldPreserveTypedExecution()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Country = "POLAND", City = "WARSAW", Population = 500 },
                new BasicEntity { Country = "POLAND", City = "KRAKOW", Population = 200 },
                new BasicEntity { Country = "GERMANY", City = "BERLIN", Population = 50 }
            ]
        };

        var count = CreateAndRunVirtualMachine(
            "select Count(1)::int as CountValue from #A.entities()", sources).Run();
        var customCount = CreateAndRunVirtualMachine(
            "select CustomRowCount()::int as CountValue from #A.entities()", sources).Run();
        var sumText = CreateAndRunVirtualMachine(
            "select Sum(GetPopulation()::decimal)::string as TotalText from #A.entities()", sources).Run();

        TableMaterializationTestHelper.AssertColumns(count, ("CountValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(count, [3]);
        TableMaterializationTestHelper.AssertColumns(customCount, ("CountValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(customCount, [3]);
        TableMaterializationTestHelper.AssertColumns(sumText, ("TotalText", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(sumText, ["750"]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_WindowResults_ShouldPreserveValuesAndOrdering()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                select Name,
                       RunningProduct(ToDecimal(Population)) over (order by Name)::string as ProductText
                from #A.entities()
                order by RunningProduct(ToDecimal(Population)) over (order by Name)::string desc",
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] =
                [
                    new BasicEntity("Charlie") { Population = 4 },
                    new BasicEntity("Alice") { Population = 2 },
                    new BasicEntity("Bob") { Population = 3 }
                ]
            }).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("ProductText", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Bob", "6"],
            ["Charlie", "24"],
            ["Alice", "2"]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_WindowResultsInQualify_ShouldFilterByConvertedValue()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                select Name, RowNumber() over (order by Name)::int as RowNumber
                from #A.entities()
                qualify RowNumber() over (order by Name)::int <= 2
                order by Name",
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] =
                [
                    new BasicEntity("Charlie"),
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RowNumber", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 1],
            ["Bob", 2]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_InjectedDefaultAndGenericArguments_ShouldPreserveBinding()
    {
        var table = CreateAndRunVirtualMachine(
            @"
                select
                    GetCountryOrDefault('fallback')::string as Country,
                    GetCountryOrDefaultGeneric('generic fallback')::string as GenericCountry
                from #A.entities()",
            CreateSingleSource(new BasicEntity { Country = null, City = "WARSAW", Population = 500 })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("GenericCountry", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["fallback", "generic fallback"]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_RepeatedDeterministicCalls_ShouldMatchWithAndWithoutCse()
    {
        const string query =
            "select GetPopulation()::int as FirstValue, GetPopulation()::int as SecondValue from #A.entities()";
        var sources = CreateSingleSource(new BasicEntity { Country = "POLAND", Population = 500 });

        var withCse = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            new CompilationOptions(useCommonSubexpressionElimination: true)).Run();
        var withoutCse = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            new CompilationOptions(useCommonSubexpressionElimination: false)).Run();

        TableMaterializationTestHelper.AssertColumns(
            withCse,
            ("FirstValue", typeof(int?)),
            ("SecondValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(withCse, [500, 500]);
        TableMaterializationTestHelper.AssertRowsUnordered(withoutCse, [500, 500]);
    }

    [TestMethod]
    public void MethodCallPostfixCast_NonDeterministicCalls_ShouldRemainDistinct()
    {
        var table = CreateAndRunVirtualMachine(
            "select NextValue()::int as FirstValue, NextValue()::int as SecondValue from #A.entities()",
            CreateSingleSource(new BasicEntity { Country = "POLAND", Population = 500 })).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("FirstValue", typeof(int?)),
            ("SecondValue", typeof(int?)));
        Assert.AreEqual(1, table.Count);
        var first = Convert.ToInt32(table[0][0]);
        var second = Convert.ToInt32(table[0][1]);
        Assert.AreEqual(first + 1, second);
    }

    [TestMethod]
    public void MethodCallPostfixCast_Inspection_ShouldUseDedicatedIrAndDirectHelpers()
    {
        var inspection = InstanceCreator.CompileForInspection(
            @"
                select
                    GetOne()::int as IntAlias,
                    GetOne()::Int32 as IntClr,
                    GetOne()::float as FloatAlias,
                    GetOne()::Single as FloatClr,
                    GetCountry()::string as Country,
                    GetOne()::string as Text
                from #A.entities()",
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(CreateSingleSource(new BasicEntity { Country = "POLAND" })),
            LoggerResolver,
            new CompilationOptions(useCommonSubexpressionElimination: false));

        Assert.IsNotNull(inspection.ExecutionPlan);
        var directCasts = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan.Body)
            .OfType<ExecutionStrictCast>()
            .Where(static cast => cast.Expression is ExecutionMethodCall)
            .ToArray();

        Assert.IsGreaterThanOrEqualTo(6, directCasts.Length);
        CollectionAssert.AreEquivalent(
            new[] { "Int32", "Single", "String" },
            directCasts.Select(static cast => cast.TargetTypeName).Distinct().ToArray());

        var injectedCountryCast = directCasts.First(static cast =>
            cast.Expression is ExecutionMethodCall { Method.MethodName: "GetCountry" });
        var injectedCountryCall = (ExecutionMethodCall)injectedCountryCast.Expression;
        Assert.AreEqual("GetCountry", injectedCountryCall.Method.MethodName);
        Assert.IsNotNull(injectedCountryCall.InjectedSource);

        Assert.Contains("StrictCastRuntime.ToInt32", inspection.GeneratedCSharpCode);
        Assert.Contains("StrictCastRuntime.ToSingle", inspection.GeneratedCSharpCode);
        Assert.Contains("StrictCastRuntime.ToString", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("System.Reflection", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("Convert.ChangeType", inspection.GeneratedCSharpCode);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSingleSource(BasicEntity entity)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [entity]
        };
    }
}
