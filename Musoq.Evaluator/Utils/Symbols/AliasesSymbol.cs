﻿using System.Collections.Generic;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Utils.Symbols;

public class AliasesSymbol : Symbol
{
    private readonly HashSet<string> _aliases = [];
    
    public void AddAlias(string alias)
    {
        _aliases.Add(alias);
    }
    
    public bool ContainsAlias(string alias)
    {
        return _aliases.Contains(alias);
    }
}