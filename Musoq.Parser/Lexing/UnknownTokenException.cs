using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Lexing;

public class UnknownTokenException : Exception
{
    public int Position { get; }
    public string UnparsedQuery { get; }
    public IReadOnlyList<string> Suggestions { get; }

    public UnknownTokenException(int position, char c, string s)
        : base($"Token '{c}' that starts at position {position} was unrecognized. Rest of the unparsed query is '{s}'")
    {
        Position = position;
        UnparsedQuery = s;
        Suggestions = new List<string>();
    }

    public UnknownTokenException(int position, string unparsedQuery, IEnumerable<string> suggestions = null)
        : base(BuildMessage(position, unparsedQuery, suggestions))
    {
        Position = position;
        UnparsedQuery = unparsedQuery;
        Suggestions = suggestions?.ToList() ?? new List<string>();
    }

    private static string BuildMessage(int position, string unparsedQuery, IEnumerable<string> suggestions)
    {
        var message = $"Unrecognized token at position {position}. Rest of the unparsed query: '{unparsedQuery}'";
        
        var suggestionList = suggestions?.ToList();
        if (suggestionList != null && suggestionList.Any())
        {
            message += $"\n\nDid you mean one of these keywords?\n  - {string.Join("\n  - ", suggestionList)}";
        }

        return message;
    }
}