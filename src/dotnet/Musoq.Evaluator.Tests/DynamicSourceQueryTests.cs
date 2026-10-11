using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Dynamic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class DynamicSourceQueryTests : DynamicQueryTestsBase
{
    private static readonly CompilationOptions ExecutionIrCompilationOptions = new(
        usePrimitiveTypeValidation: false);

    private const string KeywordCollisionQuery =
        "select Exists, ANY, Some, All, Rows, Range, Qualify, Filter, Present, Missing, Substream, [Between], [End], [Case], [Select], [From], [Take] from #dynamic.all()";

    private static readonly string[] KeywordCollisionColumns =
    [
        "Exists", "ANY", "Some", "All", "Rows", "Range", "Qualify", "Filter", "Present", "Missing",
        "Substream", "Between", "End", "Case", "Select", "From", "Take"
    ];


    [TestMethod]
    public void WithDynamicSource_DescDynamicObjectWithSimpleColumns_ShouldPass()
    {
        const string query = "desc #dynamic.all()";
        var sources =
            new List<dynamic>
            {
                CreateExpandoObject(1, "Test1"),
                CreateExpandoObject(2, "Test2")
            };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Id", table[0].ReadValue(0));
        Assert.AreEqual("System.Int32", table[0].ReadValue(2));
        Assert.AreEqual("Name", table[1].ReadValue(0));
        Assert.AreEqual("System.String", table[1].ReadValue(2));
    }

    [TestMethod]
    public void WithDynamicSource_SimpleQuery_ShouldPass()
    {
        const string query = "select Id, Name from #dynamic.all()";
        var sources =
            new List<dynamic>
            {
                CreateExpandoObject(1, "Test1"),
                CreateExpandoObject(2, "Test2")
            };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(2, table.Count, "Table should contain 2 rows");

        Assert.IsTrue(table.All(row =>
                new[] { (1, "Test1"), (2, "Test2") }.Contains(((int)row.ReadValue(0), (string)row.ReadValue(1)))),
            "Expected rows with values: (1,Test1), (2,Test2)");
    }

    [TestMethod]
    public void WithDynamicSource_WhenExecutionIrRendererIsEnabled_ShouldUseNativeMaterializerAndReturnSimpleColumns()
    {
        const string query = "select d.Id, d.Name from #dynamic.all() d";
        var sources = new List<dynamic>
        {
            CreateExpandoObject(1, "Test1"),
            CreateExpandoObject(2, "Test2")
        };

        var inspection = CreateDynamicInspection(query, sources);
        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            compilationOptions: ExecutionIrCompilationOptions);

        var table = vm.Run(TestContext.CancellationToken);




        Assert.Contains("IQueryRowMaterializer<", inspection.GeneratedCSharpCode);
        Assert.Contains("reader.Read<", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("__dynamicValue", inspection.GeneratedCSharpCode);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("EvaluationHelper.GetColumnValue", StringComparison.Ordinal));
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0].ReadValue(0));
        Assert.AreEqual("Test1", table[0].ReadValue(1));
        Assert.AreEqual(2, table[1].ReadValue(0));
        Assert.AreEqual("Test2", table[1].ReadValue(1));
    }

    [TestMethod]
    public void WithDynamicSource_WhenExecutionIrRendererIsEnabled_ShouldAdaptObjectValuedColumns()
    {
        const string query = "select d.Complex.Id, d.Complex.Name from #schema.first() d";
        using var compiled = CompileNative(query, new NativeRoot<NativePerson>(new(1, "Test1")));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        TableMaterializationTestHelper.AssertColumns(table, ("d.Complex.Id", typeof(int)), ("d.Complex.Name", typeof(string)));
        Assert.AreEqual(1, table[0].GetValue<int>(0));
        Assert.AreEqual("Test1", table[0].GetValue<string>(1));
    }

    [TestMethod]
    public void WithDynamicSource_DescDynamicObjectWithComplexColumns_ShouldPass()
    {
        const string query = "desc #dynamic.all()";
        var sources = new List<dynamic>
        {
            CreateExpandoObject(CreateExpandoObject(1, "Test1"))
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Complex", table[0].ReadValue(0));
        Assert.AreEqual(0, (int)table[0].ReadValue(1));
        Assert.AreEqual(typeof(ExpandoObject).FullName, table[0].ReadValue(2));
    }

    [TestMethod]
    public void WithDynamicSource_AccessComplexObjectProperties_ShouldPass()
    {
        const string query = "select Complex.Id, Complex.Name from #schema.first()";
        using var compiled = CompileNative(query, new NativeRoot<NativePerson>(new(1, "Test1")));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        TableMaterializationTestHelper.AssertColumns(table, ("Complex.Id", typeof(int)), ("Complex.Name", typeof(string)));
        Assert.AreEqual(1, table[0].GetValue<int>(0));
        Assert.AreEqual("Test1", table[0].GetValue<string>(1));
    }

    [TestMethod]
    public void WithDynamicSource_AccessComplexChainedObjectProperties_ShouldPass()
    {
        const string query = "select Complex.Complex.Id, Complex.Complex.Name from #schema.first()";
        using var compiled = CompileNative(query, new NativeRoot<NativeRoot<NativePerson>>(new(new(1, "Test1"))));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        TableMaterializationTestHelper.AssertColumns(table, ("Complex.Complex.Id", typeof(int)), ("Complex.Complex.Name", typeof(string)));
        Assert.AreEqual(1, table[0].GetValue<int>(0));
        Assert.AreEqual("Test1", table[0].GetValue<string>(1));
    }

    [TestMethod]
    public void WithDynamicSource_AccessArray_ShouldPass()
    {
        const string query = "select Complex.Array[0], Complex.Array[1] from #schema.first()";
        using var compiled = CompileNative(query, new NativeRoot<NativeArray<int>>(new([1, 2])));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        TableMaterializationTestHelper.AssertColumns(table, ("Complex.Array[0]", typeof(int)), ("Complex.Array[1]", typeof(int)));
        Assert.AreEqual(1, table[0].GetValue<int>(0));
        Assert.AreEqual(2, table[0].GetValue<int>(1));
    }

    [TestMethod]
    public void WithDynamicSource_AccessExpandoObjectArray_ShouldPass()
    {
        const string query = "select Complex.Array[0].Id, Complex.Array[0].Name from #schema.first()";
        using var compiled = CompileNative(query, new NativeRoot<NativeArray<NativePerson>>(new([new(1, "Test1")])));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        TableMaterializationTestHelper.AssertColumns(table, ("Complex.Array[0].Id", typeof(int)), ("Complex.Array[0].Name", typeof(string)));
        Assert.AreEqual(1, table[0].GetValue<int>(0));
        Assert.AreEqual("Test1", table[0].GetValue<string>(1));
    }


    [TestMethod]
    public void WithDynamicSource_IncrementAccessedProperty_ShouldPass()
    {
        const string query = "select Increment(Complex.Array[0].Id) as Incremented from #schema.first()";
        using var compiled = CompileNative(query, new NativeRoot<NativeArray<NativePerson>>(new([new(1, null)])));
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        Assert.HasCount(1, rows);
        TableMaterializationTestHelper.AssertColumns(table, ("Incremented", typeof(int)));
        Assert.AreEqual(2, table[0].GetValue<int>(0));
    }

    [TestMethod]
    public void WithRedundantColumn_ShouldBeTrimmed_ShouldPass()
    {
        const string query = @"
select 
    weather.Location as 'City', 
    weather.TemperatureInCelsiusDegree as 'TemperatureInCelsiusDegree', 
    location.Name as 'Location' 
from #weather.all() weather 
inner join #location.all() location on weather.Location = location.Name
";
        dynamic weather = new ExpandoObject();
        weather.Location = "London";
        weather.TemperatureInCelsiusDegree = 10;
        var weatherSource = new List<dynamic>
        {
            weather
        };
        var weatherSchema = new Dictionary<string, Type>
        {
            { "Name", typeof(decimal) },
            { "Location", typeof(string) },
            { "TemperatureInCelsiusDegree", typeof(int) }
        };

        dynamic location = new ExpandoObject();
        location.Name = "London";
        var locationSource = new List<dynamic>
        {
            location
        };
        var locationSchema = new Dictionary<string, Type>
        {
            { "Name", typeof(string) }
        };

        var vm = CreateAndRunVirtualMachine(query, [
            ("#weather", weatherSource, weatherSchema),
            ("#location", locationSource, locationSchema)
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count());

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("London", table[0].ReadValue(0));
        Assert.AreEqual(10, table[0].ReadValue(1));
        Assert.AreEqual("London", table[0].ReadValue(2));
    }

    [TestMethod]
    public void WithDynamicSource_SyntaxKeywordAsColumnNameUSed_ShouldPass()
    {
        const string query = "select [case], [end] from #dynamic.all()";
        IDictionary<string, object?> expando = new ExpandoObject();

        expando.Add("case", "case");
        expando.Add("end", "end");

        var sources =
            new List<dynamic>
            {
                (dynamic)expando
            };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("case", table[0].ReadValue(0));
        Assert.AreEqual("end", table[0].ReadValue(1));
    }

    [TestMethod]
    public void WithDynamicSource_ContextualExistsColumn_ShouldPass()
    {
        const string query = "select FullPath, Exists from #dynamic.all() take 5";
        IDictionary<string, object?> expando = new ExpandoObject();
        expando.Add("FullPath", @"D:\repos\Musoq.Cloud\src\dotnet\Musoq\bin\Debug\net10.0");
        expando.Add("Exists", true);

        var vm = CreateAndRunVirtualMachine(query, [(dynamic)expando]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("FullPath", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("Exists", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(bool), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(expando["FullPath"], table[0].ReadValue(0));
        Assert.AreEqual(true, table[0].ReadValue(1));
    }

    [TestMethod]
    public void WithDynamicSource_WhenExecutionIrRendererIsEnabledForContextualExistsColumn_ShouldPass()
    {
        const string query = "select FullPath, Exists from #dynamic.all() take 5";
        IDictionary<string, object?> expando = new ExpandoObject();
        expando.Add("FullPath", @"D:\repos\Musoq.Cloud\src\dotnet\Musoq\bin\Debug\net10.0");
        expando.Add("Exists", true);
        var sources = new List<dynamic> { (dynamic)expando };

        CreateDynamicInspection(query, sources);
        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            compilationOptions: ExecutionIrCompilationOptions);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Exists", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(true, table[0].ReadValue(1));
    }

    [TestMethod]
    public void WithDynamicSource_KeywordCollisionCatalog_ShouldPreserveValuesAndNames()
    {
        var sources = CreateKeywordCollisionSources();
        var vm = CreateAndRunVirtualMachine(KeywordCollisionQuery, sources);

        var table = vm.Run(TestContext.CancellationToken);

        AssertKeywordCollisionResult(table);
    }

    [TestMethod]
    public void WithDynamicSource_WhenExecutionIrRendererIsEnabledForKeywordCollisionCatalog_ShouldPreserveValuesAndNames()
    {
        var sources = CreateKeywordCollisionSources();

        CreateDynamicInspection(KeywordCollisionQuery, sources);
        var vm = CreateAndRunVirtualMachine(
            KeywordCollisionQuery,
            sources,
            compilationOptions: ExecutionIrCompilationOptions);

        var table = vm.Run(TestContext.CancellationToken);

        AssertKeywordCollisionResult(table);
    }

    [TestMethod]
    public void WithDynamicSource_WhenExecutionIrRendererIsEnabledForKeywordColumns_ShouldUseNativeMaterializer()
    {
        const string query = "select [case], [end] from #dynamic.all()";
        IDictionary<string, object?> expando = new ExpandoObject();

        expando.Add("case", "case");
        expando.Add("end", "end");

        var sources = new List<dynamic>
        {
            (dynamic)expando
        };

        var inspection = CreateDynamicInspection(query, sources);
        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            compilationOptions: ExecutionIrCompilationOptions);

        var table = vm.Run(TestContext.CancellationToken);



        Assert.Contains("IQueryRowMaterializer<", inspection.GeneratedCSharpCode);
        Assert.Contains("reader.Read<", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("__dynamicValue", inspection.GeneratedCSharpCode);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("EvaluationHelper.GetColumnValue", StringComparison.Ordinal));
        Assert.AreEqual("case", table[0].ReadValue(0));
        Assert.AreEqual("end", table[0].ReadValue(1));
    }

    private QueryInspectionResult CreateDynamicInspection(
        string query,
        IReadOnlyCollection<dynamic> values,
        IReadOnlyDictionary<string, Type>? schema = null)
    {
        schema ??= ((IDictionary<string, object?>)values.First()).ToDictionary(field => field.Key, field => field.Value?.GetType() ?? typeof(object));

        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new AnySchemaNameProvider(
                new Dictionary<string, (IReadOnlyDictionary<string, Type> Schema, IEnumerable<dynamic> Values)>
                {
                    { "dynamic", (schema, values) }
                }),
            LoggerResolver,
            ExecutionIrCompilationOptions);
    }

    private static List<dynamic> CreateKeywordCollisionSources()
    {
        IDictionary<string, object?> expando = new ExpandoObject();
        foreach (var column in KeywordCollisionColumns)
            expando.Add(column, $"value:{column}");

        return [(dynamic)expando];
    }

    private void AssertKeywordCollisionResult(Table table)
    {
        Assert.AreEqual(1, table.Count);
        Assert.HasCount(KeywordCollisionColumns.Length, table.Columns);

        for (var index = 0; index < KeywordCollisionColumns.Length; index++)
        {
            var column = KeywordCollisionColumns[index];
            Assert.AreEqual(column, table.Columns.ElementAt(index).ColumnName);
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(index).ColumnType);
            Assert.AreEqual($"value:{column}", table[0].ReadValue(index));
        }
    }
}
