using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private static Table CreateTableFromConstructors(Func<SchemaMethodInfo[]> getConstructors)
    {
        var maxColumns = 0;
        var values = new List<List<string>>();

        foreach (var constructor in getConstructors())
        {
            var row = new List<string>();
            values.Add(row);

            row.Add(constructor.MethodName);

            if (constructor.ConstructorInfo.Arguments.Length > maxColumns)
                maxColumns = constructor.ConstructorInfo.Arguments.Length;

            var signature = SchemaSourceSignature.Create(constructor);

            for (var index = 0; index < signature.Parameters.Length; index++)
            {
                var param = signature.Parameters[index];
                var suffix = param.HasDefaultValue
                    ? $" = {SchemaSourceDefaultFormatter.Format(param.DefaultValue)}"
                    : string.Empty;
                row.Add($"{param.Name}: {param.ParameterType.FullName}{suffix}");
            }
        }

        maxColumns += 1;

        foreach (var row in values)
            if (maxColumns > row.Count)
                row.AddRange(new string[maxColumns - row.Count]);

        var columns = new Column[maxColumns];
        columns[0] = new Column("Name", typeof(string), 0);

        for (var i = 1; i < columns.Length; i++) columns[i] = new Column($"Param {i - 1}", typeof(string), i);

        var descTable = new Table("desc", columns);
        var layout = RowLayout.Create(
            columns.Length,
            columns.Select(static column => new RowLayoutName(column.ColumnName, column.ColumnIndex)).ToArray());

        foreach (var row in values)
            descTable.AddUnchecked(new DescriptionConstructorRow(layout, row.ToArray()));

        return descTable;
    }

}
