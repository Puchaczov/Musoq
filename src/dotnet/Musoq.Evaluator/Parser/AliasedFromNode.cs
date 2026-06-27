using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class AliasedFromNode(
    string identifier,
    ArgsListNode args,
    string alias,
    int inSourcePosition,
    string? typeParameter = null)
    : Musoq.Parser.Nodes.From.AliasedFromNode(identifier, args, alias, typeof(RowSource<>), inSourcePosition,
        typeParameter);
