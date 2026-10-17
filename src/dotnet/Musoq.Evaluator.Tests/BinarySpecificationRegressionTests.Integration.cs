using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    private const string IntegrationSchema = """
        binary Header { Count: byte };
        binary RecordOne {
            Kind: byte,
            Measure: double le when Kind = 1,
            Enabled: Measure > 0,
            Extra: byte when Enabled,
            Choice: CASE WHEN NOT Kind = 1 THEN 9 ELSE Extra END
        };
        binary Item { Values: byte[parent.parent.Header.Count] };
        binary Outline { Rows: Item[parent.Header.Count] };
        binary Rec { Header: Header, First: RecordOne, Body: Outline, Tail: byte };
        """;

    private const string IntegrationProjection = "select r.First.Measure, r.First.Enabled, r.First.Choice, v.Value, r.Tail from #test.files() f ";
    private const string IntegrationApply = "outer apply r.Body.Rows i outer apply i.Values v";

    [TestMethod]
    public void CompleteIntegratedBinaryQuery_ShouldParseConditionalAndParentDependentRecords()
    {
        var table = RunBinaryQuery(IntegrationSchema + IntegrationProjection + "cross apply Interpret<Rec>(f.Content) r " + IntegrationApply,
            [2, 1, 0, 0, 0, 0, 0, 0, 248, 63, 7, 10, 20, 30, 40, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table,
            [1.5d, true, 7, (byte)10, (byte)99], [1.5d, true, 7, (byte)20, (byte)99],
            [1.5d, true, 7, (byte)30, (byte)99], [1.5d, true, 7, (byte)40, (byte)99]);
    }

    [TestMethod]
    public void IntegratedBinaryQueryWithoutOptionalFields_ShouldKeepEmptyCollectionRow()
    {
        var table = RunBinaryQuery(IntegrationSchema + IntegrationProjection + "cross apply Interpret<Rec>(f.Content) r " + IntegrationApply, [0, 0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null, 9, null, (byte)99]);
    }

    [TestMethod]
    public void StrictIntegratedBinaryQuery_ShouldReportTruncatedInput()
    {
        var exception = Assert.Throws<ParseException>(() => RunBinaryQuery(IntegrationSchema + IntegrationProjection + "cross apply Interpret<Rec>(f.Content) r " + IntegrationApply, [2, 1]));

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
    }

    [TestMethod]
    public void TolerantIntegratedBinaryQuery_ShouldPreserveRejectedRecord()
    {
        var table = RunBinaryQuery(IntegrationSchema + IntegrationProjection + "outer apply TryInterpret<Rec>(f.Content) r " + IntegrationApply, [2, 1]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null, null, null, null, null });
    }
}
