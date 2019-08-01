using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Musoq.Schema.Xml
{
    public class DynamicElement : DynamicObject, IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // We don't care about the return value...
            _values.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _values[binder.Name] = value;
            return true;
        }

        public ICollection<string> Keys => _values.Keys;

        public ICollection<object> Values => _values.Values;

        public int Count => _values.Count;

        public bool IsReadOnly => true;

        public object this[string key]
        {
            get
            {
                if (_values.ContainsKey(key))
                    return _values[key];

                return null;
            }

            set => _values[key] = value; }

        public void Add(string key, object value)
        {
            _values.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _values.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _values.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _values.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _values.ContainsKey(item.Key) && _values[item.Key].Equals(item.Value);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {

        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return Values.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
