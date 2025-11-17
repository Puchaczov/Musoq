using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PassPrimitiveTypesTests : BasicEntityTestBase
{
    [TestMethod]
    public void GetSchemaTableAndRowSourcePassedPrimitiveArgumentsTest()
    {
        var query = "select 1 from #test.whatever(1, 2d, true, false, 'text')";

        var vm = CreateAndRunVirtualMachine(query, [], (passedParams) =>
        {
            Assert.AreEqual(1, passedParams[0]);
            Assert.AreEqual(2m, passedParams[1]);
            Assert.IsTrue((bool?)passedParams[2]);
            Assert.IsFalse((bool?)passedParams[3]);
            Assert.AreEqual("text", passedParams[4]);
        }, WhenCheckedParameters.OnSchemaTableOrRowSourceGet);

        vm.Run();
    }

    [TestMethod]
    public void CallWithPrimitiveArgumentsTest()
    {
        var query = "select PrimitiveArgumentsMethod(1, 2d, true, false, 'text') from #test.whatever()";

        var vm = CreateAndRunVirtualMachine(query, [], (passedParams) =>
        {
            Assert.AreEqual(1L, passedParams[0]);
            Assert.AreEqual(2m, passedParams[1]);
            Assert.IsTrue((bool?)passedParams[2]);
            Assert.IsFalse((bool?)passedParams[3]);
            Assert.AreEqual("text", passedParams[4]);
        }, WhenCheckedParameters.OnMethodCall);

        vm.Run();
    }

    private enum WhenCheckedParameters
    {
        OnSchemaTableOrRowSourceGet,
        OnMethodCall
    }

    private class TestSchemaProvider(
        IEnumerable<TestEntity> entities,
        Action<object[]> onGetTableOrRowSource,
        WhenCheckedParameters whenChecked)
        : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new TestSchema(entities, onGetTableOrRowSource, whenChecked);
        }
    }

    private class TestSchema(
        IEnumerable<TestEntity> entities,
        Action<object[]> onGetTableOrRowSource,
        WhenCheckedParameters whenChecked)
        : SchemaBase("test", CreateLibrary())
    {
        public override RowSource GetRowSource(string name, RuntimeContext communicator, params object[] parameters)
        {
            if(whenChecked == WhenCheckedParameters.OnSchemaTableOrRowSourceGet) onGetTableOrRowSource(parameters);
            return new EntitySource<TestEntity>(entities, new Dictionary<string, int>(), new Dictionary<int, Func<TestEntity, object>>());
        }

        public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
        {
            if (whenChecked == WhenCheckedParameters.OnSchemaTableOrRowSourceGet) onGetTableOrRowSource(parameters);
            return new TestTable();
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var lib = new Library();

            propertiesManager.RegisterProperties(lib);
            methodManager.RegisterLibraries(lib);

            return new MethodsAggregator(methodManager);
        }

        public override SchemaMethodInfo[] GetConstructors()
        {
            var methodInfos = new List<SchemaMethodInfo>();
            return methodInfos.ToArray();
        }
    }

    private class TestTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [];

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.Single(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(TestEntity));
    }

    private class TestEntity;

    private CompiledQuery CreateAndRunVirtualMachine(string script, IEnumerable<TestEntity> source, Action<object[]> onGetTableOrRowSource, WhenCheckedParameters whenChecked)
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());
            
        return InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString(), new TestSchemaProvider(source, onGetTableOrRowSource, whenChecked), LoggerResolver);
    }
}
