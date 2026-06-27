using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Tables;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    public static Table GetSpecificSchemaDescriptions(ISchema schema, SourceExecutionContext executionContext)
    {
        return CreateTableFromConstructors(() => schema.GetRawConstructors(executionContext));
    }

    public static Table GetConstructorsForSpecificMethod(ISchema schema, string methodName,
        SourceExecutionContext executionContext)
    {
        return CreateTableFromConstructors(() => schema.GetRawConstructors(methodName, executionContext));
    }

    public static Table GetMethodsForSchema(ISchema schema, SourceExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(executionContext);
        executionContext.EndWorkToken.ThrowIfCancellationRequested();

        var libraryMethods = schema.GetAllLibraryMethods();

        var newTable = new Table("desc", [
            new Column("Method", typeof(string), 0),
            new Column("Description", typeof(string), 1),
            new Column("Category", typeof(string), 2),
            new Column("Source", typeof(string), 3)
        ]);


        var methodRows =
            new List<(string Signature, string Description, string Category, string Source, int SortOrder)>();

        foreach (var (_, methodInfos) in libraryMethods)
        {
            foreach (var methodInfo in methodInfos)
            {
                executionContext.EndWorkToken.ThrowIfCancellationRequested();

                var bindableAttr = methodInfo.GetCustomAttribute<BindableMethodAttribute>();
                if (bindableAttr?.IsInternal == true)
                    continue;

                var signature = CSharpTypeNameHelper.FormatMethodSignature(methodInfo);
                var description = GetXmlDocumentation(methodInfo);
                var category = GetMethodCategory(methodInfo);
                var source = GetMethodSource(methodInfo);
                var sortOrder = source == "Schema" ? 0 : 1;

                methodRows.Add((signature, description, category, source, sortOrder));
            }
        }


        var sortedRows = methodRows
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => row.Category)
            .ThenBy(row => row.Signature);

        foreach (var row in sortedRows)
            newTable.AddUnchecked(new DescriptionMethodRow(row.Signature, row.Description, row.Category, row.Source));

        return newTable;
    }
}
