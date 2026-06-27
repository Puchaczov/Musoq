using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

internal static class TableMaterializationTestHelper
{
    public static Table Materialize(Table table)
    {
        _ = table.Count;
        return table;
    }
}
