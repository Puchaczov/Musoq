using System;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
/// Exception thrown when a query expression has an invalid (non-primitive) type.
/// </summary>
public class InvalidQueryExpressionTypeException : Exception
{
    public InvalidQueryExpressionTypeException(string expressionDescription, Type invalidType, string context)
        : base($"Expression '{expressionDescription}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. " +
               "Only primitive types (numeric, string, bool, char, DateTime, DateTimeOffset, Guid, TimeSpan, decimal, null) are allowed in query expressions.")
    {
    }
    
    public InvalidQueryExpressionTypeException(FieldNode field, Type invalidType, string context)
        : base($"Query output column '{field.FieldName}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. " +
               "Only primitive types (numeric, string, bool, char, DateTime, DateTimeOffset, Guid, TimeSpan, decimal, null) are allowed in query outputs.")
    {
    }
}
