using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DynamicSourceWithDynamicTypeHinting : DynamicQueryTestsBase
{
    [TestMethod]
    public void WithDynamicSource_DescDynamicObjectWithSimpleColumns_ShouldPass()
    {
        const string query = "desc #dynamic.all()";
        var sources =
            new List<dynamic>
            {
                new ComplexType(1, "Test1"),
            };
        
        var vm = CreateAndRunVirtualMachine(query, sources, new Dictionary<string, Type>()
        {
            {"Id", typeof(int)},
            {"Name", typeof(string)},
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Id", table[0][0]);
        Assert.AreEqual("System.Int32", table[0][2]);
        Assert.AreEqual("Name", table[1][0]);
        Assert.AreEqual("System.String", table[1][2]);
    }
    
    [TestMethod]
    public void WithDynamicSource_SimpleQuery_ShouldPass()
    {
        const string query = "select Id, Name from #dynamic.all()";
        var sources =
            new List<dynamic>
            {
                new ComplexType(1, "Test1"),
                new ComplexType(2, "Test2")
            };
        
        var vm = CreateAndRunVirtualMachine(query, sources, new Dictionary<string, Type>
        {
            {"Id", typeof(int)},
            {"Name", typeof(string)},
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(2, table.Count, "Table should have 2 entries");

        Assert.IsTrue(table.Any(entry => 
                (int)entry[0] == 1 && 
                (string)entry[1] == "Test1"), 
            "First entry should have values 1 and 'Test1'");

        Assert.IsTrue(table.Any(entry => 
                (int)entry[0] == 2 && 
                (string)entry[1] == "Test2"), 
            "Second entry should have values 2 and 'Test2'");
    }

    [TestMethod]
    public void WithDynamicSource_DescDynamicObjectWithComplexColumns_ShouldPass()
    {
        const string query = "desc #dynamic.all()";
        var sources = new List<dynamic>
        {
            new ComplexExpandoType(new ComplexType(1, "Test1"))
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, new Dictionary<string, Type>
        {
            {"Complex", typeof(ComplexExpandoType)}
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Complex", table[0][0]);
        Assert.AreEqual(0, (int)table[0][1]);
        Assert.AreEqual(typeof(ComplexExpandoType).FullName, table[0][2]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessComplexObjectProperties_ShouldPass()
    {
        const string query = "select Complex.Id, Complex.Name from #dynamic.all()";
        var sources = new List<dynamic>
        {
            CreateExpandoObject(new ComplexType(1, "Test1"))
        };
        var schema = new Dictionary<string, Type>
        {
            {"Complex", typeof(ComplexType)},
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessArray_ShouldPass()
    {
        const string query = "select Complex.Array[0], Complex.Array[1] from #dynamic.all()";
        var sources = new List<dynamic>
        {
            CreateExpandoObject(new ComplexArrayOfShortsType([1, 2]))
        };
        var schema = new Dictionary<string, Type>()
        {
            {"Complex", typeof(ComplexArrayOfShortsType)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(short), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(short), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)1, table[0][0]);
        Assert.AreEqual((short)2, table[0][1]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessExpandoObjectArray_ShouldPass()
    {
        const string query = "select Complex.Array[0].Id, Complex.Array[0].Name from #dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(new ComplexArrayOfComplexTypeType([
                new ComplexType(1, "Test1")
            ]))
        };
        var schema = new Dictionary<string, Type>
        {
            {"Complex", typeof(ComplexArrayOfComplexTypeType)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
    }
    

    [TestMethod]
    public void WithDynamicSource_IncrementAccessedProperty_ShouldPass()
    {
        const string query = "select Increment(Complex.Array[0].Id) from #dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(new ComplexArrayOfComplexTypeType([
                new ComplexType(1, "test1")
            ]))
        };
        var schema = new Dictionary<string, Type>
        {
            {"Complex", typeof(ComplexArrayOfComplexTypeType)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]);
    }

    [TestMethod]
    public void WithDynamicSource_IncrementAccessedProperty_ShouldPass2()
    {
        const string query = "select TrueWhenCalled() from #dynamic.all()";
        var sources = new List<dynamic>()
        {
            new ComplexType(1, "test1")
        };
        var schema = new Dictionary<string, Type>
        {
            {"Complex", typeof(ComplexType)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.IsTrue((bool?)table[0][0]);
    }

    private ExpandoObject CreateExpandoObject(ComplexType complexType)
    {
        var expandoObject = new ExpandoObject();
        
        ((IDictionary<string, object>) expandoObject).Add("Complex", complexType);
        
        return expandoObject;
    }

    private ExpandoObject CreateExpandoObject(DynamicObject dynamicObject)
    {
        var expandoObject = new ExpandoObject();
        
        ((IDictionary<string, object>) expandoObject).Add("Complex", dynamicObject);
        
        return expandoObject;
    }

    [DynamicObjectPropertyTypeHint("Id", typeof(int))]
    [DynamicObjectPropertyTypeHint("Name", typeof(string))]
    public class ComplexType : DynamicObject
    {
        private readonly int _intValue;
        private readonly string _stringValue;

        public ComplexType(int intValue, string stringValue)
        {
            _intValue = intValue;
            _stringValue = stringValue;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            switch (binder.Name)
            {
                case "Id":
                    result = _intValue;
                    return true;
                case "Name":
                    result = _stringValue;
                    return true;
                default:
                    return base.TryGetMember(binder, out result);
            }
        }
    }

    public class ComplexArrayType<TType> : DynamicObject
    {
        private readonly TType[] _arrayValue;

        protected ComplexArrayType(TType[] arrayValue)
        {
            _arrayValue = arrayValue;
        }
        
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name != "Array") return base.TryGetMember(binder, out result);
            
            result = _arrayValue;
            return true;

        }
    }
    
    [DynamicObjectPropertyTypeHint("Array", typeof(short[]))]
    public class ComplexArrayOfShortsType : ComplexArrayType<short>
    {
        public ComplexArrayOfShortsType(short[] arrayValue) 
            : base(arrayValue)
        {
        }
    }
    
    [DynamicObjectPropertyTypeHint("Array", typeof(ComplexType[]))]
    public class ComplexArrayOfComplexTypeType : ComplexArrayType<ComplexType>
    {
        public ComplexArrayOfComplexTypeType(ComplexType[] arrayValue) 
            : base(arrayValue)
        {
        }
    }
    
    [DynamicObjectPropertyTypeHint("Complex", typeof(ComplexType))]
    public class ComplexExpandoType : DynamicObject
    {
        private readonly ComplexType _expandoType;

        public ComplexExpandoType(ComplexType complexType)
        {
            _expandoType = complexType;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name == "Complex")
            {
                result = _expandoType;
                return true;
            }
            
            return base.TryGetMember(binder, out result);
        }
    }

    [DynamicObjectPropertyTypeHint("NestedExpando", typeof(DynamicObject))]
    public class NestedExpandoType : DynamicObject
    {
        private readonly DynamicObject _nestedExpandoType;

        public NestedExpandoType(DynamicObject nestedExpandoType)
        {
            _nestedExpandoType = nestedExpandoType;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name == "NestedExpando")
            {
                result = _nestedExpandoType;
                return true;
            }
            
            return base.TryGetMember(binder, out result);
        }
    }
}
