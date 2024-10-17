using System;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Exceptions;

public class AliasAlreadyUsedException(SchemaFromNode node, string alias)
    : Exception($"Alias {alias} is already used in query. Please, use different alias. Problem occurred in schema from node {node.ToString()}");