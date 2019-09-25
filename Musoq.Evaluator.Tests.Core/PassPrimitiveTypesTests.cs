using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Core.Schema;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests.Core
{
    [TestClass]
    public class PassPrimitiveTypesTests : TestBase
    {
        [TestMethod]
        public void GetSchemaTableAndRowSourcePassedPrimitiveArgumentsTest()
        {
            var query = "select 1 from #test.whatever(1, 2d, true, false, 'text')";

            var vm = CreateAndRunVirtualMachine(query, new List<TestEntity>(), (passedParams) =>
            {
                Assert.AreEqual(1, passedParams[0]);
                Assert.AreEqual(2m, passedParams[1]);
                Assert.AreEqual(true, passedParams[2]);
                Assert.AreEqual(false, passedParams[3]);
                Assert.AreEqual("text", passedParams[4]);
            }, WhenCheckedParameters.OnSchemaTableOrRowSourceGet);

            vm.Run();
        }

        [TestMethod]
        public void CallWithPrimitiveArgumentsTest()
        {
            var query = "select PrimitiveArgumentsMethod(1, 2d, true, false, 'text') from #test.whatever()";

            var vm = CreateAndRunVirtualMachine(query, new List<TestEntity>(), (passedParams) =>
            {
                Assert.AreEqual(1L, passedParams[0]);
                Assert.AreEqual(2m, passedParams[1]);
                Assert.AreEqual(true, passedParams[2]);
                Assert.AreEqual(false, passedParams[3]);
                Assert.AreEqual("text", passedParams[4]);
            }, WhenCheckedParameters.OnMethodCall);

            vm.Run();
        }

        private enum WhenCheckedParameters
        {
            OnSchemaTableOrRowSourceGet,
            OnMethodCall
        }

        private class TestSchemaProvider : ISchemaProvider
        {
            private readonly IEnumerable<TestEntity> _entities;
            private readonly Action<object[]> _onGetTableOrRowSource;
            private readonly WhenCheckedParameters _whenChecked;

            public TestSchemaProvider(IEnumerable<TestEntity> entities, Action<object[]> onGetTableOrRowSource, WhenCheckedParameters whenChecked)
            {
                _entities = entities;
                _onGetTableOrRowSource = onGetTableOrRowSource;
                _whenChecked = whenChecked;
            }
            public ISchema GetSchema(string schema)
            {
                return new TestSchema(_entities, _onGetTableOrRowSource, _whenChecked);
            }
        }

        private class TestSchema : SchemaBase
        {

            private readonly IEnumerable<TestEntity> _entities;
            private readonly Action<object[]> _onGetTableOrRowSource;
            private readonly WhenCheckedParameters _whenChecked;

            public TestSchema(IEnumerable<TestEntity> entities, Action<object[]> onGetTableOrRowSource,
                WhenCheckedParameters whenChecked)
                : base("test", CreateLibrary())
            {
                _entities = entities;
                _onGetTableOrRowSource = onGetTableOrRowSource;
                _whenChecked = whenChecked;
            }

            public override RowSource GetRowSource(string name, RuntimeContext communicator, params object[] parameters)
            {
                if(_whenChecked == WhenCheckedParameters.OnSchemaTableOrRowSourceGet) _onGetTableOrRowSource(parameters);
                return new EntitySource<TestEntity>(_entities, new Dictionary<string, int>(), new Dictionary<int, Func<TestEntity, object>>());
            }

            public override ISchemaTable GetTableByName(string name, params object[] parameters)
            {
                if (_whenChecked == WhenCheckedParameters.OnSchemaTableOrRowSourceGet) _onGetTableOrRowSource(parameters);
                return new TestTable();
            }

            private static MethodsAggregator CreateLibrary()
            {
                var methodManager = new MethodsManager();
                var propertiesManager = new PropertiesManager();

                var lib = new TestLibrary();

                propertiesManager.RegisterProperties(lib);
                methodManager.RegisterLibraries(lib);

                return new MethodsAggregator(methodManager, propertiesManager);
            }

            public override SchemaMethodInfo[] GetConstructors()
            {
                var methodInfos = new List<SchemaMethodInfo>();
                return methodInfos.ToArray();
            }
        }

        private class TestTable : ISchemaTable
        {
            public ISchemaColumn[] Columns => new ISchemaColumn[0];

            public ISchemaColumn GetColumnByName(string name)
            {
                return Columns.Single(column => column.ColumnName == name);
            }
        }

        private class TestEntity
        {
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script, IEnumerable<TestEntity> source, Action<object[]> onGetTableOrRowSource, WhenCheckedParameters whenChecked)
        {
            return InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString(), new TestSchemaProvider(source, onGetTableOrRowSource, whenChecked));
        }
    }
}
