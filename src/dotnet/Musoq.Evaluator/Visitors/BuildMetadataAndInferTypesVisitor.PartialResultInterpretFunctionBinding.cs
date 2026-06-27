namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static bool IsPartialResultInterpretFunction(string identifier)
    {
        return identifier.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase) ||
               identifier.Equals("PartialParse", StringComparison.OrdinalIgnoreCase);
    }
}
