using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

public class SetOperatorMustHaveSameTypesOfColumnsException(FieldNode left, FieldNode right) 
    : Exception($"Set operator must have the same types of columns in both queries. Left column expression is {left.ToString()} and right column expression is {right.ToString()}");