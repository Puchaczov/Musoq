using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Additional tests for Tables and Helpers with low coverage (Session 4 - Phase 2)
/// </summary>
[TestClass]
public partial class SafeDataAccessTests
{
    #region Helper Classes

    private static ISchemaTable CreateVariableTable(ISchemaColumn[] columns, Type? metadata = null)
    {
        var type = typeof(Table).Assembly.GetType("Musoq.Evaluator.Tables.VariableTable") ??
                   throw new InvalidOperationException("VariableTable type was not found.");
        return (ISchemaTable)(Activator.CreateInstance(type, columns, metadata) ??
                              throw new InvalidOperationException("VariableTable instance was not created."));
    }

    private sealed class TestSchemaColumn(string name, Type type, int index) : ISchemaColumn
    {
        public string ColumnName { get; } = name;
        public int ColumnIndex { get; } = index;
        public Type ColumnType { get; } = type;
        public Type SourceReadType { get; } = type;
        public EnumTypeDescriptor? EnumType => null;
    }

    #endregion
}
