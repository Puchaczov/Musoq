using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DynamicSourceWithDynamicAndDefaultTypeHinting : DynamicQueryTestsBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WithDynamicSource_AccessComplexObjectProperties_ShouldPass()
    {
        const string query = "select Multiform.AsInt, Multiform.AsDouble from #dynamic.all()";
        var sources = new List<dynamic>
        {
            CreateExpandoObject(new MultiformType(2.99d))
        };
        var schema = new Dictionary<string, Type>
        {
            { "Multiform", typeof(MultiformType) }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, schema);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(double), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0].Values[0]);
        Assert.AreEqual(2.99d, table[0].Values[1]);
    }

    private static ExpandoObject CreateExpandoObject(MultiformType multiform)
    {
        dynamic obj = new ExpandoObject();
        obj.Multiform = multiform;
        return obj;
    }

    [DynamicObjectPropertyTypeHint("AsInt", typeof(int))]
    [DynamicObjectPropertyDefaultTypeHint(typeof(double))]
    public class MultiformType : DynamicObject
    {
        private readonly double _value;

        public MultiformType(double value)
        {
            _value = value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name == "AsInt")
            {
                result = (int)_value;
                return true;
            }

            if (binder.Name == "AsDouble")
            {
                result = _value;
                return true;
            }

            result = null;
            return false;
        }
    }
}
