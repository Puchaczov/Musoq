using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    internal IReadOnlyDictionary<SchemaFromNode, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsPerSchema =>
        _sourceBinding.SourceContractDiagnosticLocationsPerSchema.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);
}
