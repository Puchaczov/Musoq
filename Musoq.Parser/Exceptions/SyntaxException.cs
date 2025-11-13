using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Helpers;

namespace Musoq.Parser.Exceptions;

public class SyntaxException : Exception
{
    public string QueryPart { get; }
    public IReadOnlyList<string> Suggestions { get; }

    public SyntaxException(string message, string queryPart) : base(message)
    {
        QueryPart = queryPart;
        Suggestions = new List<string>();
    }

    public SyntaxException(string message, string queryPart, Exception innerException) : base(message, innerException)
    {
        QueryPart = queryPart;
        Suggestions = ExtractSuggestionsFromInnerException(innerException);
    }

    public SyntaxException(string message, string queryPart, IEnumerable<string> suggestions) 
        : base(BuildMessageWithSuggestions(message, suggestions))
    {
        QueryPart = queryPart;
        Suggestions = suggestions?.ToList() ?? new List<string>();
    }

    private static List<string> ExtractSuggestionsFromInnerException(Exception innerException)
    {
        if (innerException is Lexing.UnknownTokenException unknownTokenEx)
        {
            return unknownTokenEx.Suggestions.ToList();
        }

        return new List<string>();
    }

    private static string BuildMessageWithSuggestions(string message, IEnumerable<string> suggestions)
    {
        var suggestionList = suggestions?.ToList();
        if (suggestionList == null || !suggestionList.Any())
            return message;

        return $"{message}\n\nDid you mean one of these keywords?\n  - {string.Join("\n  - ", suggestionList)}";
    }
}