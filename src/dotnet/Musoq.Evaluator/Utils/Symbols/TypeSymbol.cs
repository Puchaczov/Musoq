using System;

namespace Musoq.Evaluator.Utils.Symbols;

public class TypeSymbol : Symbol
{
    public TypeSymbol(Type type)
    {
        Type = type;
    }

    public Type Type { get; }
}
