using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplyNode(ApplyFromNode applies) : Musoq.Parser.Nodes.From.ApplyNode(applies, typeof(RowSource<>));
