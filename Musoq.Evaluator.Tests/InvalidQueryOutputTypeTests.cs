using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class InvalidQueryOutputTypeTests : BasicEntityTestBase
{
    /// <summary>
    /// Creates and runs a virtual machine with primitive type validation enabled.
    /// </summary>
    private CompiledQuery CreateAndRunVirtualMachineWithValidation<T>(
        string script,
        IDictionary<string, IEnumerable<T>> sources)
        where T : BasicEntity
    {
        return CreateAndRunVirtualMachine(script, sources, ValidationEnabledCompilationOptions);
    }
    
    [TestMethod]
    public void WhenSelectComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange - Self returns BasicEntity which is a complex type
        var query = "select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenSelectArrayType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange - Array returns int[] which is not allowed
        var query = "select Array from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenSelectDictionaryType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange - Dictionary returns Dictionary<string,string> which is a complex type
        var query = "select Dictionary from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenSelectOtherComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange - Other returns BasicEntity which is a complex type
        var query = "select Other from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenSelectPrimitiveTypes_ShouldSucceed()
    {
        // Arrange - Name, City, Population are all primitive types
        var query = "select Name, City, Population, Money, Time from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        // Act
        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        // Assert
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenUnionWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange
        var query = @"select Self from #A.Entities() union (Self) select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenUnionAllWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange
        var query = @"select Self from #A.Entities() union all (Self) select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenExceptWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange
        var query = @"select Self from #A.Entities() except (Self) select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenIntersectWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange
        var query = @"select Self from #A.Entities() intersect (Self) select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenCteWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange
        var query = @"
with cte as (
    select Self from #A.Entities()
)
select Self from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenCteWithArrayType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange
        var query = @"
with cte as (
    select Array from #A.Entities()
)
select Array from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenCteWithPrimitiveTypes_ShouldSucceed()
    {
        // Arrange
        var query = @"
with cte as (
    select Name, City from #A.Entities()
)
select Name, City from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        // Act
        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        // Assert
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenSelectNullableType_ShouldSucceed()
    {
        // Arrange - NullableValue is int? which should be allowed
        var query = "select NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act
        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        // Assert
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenSelectMixedTypesWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange - Name is valid but Self is not
        var query = "select Name, Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenNestedCteWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        // Arrange
        var query = @"
with cte1 as (
    select Self from #A.Entities()
),
cte2 as (
    select Self from cte1
)
select Self from cte2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        // Act & Assert
        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }
    
    #region Primitive Type Validation - All Supported Types
    
    [TestMethod]
    public void WhenSelectStringType_ShouldSucceed()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test", table[0][0]);
    }

    [TestMethod]
    public void WhenSelectDecimalType_ShouldSucceed()
    {
        var query = "select Money from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Jan", 100.5m)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100.5m, table[0][0]);
    }

    [TestMethod]
    public void WhenSelectDateTimeType_ShouldSucceed()
    {
        var query = "select Time from #A.Entities()";
        var now = DateTime.Now;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity(now)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(now, table[0][0]);
    }

    [TestMethod]
    public void WhenSelectIntegerType_ShouldSucceed()
    {
        var query = "select Id from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test") { Id = 42 }] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
    }

    [TestMethod]
    public void WhenSelectNullLiteral_ShouldSucceed()
    {
        var query = "select null from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void WhenSelectIntegerLiteral_ShouldSucceed()
    {
        var query = "select 42 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenSelectStringLiteral_ShouldSucceed()
    {
        var query = "select 'hello world' from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello world", table[0][0]);
    }

    [TestMethod]
    public void WhenSelectBooleanLiteral_ShouldSucceed()
    {
        var query = "select true, false from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue((bool?)table[0][0]);
        Assert.IsFalse((bool?)table[0][1]);
    }

    [TestMethod]
    public void WhenSelectDecimalLiteral_ShouldSucceed()
    {
        var query = "select 123.456d from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region WHERE Clause Validation

    [TestMethod]
    public void WhenWhereWithPrimitiveComparison_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() where Population > 500";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenWhereWithStringComparison_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() where Name = 'test'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenWhereWithNullCheck_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() where Name is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenWhereWithMultipleConditions_ShouldSucceed()
    {
        var query = "select City from #A.Entities() where Population > 100 and City is not null";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region GROUP BY Clause Validation

    [TestMethod]
    public void WhenGroupByWithPrimitiveColumn_ShouldSucceed()
    {
        var query = "select City, Count(City) from #A.Entities() group by City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000), new BasicEntity("city1", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][1]);
    }

    [TestMethod]
    public void WhenGroupByWithMultiplePrimitiveColumns_ShouldSucceed()
    {
        var query = "select City, Country, Count(Name) from #A.Entities() group by City, Country";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenGroupByWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        var query = "select Self from #A.Entities() group by Self";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenGroupByWithArrayType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        var query = "select Array from #A.Entities() group by Array";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    #endregion

    #region HAVING Clause Validation

    [TestMethod]
    public void WhenHavingWithPrimitiveAggregation_ShouldSucceed()
    {
        var query = "select City, Count(City) from #A.Entities() group by City having Count(City) > 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenHavingWithSumAggregation_ShouldSucceed()
    {
        var query = "select City, Sum(Population) from #A.Entities() group by City having Sum(Population) > 500";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region ORDER BY Clause Validation

    [TestMethod]
    public void WhenOrderByWithPrimitiveColumn_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() order by Name asc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("b"), new BasicEntity("a")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        var names = table.Select(row => (string)row[0]).ToList();
        CollectionAssert.Contains(names, "a");
        CollectionAssert.Contains(names, "b");
    }

    [TestMethod]
    public void WhenOrderByWithNumericColumn_ShouldSucceed()
    {
        var query = "select Name, Population from #A.Entities() order by Population desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("country1", 100), new BasicEntity("country2", 200)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        var populations = table.Select(row => (decimal)row[1]).ToList();
        CollectionAssert.Contains(populations, 100m);
        CollectionAssert.Contains(populations, 200m);
    }

    [TestMethod]
    public void WhenOrderByWithMultipleColumns_ShouldSucceed()
    {
        var query = "select City, Country from #A.Entities() order by City asc, Country desc";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenOrderByWithComplexType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        var query = "select Name from #A.Entities() order by Self";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    [TestMethod]
    public void WhenOrderByWithArrayType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        var query = "select Name from #A.Entities() order by Array";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        Assert.Throws<InvalidQueryExpressionTypeException>(() => CreateAndRunVirtualMachineWithValidation(query, sources));
    }

    #endregion

    #region SKIP/TAKE Clause Validation

    [TestMethod]
    public void WhenSkipWithIntegerLiteral_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() skip 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        var name = (string)table[0][0];
        Assert.IsTrue(name == "a" || name == "b", "Result should be one of the input values");
    }

    [TestMethod]
    public void WhenTakeWithIntegerLiteral_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        var name = (string)table[0][0];
        Assert.IsTrue(name == "a" || name == "b", "Result should be one of the input values");
    }

    [TestMethod]
    public void WhenSkipAndTakeWithIntegerLiterals_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() skip 1 take 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b"), new BasicEntity("c")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        var name = (string)table[0][0];
        Assert.IsTrue(name == "a" || name == "b" || name == "c", "Result should be one of the input values");
    }

    #endregion

    #region Expression Validation

    [TestMethod]
    public void WhenArithmeticExpressionWithPrimitives_ShouldSucceed()
    {
        var query = "select Population + 100, Population * 2 from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1100m, table[0][0]);
        Assert.AreEqual(2000m, table[0][1]);
    }

    [TestMethod]
    public void WhenStringConcatenation_ShouldSucceed()
    {
        var query = "select City + ' - ' + Country from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("city1 - country1", table[0][0]);
    }

    [TestMethod]
    public void WhenCaseWhenWithPrimitiveResults_ShouldSucceed()
    {
        var query = "select case when Population > 500 then 'Large' else 'Small' end from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Large", table[0][0]);
    }

    [TestMethod]
    public void WhenCoalesceWithPrimitives_ShouldSucceed()
    {
        var query = "select Coalesce(NullableValue, 0) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Aggregation Function Validation

    [TestMethod]
    public void WhenCountAggregation_ShouldSucceed()
    {
        var query = "select Count(Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]);
    }

    [TestMethod]
    public void WhenSumAggregation_ShouldSucceed()
    {
        var query = "select Sum(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 100), new BasicEntity("city2", "country2", 200)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(300m, table[0][0]);
    }

    [TestMethod]
    public void WhenAvgAggregation_ShouldSucceed()
    {
        var query = "select Avg(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 100), new BasicEntity("city2", "country2", 200)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(150m, table[0][0]);
    }

    [TestMethod]
    public void WhenMinMaxAggregation_ShouldSucceed()
    {
        var query = "select Min(Population), Max(Population) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 100), new BasicEntity("city2", "country2", 200)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100m, table[0][0]);
        Assert.AreEqual(200m, table[0][1]);
    }

    #endregion

    #region SET Operators with Primitive Types

    [TestMethod]
    public void WhenUnionWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenUnionAllWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void WhenExceptWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() except (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] },
            { "#B", [new BasicEntity("b")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void WhenIntersectWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"select Name from #A.Entities() intersect (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("a"), new BasicEntity("b")] },
            { "#B", [new BasicEntity("b"), new BasicEntity("c")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0][0]);
    }

    #endregion

    #region Complex CTE Scenarios with Primitive Types

    [TestMethod]
    public void WhenCteWithAggregation_ShouldSucceed()
    {
        var query = @"
with totals as (
    select City, Sum(Population) as TotalPop from #A.Entities() group by City
)
select City, TotalPop from totals";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000), new BasicEntity("city1", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1500m, table[0][1]);
    }

    [TestMethod]
    public void WhenMultipleCteWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"
with cte1 as (
    select Name, City from #A.Entities()
),
cte2 as (
    select Name, City from cte1
)
select Name, City from cte2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Join Scenarios with Primitive Types

    [TestMethod]
    public void WhenInnerJoinWithPrimitiveColumns_ShouldSucceed()
    {
        var query = @"select a.Name, b.City from #A.Entities() a inner join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] },
            { "#B", [new BasicEntity("city1", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenLeftJoinWithPrimitiveColumns_ShouldSucceed()
    {
        var query = @"select a.Name, b.City from #A.Entities() a left outer join #B.Entities() b on a.City = b.City";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] },
            { "#B", [new BasicEntity("city2", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region Subquery Scenarios

    // Note: Subqueries in WHERE are not supported in Musoq.
    // This region is reserved for future subquery support tests.

    #endregion

    #region Error Message Validation

    [TestMethod]
    public void WhenSelectComplexType_ExceptionMessageShouldContainColumnName()
    {
        var query = "select Self from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var exception = Assert.Throws<InvalidQueryExpressionTypeException>(() => 
            CreateAndRunVirtualMachineWithValidation(query, sources));
        
        Assert.Contains("Self", exception.Message, "Exception message should contain the column name 'Self'");
        Assert.Contains("SELECT", exception.Message, "Exception message should mention SELECT clause");
    }

    [TestMethod]
    public void WhenOrderByComplexType_ExceptionMessageShouldMentionOrderBy()
    {
        var query = "select Name from #A.Entities() order by Self";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var exception = Assert.Throws<InvalidQueryExpressionTypeException>(() => 
            CreateAndRunVirtualMachineWithValidation(query, sources));
        
        Assert.Contains("ORDER BY", exception.Message, "Exception message should mention ORDER BY clause");
    }

    [TestMethod]
    public void WhenGroupByComplexType_ExceptionMessageShouldMentionGroupBy()
    {
        var query = "select Self from #A.Entities() group by Self";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var exception = Assert.Throws<InvalidQueryExpressionTypeException>(() => 
            CreateAndRunVirtualMachineWithValidation(query, sources));
        
        Assert.IsTrue(exception.Message.Contains("GROUP BY") || exception.Message.Contains("SELECT"), 
            "Exception message should mention GROUP BY or SELECT clause");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void WhenSelectDistinctWithPrimitiveTypes_ShouldSucceed()
    {
        var query = "select distinct City from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000), new BasicEntity("city1", "country2", 500)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenSelectWithAlias_ShouldSucceed()
    {
        var query = "select Name as PersonName, City as Location from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenEmptyResult_ShouldSucceed()
    {
        var query = "select Name from #A.Entities() where 1 = 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(0, table.Count);
    }

    #endregion
}
