using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static class TableHelper
{
    public static Table OrderBy(this Table table, Func<List<Row>, List<Row>> orderByFunc)
    {
        var newTable = new Table(table.Name, table.Columns.ToArray());


        var rows = table.ToList();
        var orderedList = orderByFunc(rows);

        foreach (var row in orderedList)
            newTable.Add(row);
        return newTable;
    }
}