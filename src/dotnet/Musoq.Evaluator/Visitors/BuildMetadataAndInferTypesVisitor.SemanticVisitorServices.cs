namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly SemanticDiagnosticReporter _diagnosticReporter;
    private readonly SemanticMethodBindingService _methodBindingService;
    private readonly SemanticQueryValidationService _queryValidationService;
    private readonly SemanticResultShapeBindingService _resultShapeBindingService;
    private readonly SemanticSourceBindingService _sourceBindingService;
}
