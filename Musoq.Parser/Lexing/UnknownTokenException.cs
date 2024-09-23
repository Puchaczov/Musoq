using System;

namespace Musoq.Parser.Lexing;

public class UnknownTokenException : Exception
{
    public UnknownTokenException(int position, char c, string s)
        : base($"Token '{c}' that starts at position {position} was unrecognized. Rest of the unparsed query is '{s}'")
    {
    }
}