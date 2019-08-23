using System;
using System.Collections.Generic;
using System.Text;

public static class DictionaryExtensions
{
    /// <summary>
    /// Returns the value associated with the given key, creating a new
    /// entry if none exists.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        where TValue : new()
    {
        if (!dict.ContainsKey(key))
            dict.Add(key, new TValue());

        return dict[key];
    }
}
