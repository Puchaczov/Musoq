using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PrimitiveConversionSqlBindingTests : BasicEntityTestBase
{
    [TestMethod]
    public void PrimitiveConversions_ShouldBindRepresentativeSqlForEverySourceFamily()
    {
        var table = TestResultMethodTemplate(
            "ToInt32('42'), " +
            "ToInt32(42ub), " +
            "ToInt32(42b), " +
            "ToInt32(42s), " +
            "ToInt32(42us), " +
            "ToInt64(42i), " +
            "ToInt64(42ui), " +
            "ToInt64(42l), " +
            "ToUInt64(42ul), " +
            "ToInt32(ToSingle('42')), " +
            "ToInt32(ToDouble('42')), " +
            "ToInt32(42d), " +
            "ToInt32(true), " +
            "ToInt32(ToChar('*')), " +
            "ToString(Self)");

        Type[] expectedTypes =
        [
            typeof(int?), typeof(int?), typeof(int?), typeof(int?), typeof(int?),
            typeof(long?), typeof(long?), typeof(long?), typeof(ulong?),
            typeof(int?), typeof(int?), typeof(int?), typeof(int?), typeof(int?),
            typeof(string)
        ];
        object[] expectedValues =
        [
            42, 42, 42, 42, 42,
            42L, 42L, 42L, 42UL,
            42, 42, 42, 1, 42,
            "TEST STRING"
        ];

        Assert.AreEqual(1, table.Count);
        Assert.HasCount(expectedTypes.Length, table.Columns);
        for (var index = 0; index < expectedTypes.Length; index++)
        {
            Assert.AreEqual(expectedTypes[index], table.Columns.ElementAt(index).ColumnType);
            Assert.AreEqual(expectedValues[index], table[0][index]);
        }
    }
}
