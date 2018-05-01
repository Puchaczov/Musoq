using System.Collections.Generic;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.Utils
{
    public class SymbolTable
    {
        private readonly Dictionary<object, Symbol> _symbols = new Dictionary<object, Symbol>();

        public void AddSymbol(object key, Symbol symbol)
        {
            _symbols.Add(key, symbol);
        }

        public void AddSymbolIfNotExist(object key, Symbol symbol)
        {
            if (!_symbols.ContainsKey(key))
                _symbols.Add(key, symbol);
        }

        public Symbol GetSymbol(object key)
        {
            return _symbols[key];
        }

        public TSymbol GetSymbol<TSymbol>(object key) where TSymbol : Symbol => (TSymbol) GetSymbol(key);

        public void UpdateSymbol(object oldKey, object newKey)
        {
            _symbols.Add(newKey, GetSymbol(oldKey));
            _symbols.Remove(oldKey);
        }

        public bool SymbolIsOfType<TType>(object key) where TType : Symbol =>
            _symbols.ContainsKey(key) && _symbols[key].GetType() == typeof(TType);
    }
}