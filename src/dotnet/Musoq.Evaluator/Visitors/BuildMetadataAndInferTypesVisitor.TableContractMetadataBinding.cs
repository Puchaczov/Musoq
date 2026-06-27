using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CreateTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tableColumns = new List<ISchemaColumn>();

        for (var i = 0; i < node.Columns.Count; i++)
        {
            var column = node.Columns[i];
            var remappedType = EvaluationHelper.RemapPrimitiveTypes(column.TypeName);
            var type = EvaluationHelper.RemapPrimitiveTypeAsNullable(remappedType);

            if (type == null)
            {
                if (TryReportTypeNotFound(remappedType, node))
                    continue;
                var span = node.SpanOrEmpty();
                throw new TypeNotFoundException(remappedType, string.Empty, span);
            }

            tableColumns.Add(CreateSchemaColumn(column, i, type));
        }

        var table = new DynamicTable(tableColumns.ToArray());
        _sourceBinding.ExplicitlyDefinedTables.Add(node.Name, table);
        _sourceBinding.ExplicitlyDefinedTableDiagnosticLocations[node.Name] =
            SourceContractDiagnosticLocationMap.FromTable(node);

        Nodes.Push(new CreateTableNode(node.Name, node.Columns));
    }
}
