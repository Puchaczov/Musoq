using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCore005DataTypeTests : BasicEntityTestBase
{
    [TestMethod]
    public void LiteralAndNullPostfixCasts_ShouldExposeDocumentedTypesAndValues()
    {
        var table = CreateAndRunVirtualMachine(
            "select 42::Int32 as IntValue, .5::Decimal as DecimalValue, true::Boolean as BoolValue, 'Z'::Char as CharValue, null::DateTime as NullDate from #A.Entities()",
            CreateSingleSource(new BasicEntity())).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("IntValue", typeof(int?)),
            ("DecimalValue", typeof(decimal?)),
            ("BoolValue", typeof(bool?)),
            ("CharValue", typeof(char?)),
            ("NullDate", typeof(DateTime?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { 42, .5m, true, 'Z', null });
    }
}
