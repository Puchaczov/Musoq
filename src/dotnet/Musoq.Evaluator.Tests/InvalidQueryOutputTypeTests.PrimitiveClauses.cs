using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class InvalidQueryOutputTypeTests
{
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
        Assert.AreEqual(2L, table[0][1]);
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

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
    }

    [TestMethod]
    public void WhenGroupByWithArrayType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        var query = "select Array from #A.Entities() group by Array";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Array");
    }



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

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
    }

    [TestMethod]
    public void WhenOrderByWithArrayType_ShouldThrowInvalidQueryExpressionTypeException()
    {
        var query = "select Name from #A.Entities() order by Array";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Array");
    }



}
