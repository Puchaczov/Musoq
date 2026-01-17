using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Tests for TemporarySchemas classes: DescSchema, TableMetadataSource
/// </summary>
[TestClass]
public class TemporarySchemasTests
{
    #region TableMetadataSource Tests
    
    [TestMethod]
    public void TableMetadataSource_Rows_ShouldEnumerateColumns()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestColumn("Column1", typeof(string), 0),
            new TestColumn("Column2", typeof(int), 1),
            new TestColumn("Column3", typeof(bool), 2)
        };
        var source = new TableMetadataSource(columns);
        
        // Act
        var rows = new List<IObjectResolver>();
        foreach (var row in source.Rows)
        {
            rows.Add(row);
        }
        
        // Assert
        Assert.HasCount(3, rows);
    }
    
    [TestMethod]
    public void TableMetadataSource_Rows_ShouldContainColumnName()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestColumn("TestColumn", typeof(string), 0)
        };
        var source = new TableMetadataSource(columns);
        
        // Act
        var resolver = GetFirstRow(source);
        var columnName = resolver["ColumnName"];
        
        // Assert
        Assert.AreEqual("TestColumn", columnName);
    }
    
    [TestMethod]
    public void TableMetadataSource_Rows_ShouldContainColumnIndex()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestColumn("TestColumn", typeof(string), 5)
        };
        var source = new TableMetadataSource(columns);
        
        // Act
        var resolver = GetFirstRow(source);
        var columnIndex = resolver["ColumnIndex"];
        
        // Assert
        Assert.AreEqual(5, columnIndex);
    }
    
    [TestMethod]
    public void TableMetadataSource_Rows_ShouldContainColumnTypeName()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestColumn("TestColumn", typeof(string), 0)
        };
        var source = new TableMetadataSource(columns);
        
        // Act
        var resolver = GetFirstRow(source);
        var columnType = resolver["ColumnType"];
        
        // Assert
        Assert.AreEqual("String", columnType);
    }
    
    [TestMethod]
    public void TableMetadataSource_Rows_WithEmptyColumns_ShouldReturnNoRows()
    {
        // Arrange
        var columns = Array.Empty<ISchemaColumn>();
        var source = new TableMetadataSource(columns);
        
        // Act
        var rows = new List<IObjectResolver>();
        foreach (var row in source.Rows)
        {
            rows.Add(row);
        }
        
        // Assert
        Assert.IsEmpty(rows);
    }
    
    [TestMethod]
    public void TableMetadataSource_Rows_ShouldAccessByIndex()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestColumn("Col", typeof(int), 0)
        };
        var source = new TableMetadataSource(columns);
        
        // Act
        var resolver = GetFirstRow(source);
        var columnName = resolver[0];    // ColumnName
        var columnIndex = resolver[1];   // ColumnIndex
        var columnType = resolver[2];    // ColumnType
        
        // Assert
        Assert.AreEqual("Col", columnName);
        Assert.AreEqual(0, columnIndex);
        Assert.AreEqual("Int32", columnType);
    }
    
    #endregion
    
    // NOTE: DescSchema tests removed - DescSchema is designed for internal use only 
    // and its constructor passes null to SchemaBase which now throws SchemaArgumentException.
    // DescSchema is tested indirectly through the "desc #schema" query tests in DescStatementTests.cs
    
    #region Helper Methods
    
    private static IObjectResolver GetFirstRow(TableMetadataSource source)
    {
        foreach (var row in source.Rows)
        {
            return row;
        }
        throw new InvalidOperationException("No rows in source");
    }
    
    #endregion
    
    #region Test Helpers
    
    private class TestColumn : ISchemaColumn
    {
        public TestColumn(string name, Type type, int index)
        {
            ColumnName = name;
            ColumnType = type;
            ColumnIndex = index;
        }
        
        public string ColumnName { get; }
        public int ColumnIndex { get; }
        public Type ColumnType { get; }
    }
    
    private class TestTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => Array.Empty<ISchemaColumn>();
        public ISchemaColumn GetColumnByName(string name) => null;
        public ISchemaColumn[] GetColumnsByName(string name) => Array.Empty<ISchemaColumn>();
        public SchemaTableMetadata Metadata => new(typeof(object));
    }
    
    #endregion
}
