using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Tests;

[TestClass]
public class ColumnStabilityTests
{
    [TestMethod]
    public void LegacySchemaColumnImplementation_DefaultsToStable()
    {
        ISchemaColumn column = new LegacyColumn();

        Assert.AreEqual(ColumnStability.Stable, column.Stability);
    }

    [TestMethod]
    public void SchemaColumn_ExplicitVolatileMetadataIsPreserved()
    {
        var column = new SchemaColumn("Value", 0, typeof(int), ColumnStability.Volatile);

        Assert.AreEqual(ColumnStability.Volatile, column.Stability);
    }

    [TestMethod]
    public void DynamicTable_MergesConflictingStabilityConservatively()
    {
        var table = new DynamicTable(
        [
            new SchemaColumn("Value", 0, typeof(int), ColumnStability.Stable),
            new SchemaColumn("Value", 0, typeof(int), ColumnStability.Volatile)
        ]);

        Assert.AreEqual(ColumnStability.Volatile, table.GetColumnByName("Value")!.Stability);
    }

    [TestMethod]
    public void TypeHelper_MarksNonDeterministicEntityPropertyVolatile()
    {
        var (_, _, columns) = TypeHelper.GetEntityMap<MarkedEntity>();

        Assert.AreEqual(ColumnStability.Stable, columns.Single(column => column.ColumnName == nameof(MarkedEntity.Stable)).Stability);
        Assert.AreEqual(ColumnStability.Volatile, columns.Single(column => column.ColumnName == nameof(MarkedEntity.Volatile)).Stability);
    }

    private sealed class LegacyColumn : ISchemaColumn
    {
        public string ColumnName => "Value";
        public int ColumnIndex => 0;
        public Type ColumnType => typeof(int);
        public Type SourceReadType => ColumnType;
        public EnumTypeDescriptor? EnumType => null;
    }

    private sealed class MarkedEntity
    {
        [EntityProperty]
        public int Stable { get; init; }

        [EntityProperty]
        [NonDeterministic]
        public int Volatile { get; init; }
    }
}
