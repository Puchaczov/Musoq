using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class InvalidQueryOutputTypeTests : BasicEntityTestBase
{
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Array");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Dictionary");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Other");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Array");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachineWithValidation(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3027_InvalidExpressionType, DiagnosticPhase.Bind, "Self");
    }


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

}
