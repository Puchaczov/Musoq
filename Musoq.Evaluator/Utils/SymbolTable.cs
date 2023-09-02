using System.Collections.Generic;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.Utils
{
    public class SymbolTable
    {
        private readonly Dictionary<object, Symbol> _symbols = new();

        public void AddSymbol(object key, Symbol symbol)
        {
            _symbols.Add(key, symbol);
        }

        public void AddSymbolIfNotExist(object key, Symbol symbol)
        {
            _symbols.TryAdd(key, symbol);
        }

        public Symbol GetSymbol(object key)
        {
            return _symbols[key];
        }

        public TSymbol GetSymbol<TSymbol>(object key) where TSymbol : Symbol
        {
            return (TSymbol) GetSymbol(key);
        }

        public void MoveSymbol(object oldKey, object newKey)
        {
            _symbols.Add(newKey, GetSymbol(oldKey));
            _symbols.Remove(oldKey);
        }

        public void UpdateSymbol(object oldKey, Symbol symbol)
        {
            _symbols.Remove(oldKey);
            _symbols.Add(oldKey, symbol);
        }

        public bool SymbolIsOfType<TType>(object key) where TType : Symbol
        {
            return _symbols.ContainsKey(key) && _symbols[key].GetType() == typeof(TType);
        }
    }
}