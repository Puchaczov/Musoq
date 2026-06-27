using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplyFromNode(FromNode source, FromNode with, ApplyType applyType, bool withOrdinality = false)
    : Musoq.Parser.Nodes.From.ApplyFromNode(source, with, applyType, typeof(RowSource<>), withOrdinality);
