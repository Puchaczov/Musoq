using System;
using System.Collections.Generic;
using System.Text;
using FQL.Parser.Tokens;

namespace FQL.Parser.Nodes
{
    public class NumericAccessToken : Token
    {
        public NumericAccessToken(string name, string value, TextSpan span) 
            : base(value, TokenType.NumericAccess, span)
        {
            Index = int.Parse(value);
            Name = name;
        }

        public int Index { get; }

        public string Name { get; }
    }

    public class KeyAccessToken : Token
    {
        public KeyAccessToken(string name, string value, TextSpan span) 
            : base(value, TokenType.KeyAccess, span)
        {
            Key = value;
            Name = name;
        }

        public string Key { get; }

        public string Name { get; }
    }

    public class AccessPropertyToken : Token
    {
        public AccessPropertyToken(string value, TextSpan span) 
            : base(value, TokenType.Property, span)
        {
            Name = value;
        }

        public string Name { get; }
    }
}
