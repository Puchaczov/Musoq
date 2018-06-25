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
            var maps = TypeHelper.GetEntityMap<TestEntity>();

            var test = new TestEntity()
            {
                Prop1 = 4,
                Prop2 = "Test",
                Prop3 = 5.3m,
                Prop4 = true
            };

            Assert.AreEqual(4, maps.Columns.Length);
            Assert.AreEqual(4, maps.IndexToMethodAccessMap.Count);
            Assert.AreEqual(4, maps.NameToIndexMap.Count);

            Assert.AreEqual(0, maps.Columns[0].ColumnIndex);
            Assert.AreEqual(1, maps.Columns[1].ColumnIndex);
            Assert.AreEqual(2, maps.Columns[2].ColumnIndex);
            Assert.AreEqual(3, maps.Columns[3].ColumnIndex);

            Assert.AreEqual(nameof(TestEntity.Prop1), maps.Columns[0].ColumnName);
            Assert.AreEqual(nameof(TestEntity.Prop2), maps.Columns[1].ColumnName);
            Assert.AreEqual(nameof(TestEntity.Prop3), maps.Columns[2].ColumnName);
            Assert.AreEqual(nameof(TestEntity.Prop4), maps.Columns[3].ColumnName);

            Assert.AreEqual(typeof(int), maps.Columns[0].ColumnType);
            Assert.AreEqual(typeof(string), maps.Columns[1].ColumnType);
            Assert.AreEqual(typeof(decimal), maps.Columns[2].ColumnType);
            Assert.AreEqual(typeof(bool), maps.Columns[3].ColumnType);

            Assert.AreEqual(maps.NameToIndexMap[nameof(TestEntity.Prop1)], 0);
            Assert.AreEqual(maps.NameToIndexMap[nameof(TestEntity.Prop2)], 1);
            Assert.AreEqual(maps.NameToIndexMap[nameof(TestEntity.Prop3)], 2);
            Assert.AreEqual(maps.NameToIndexMap[nameof(TestEntity.Prop4)], 3);

            Assert.AreEqual(4, maps.IndexToMethodAccessMap[0](test));
            Assert.AreEqual("Test", maps.IndexToMethodAccessMap[1](test));
            Assert.AreEqual(5.3m, maps.IndexToMethodAccessMap[2](test));
            Assert.AreEqual(true, maps.IndexToMethodAccessMap[3](test));
        }
    }
}
