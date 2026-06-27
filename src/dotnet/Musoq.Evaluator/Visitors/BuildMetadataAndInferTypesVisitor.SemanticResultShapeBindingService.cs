using System.Globalization;
using Musoq.Evaluator.Resources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private sealed class SemanticResultShapeBindingService(ResultShapeState resultShape)
    {
        public string CreateAlias(string alias, int schemaFromKey)
        {
            return AliasGenerator.CreateAliasIfEmpty(
                alias,
                resultShape.GeneratedAliases,
                schemaFromKey.ToString(CultureInfo.InvariantCulture));
        }

        public void RegisterAlias(string alias)
        {
            resultShape.GeneratedAliases.Add(alias);
        }
    }
}
