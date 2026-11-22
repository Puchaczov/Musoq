using Musoq.Evaluator.Utils;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void CaptureBoundSourceTable(Parser.SchemaFromNode source, ISchemaTable table)
    {
        var columns = table.Columns;
        source.BoundTable = BoundTableContract.Capture(source.Alias, table, columns);
        _sourceBinding.InferredColumns.TryAdd(source, columns);
    }
}
