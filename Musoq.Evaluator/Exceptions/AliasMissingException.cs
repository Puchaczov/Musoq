using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

public class AliasMissingException(AccessMethodNode node)
    : Exception($"Alias must be provided for method call when more than one schema is used. Problem occurred in method {node.ToString()}");