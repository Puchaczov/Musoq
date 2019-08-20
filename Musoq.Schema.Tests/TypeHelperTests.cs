using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Attributes;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Tests
{
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
            var (NameToIndexMap, IndexToMethodAccessMap, Columns) = TypeHelper.GetEntityMap<TestEntity>();

            var test = new TestEntity()
            {
                Prop1 = 4,
                Prop2 = "Test",
                Prop3 = 5.3m,
                Prop4 = true
            };

            Assert.AreEqual(4, Columns.Length);
            Assert.AreEqual(4, IndexToMethodAccessMap.Count);
            Assert.AreEqual(4, NameToIndexMap.Count);

            Assert.AreEqual(0, Columns[0].ColumnIndex);
            Assert.AreEqual(1, Columns[1].ColumnIndex);
            Assert.AreEqual(2, Columns[2].ColumnIndex);
            Assert.AreEqual(3, Columns[3].ColumnIndex);

            Assert.AreEqual(nameof(TestEntity.Prop1), Columns[0].ColumnName);
            Assert.AreEqual(nameof(TestEntity.Prop2), Columns[1].ColumnName);
            Assert.AreEqual(nameof(TestEntity.Prop3), Columns[2].ColumnName);
            Assert.AreEqual(nameof(TestEntity.Prop4), Columns[3].ColumnName);

            Assert.AreEqual(typeof(int), Columns[0].ColumnType);
            Assert.AreEqual(typeof(string), Columns[1].ColumnType);
            Assert.AreEqual(typeof(decimal), Columns[2].ColumnType);
            Assert.AreEqual(typeof(bool), Columns[3].ColumnType);

            Assert.AreEqual(NameToIndexMap[nameof(TestEntity.Prop1)], 0);
            Assert.AreEqual(NameToIndexMap[nameof(TestEntity.Prop2)], 1);
            Assert.AreEqual(NameToIndexMap[nameof(TestEntity.Prop3)], 2);
            Assert.AreEqual(NameToIndexMap[nameof(TestEntity.Prop4)], 3);

            Assert.AreEqual(4, IndexToMethodAccessMap[0](test));
            Assert.AreEqual("Test", IndexToMethodAccessMap[1](test));
            Assert.AreEqual(5.3m, IndexToMethodAccessMap[2](test));
            Assert.AreEqual(true, IndexToMethodAccessMap[3](test));
        }
    }
}
