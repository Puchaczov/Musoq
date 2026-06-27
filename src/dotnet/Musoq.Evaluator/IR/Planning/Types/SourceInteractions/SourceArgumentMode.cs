using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal enum SourceArgumentMode
{
    ConstantArguments,
    SourceLocalArguments,
    CorrelatedArguments,
    Unknown
}
