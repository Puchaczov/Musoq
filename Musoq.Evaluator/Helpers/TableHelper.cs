using Musoq.Evaluator.Tables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Helpers;

public static class TableHelper
{
    public static Table OrderBy(this Table table, Func<List<Row>, List<Row>> orderByFunc)
    {
        Table newTable = new Table(table.Name, table.Columns.ToArray());
        
        
        
        var rows = table.ToList();
        var orderedList = orderByFunc(rows);

        foreach (var row in orderedList)
            newTable.Add(row);
        return newTable;
    }
}