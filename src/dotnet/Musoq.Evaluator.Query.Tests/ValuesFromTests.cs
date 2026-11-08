using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class ValuesFromTests : BasicEntityTestBase
{

    [TestMethod]
    public void ValuesSource_ProjectAndFilter_ShouldWork()
    {
        const string query = @"
from values {
    ( Name: 'Newtonsoft.Json', Approved: true, Score: 10 ),
    ( Name: 'Legacy.Package', Approved: false, Score: 20 )
} packages
where packages.Approved = false
select packages.Name, packages.Score";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Legacy.Package", 20]);
    }

    [TestMethod]
    public void ValuesSource_JoinWithSchemaSource_ShouldWork()
    {
        const string query = @"
select entity.Name, policy.Approved
from #A.Entities() entity
inner join values {
    ( Name: 'Newtonsoft.Json', Approved: true ),
    ( Name: 'Legacy.Package', Approved: false )
} policy on entity.Name = policy.Name
where policy.Approved = false";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Newtonsoft.Json"),
                    new BasicEntity("Legacy.Package"),
                    new BasicEntity("Other.Package")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Legacy.Package", table[0].ReadValue(0));
        Assert.IsFalse((bool)table[0].ReadValue(1));
    }

    [TestMethod]
    public void ValuesSource_InCteCanBeReferencedMultipleTimes_ShouldWork()
    {
        const string query = @"
with policy as (
    from values {
        ( Name: 'Newtonsoft.Json', Approved: true ),
        ( Name: 'Legacy.Package', Approved: false )
    } p
    select p.Name, p.Approved
)
select leftPolicy.Name, rightPolicy.Approved
from policy leftPolicy
inner join policy rightPolicy on leftPolicy.Name = rightPolicy.Name
where rightPolicy.Approved = false";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Legacy.Package", table[0].ReadValue(0));
        Assert.IsFalse((bool)table[0].ReadValue(1));
    }

    [TestMethod]
    public void ValuesSource_SelectFirstOrderBySkipTake_ShouldWork()
    {
        const string query = @"
select packages.Name, packages.Score
from values {
    ( Name: 'Newtonsoft.Json', Score: 10 ),
    ( Name: 'Legacy.Package', Score: 20 ),
    ( Name: 'Modern.Package', Score: 30 )
} packages
order by packages.Score desc
skip 1
take 1";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Legacy.Package", table[0].ReadValue(0));
        Assert.AreEqual(20, table[0].ReadValue(1));
    }

    [TestMethod]
    public void ValuesSource_WithNulls_ShouldInferNullableValueType()
    {
        const string query = @"
select packages.Name, packages.Score
from values {
    ( Name: 'Newtonsoft.Json', Score: null ),
    ( Name: 'Legacy.Package', Score: 20 )
} packages
order by packages.Name";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        AssertColumn(table, 1, "Score", typeof(int?));
        Assert.AreEqual("Legacy.Package", table[0].ReadValue(0));
        Assert.AreEqual(20, table[0].ReadValue(1));
        Assert.AreEqual("Newtonsoft.Json", table[1].ReadValue(0));
        Assert.IsNull(table[1].ReadValue(1));
    }

    [TestMethod]
    public void ValuesSource_WithUnsignedIntegerSuffix_ShouldInferUInt()
    {
        const string query = @"
select scores.Name, scores.Score
from values {
    ( Name: 'first', Score: 10ui ),
    ( Name: 'second', Score: 20ui )
} scores
order by scores.Score";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        AssertColumn(table, 1, "Score", typeof(uint));
        Assert.AreEqual(10u, table[0].ReadValue(1));
        Assert.AreEqual(20u, table[1].ReadValue(1));
    }

    [TestMethod]
    public void ValuesSource_WithSupportedLiteralKinds_ShouldInferExactTypes()
    {
        const string query = @"
select literals.PlainInt,
       literals.IntSuffix,
       literals.UIntSuffix,
       literals.LongSuffix,
       literals.ULongSuffix,
       literals.ShortSuffix,
       literals.UShortSuffix,
       literals.SByteSuffix,
       literals.ByteSuffix,
       literals.DecimalSuffix,
       literals.DecimalPoint,
       literals.DecimalPointSuffix,
       literals.HexValue,
       literals.BinaryValue,
       literals.OctalValue,
       literals.NegativeInt,
       literals.NegativeDecimal,
       literals.StringValue,
       literals.BooleanValue,
       literals.NullValue
from values {
    (
        PlainInt: 10,
        IntSuffix: 11i,
        UIntSuffix: 12ui,
        LongSuffix: 13l,
        ULongSuffix: 14ul,
        ShortSuffix: 15s,
        UShortSuffix: 16us,
        SByteSuffix: 17b,
        ByteSuffix: 18ub,
        DecimalSuffix: 19d,
        DecimalPoint: 20.5,
        DecimalPointSuffix: 21.5d,
        HexValue: 0x10,
        BinaryValue: 0b1010,
        OctalValue: 0o17,
        NegativeInt: -22,
        NegativeDecimal: -23.5,
        StringValue: 'literal',
        BooleanValue: true,
        NullValue: null
    )
} literals";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        AssertColumn(table, 0, "PlainInt", typeof(int));
        AssertColumn(table, 1, "IntSuffix", typeof(int));
        AssertColumn(table, 2, "UIntSuffix", typeof(uint));
        AssertColumn(table, 3, "LongSuffix", typeof(long));
        AssertColumn(table, 4, "ULongSuffix", typeof(ulong));
        AssertColumn(table, 5, "ShortSuffix", typeof(short));
        AssertColumn(table, 6, "UShortSuffix", typeof(ushort));
        AssertColumn(table, 7, "SByteSuffix", typeof(sbyte));
        AssertColumn(table, 8, "ByteSuffix", typeof(byte));
        AssertColumn(table, 9, "DecimalSuffix", typeof(decimal));
        AssertColumn(table, 10, "DecimalPoint", typeof(decimal));
        AssertColumn(table, 11, "DecimalPointSuffix", typeof(decimal));
        AssertColumn(table, 12, "HexValue", typeof(long));
        AssertColumn(table, 13, "BinaryValue", typeof(long));
        AssertColumn(table, 14, "OctalValue", typeof(long));
        AssertColumn(table, 15, "NegativeInt", typeof(int));
        AssertColumn(table, 16, "NegativeDecimal", typeof(decimal));
        AssertColumn(table, 17, "StringValue", typeof(string));
        AssertColumn(table, 18, "BooleanValue", typeof(bool));
        AssertColumn(table, 19, "NullValue", typeof(System.DBNull));

        Assert.AreEqual(10, table[0].ReadValue(0));
        Assert.AreEqual(11, table[0].ReadValue(1));
        Assert.AreEqual(12u, table[0].ReadValue(2));
        Assert.AreEqual(13L, table[0].ReadValue(3));
        Assert.AreEqual(14UL, table[0].ReadValue(4));
        Assert.AreEqual((short)15, table[0].ReadValue(5));
        Assert.AreEqual((ushort)16, table[0].ReadValue(6));
        Assert.AreEqual((sbyte)17, table[0].ReadValue(7));
        Assert.AreEqual((byte)18, table[0].ReadValue(8));
        Assert.AreEqual(19m, table[0].ReadValue(9));
        Assert.AreEqual(20.5m, table[0].ReadValue(10));
        Assert.AreEqual(21.5m, table[0].ReadValue(11));
        Assert.AreEqual(16L, table[0].ReadValue(12));
        Assert.AreEqual(10L, table[0].ReadValue(13));
        Assert.AreEqual(15L, table[0].ReadValue(14));
        Assert.AreEqual(-22, table[0].ReadValue(15));
        Assert.AreEqual(-23.5m, table[0].ReadValue(16));
        Assert.AreEqual("literal", table[0].ReadValue(17));
        Assert.IsTrue((bool)table[0].ReadValue(18));
        Assert.IsNull(table[0].ReadValue(19));
    }
}
