using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

internal readonly record struct SourceArgumentBindingContext(SchemaFromNode Source, ISchema? Schema = null);

public partial class BuildMetadataAndInferTypesVisitor
{
    internal SourceArgumentBindingContext? SourceArgumentContext { get; set; }

    private MethodResolutionContext CreateSourceArgumentMethodContext(SchemaFromNode source)
    {
        var schema = ResolveSourceSchema(source);
        var alias = AliasGenerator.CreateAliasIfEmpty(source.Alias, _resultShape.GeneratedAliases,
            _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var table = new DynamicTable([]);
        var symbol = new TableSymbol(alias, schema, table, !string.IsNullOrEmpty(source.Alias));
        return new MethodResolutionContext(alias, symbol, (schema, table, alias), typeof(object), IsUnopenedSource: true);
    }

}
