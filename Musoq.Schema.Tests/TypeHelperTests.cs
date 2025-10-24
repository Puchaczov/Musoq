using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Attributes;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Tests;

[TestClass]
public class TypeHelperTests
{
    private class TestEntity
    {
        [EntityProperty]
        public int Prop1 { get; set; }

        [EntityProperty]
        public string Prop2 { get; set; }

        [EntityProperty]
        public decimal Prop3 { get; set; }

        [EntityProperty]
        public bool Prop4 { get; set; }
    }

    [TestMethod]
    public void TestMethod1()
    {
        var (nameToIndexMap, indexToMethodAccessMap, columns) = TypeHelper.GetEntityMap<TestEntity>();

        var test = new TestEntity()
        {
            Prop1 = 4,
            Prop2 = "Test",
            Prop3 = 5.3m,
            Prop4 = true
        };

        Assert.HasCount(4, columns);
        Assert.HasCount(4, indexToMethodAccessMap);
        Assert.HasCount(4, nameToIndexMap);

        Assert.AreEqual(0, columns[0].ColumnIndex);
        Assert.AreEqual(1, columns[1].ColumnIndex);
        Assert.AreEqual(2, columns[2].ColumnIndex);
        Assert.AreEqual(3, columns[3].ColumnIndex);

        Assert.AreEqual(nameof(TestEntity.Prop1), columns[0].ColumnName);
        Assert.AreEqual(nameof(TestEntity.Prop2), columns[1].ColumnName);
        Assert.AreEqual(nameof(TestEntity.Prop3), columns[2].ColumnName);
        Assert.AreEqual(nameof(TestEntity.Prop4), columns[3].ColumnName);

        Assert.AreEqual(typeof(int), columns[0].ColumnType);
        Assert.AreEqual(typeof(string), columns[1].ColumnType);
        Assert.AreEqual(typeof(decimal), columns[2].ColumnType);
        Assert.AreEqual(typeof(bool), columns[3].ColumnType);

        Assert.AreEqual(0, nameToIndexMap[nameof(TestEntity.Prop1)]);
        Assert.AreEqual(1, nameToIndexMap[nameof(TestEntity.Prop2)]);
        Assert.AreEqual(2, nameToIndexMap[nameof(TestEntity.Prop3)]);
        Assert.AreEqual(3, nameToIndexMap[nameof(TestEntity.Prop4)]);

        Assert.AreEqual(4, indexToMethodAccessMap[0](test));
        Assert.AreEqual("Test", indexToMethodAccessMap[1](test));
        Assert.AreEqual(5.3m, indexToMethodAccessMap[2](test));
        Assert.IsTrue((bool?)indexToMethodAccessMap[3](test));
    }
}