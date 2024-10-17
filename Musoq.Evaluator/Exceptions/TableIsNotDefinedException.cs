using System;

namespace Musoq.Evaluator.Exceptions;

public class TableIsNotDefinedException(string table)
    : Exception($"Table {table} is not defined in query");