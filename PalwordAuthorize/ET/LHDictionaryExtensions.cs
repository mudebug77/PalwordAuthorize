using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public static class DictionaryExtensions
    {
        public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                dictionary.Remove(key);
                return true;
            }
            return false;
        }

        public static (TKey, TValue) ToValueTuple<TKey, TValue>(this KeyValuePair<TKey, TValue> pair)
        {
            return (pair.Key, pair.Value);
        }

    }
}
