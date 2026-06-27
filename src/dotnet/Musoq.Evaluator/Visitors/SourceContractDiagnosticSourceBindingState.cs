using System.Collections.Generic;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private sealed partial record SourceBindingState
    {
        public Dictionary<string, SourceContractDiagnosticLocationMap> ExplicitlyDefinedTableDiagnosticLocations { get; } =
            new();

        public Dictionary<SchemaFromNode, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsPerSchema { get; } =
            new();
    }
}
