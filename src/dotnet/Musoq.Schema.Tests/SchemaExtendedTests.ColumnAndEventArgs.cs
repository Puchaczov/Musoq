using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    #region SchemaColumn Additional Tests

    [TestMethod]
    public void SchemaColumn_Equality_SameValues_AreEqual()
    {
        var col1 = new SchemaColumn("Name", 0, typeof(string));
        var col2 = new SchemaColumn("Name", 0, typeof(string));

        Assert.AreEqual(col1.ColumnName, col2.ColumnName);
        Assert.AreEqual(col1.ColumnIndex, col2.ColumnIndex);
        Assert.AreEqual(col1.ColumnType, col2.ColumnType);
    }

    [TestMethod]
    public void SchemaColumn_DifferentIndex_AreNotEqual()
    {
        var col1 = new SchemaColumn("Name", 0, typeof(string));
        var col2 = new SchemaColumn("Name", 1, typeof(string));

        Assert.AreNotEqual(col1.ColumnIndex, col2.ColumnIndex);
    }

    [TestMethod]
    public void SchemaColumn_DifferentType_AreNotEqual()
    {
        var col1 = new SchemaColumn("Name", 0, typeof(string));
        var col2 = new SchemaColumn("Name", 0, typeof(int));

        Assert.AreNotEqual(col1.ColumnType, col2.ColumnType);
    }

    [TestMethod]
    public void SchemaColumn_DefaultReadModifiers_ShouldBeEmpty()
    {
        var column = new SchemaColumn("Name", 0, typeof(string));

        Assert.AreEqual(0, column.ReadModifiers.Count);
        Assert.AreSame(ColumnReadModifiers.Empty, column.ReadModifiers);
    }

    [TestMethod]
    public void SchemaColumn_ReadModifiersConstructor_ShouldCopyValues()
    {
        var modifiers = new Dictionary<string, string>
        {
            [ColumnReadModifiers.Encoding] = "windows-1250"
        };

        var column = new SchemaColumn("Name", 0, typeof(string), modifiers);
        modifiers[ColumnReadModifiers.Encoding] = "utf8";

        Assert.AreEqual("windows-1250", column.ReadModifiers[ColumnReadModifiers.Encoding]);
    }

    [TestMethod]
    public void SchemaColumn_ReadModifiers_ShouldBeReadOnly()
    {
        var column = new SchemaColumn("Name", 0, typeof(string), new Dictionary<string, string>
        {
            [ColumnReadModifiers.Trim] = "true"
        });

        Assert.IsInstanceOfType<ReadOnlyDictionary<string, string>>(column.ReadModifiers);
    }

    [TestMethod]
    public void SchemaColumn_IntendedTypeNameConstructor_ShouldPreserveReadModifiers()
    {
        var column = new SchemaColumn("Nested", 0, typeof(object), "Example.Nested", new Dictionary<string, string>
        {
            [ColumnReadModifiers.Format] = "json"
        });

        Assert.AreEqual("Example.Nested", column.IntendedTypeName);
        Assert.AreEqual("json", column.ReadModifiers[ColumnReadModifiers.Format]);
    }

    [TestMethod]
    public void SourceColumnRef_DefaultConstructor_ShouldBeSourceCompatible()
    {
        var sourceColumnRef = new SourceColumnRef("Name");

        Assert.AreEqual("Name", sourceColumnRef.Name);
        Assert.AreEqual(0, sourceColumnRef.ReadModifiers.Count);
    }

    [TestMethod]
    public void SourceColumnRef_ReadModifiersConstructor_ShouldCopyValues()
    {
        var modifiers = new Dictionary<string, string>
        {
            [ColumnReadModifiers.Culture] = "pl-PL"
        };

        var sourceColumnRef = new SourceColumnRef("Amount", modifiers);
        modifiers[ColumnReadModifiers.Culture] = "en-US";

        Assert.AreEqual("pl-PL", sourceColumnRef.ReadModifiers[ColumnReadModifiers.Culture]);
    }

    #endregion

    #region DataSourceEventArgs Tests

    [TestMethod]
    public void DataSourceEventArgs_Constructor_Begin_HasCorrectProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.Begin);

        Assert.AreEqual(DataSourcePhase.Begin, args.Phase);
        Assert.AreEqual("source1", args.DataSourceName);
        Assert.AreEqual("queryId", args.QueryId);
        Assert.IsNull(args.RowsProcessed);
    }

    [TestMethod]
    public void DataSourceEventArgs_Constructor_RowsRead_HasCorrectProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.RowsRead, 100, 50);

        Assert.AreEqual(DataSourcePhase.RowsRead, args.Phase);
        Assert.AreEqual(50, args.RowsProcessed);
        Assert.AreEqual(100, args.TotalRows);
    }

    [TestMethod]
    public void DataSourceEventArgs_Constructor_End_HasCorrectProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.End, rowsProcessed: 100);

        Assert.AreEqual(DataSourcePhase.End, args.Phase);
        Assert.AreEqual(100, args.RowsProcessed);
    }

    [TestMethod]
    public void DataSourceEventArgs_RowsKnown_Phase_Works()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.RowsKnown, 500);

        Assert.AreEqual(DataSourcePhase.RowsKnown, args.Phase);
        Assert.AreEqual(500, args.TotalRows);
    }

    #endregion

}
