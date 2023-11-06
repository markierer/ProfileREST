using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.DocumentService;

/// <summary>
/// ExtensionMethods
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// ToDictionary
    /// </summary>
    /// <param name="col">NameValueCollection</param>
    /// <returns>IDictionary</returns>
    public static IDictionary<string, string?> ToDictionary(this NameValueCollection col)
    {
        return col.Cast<string>().ToDictionary(k => k, k => col[k]);
    }

    /// <summary>
    /// ToDictionary
    /// </summary>
    /// <param name="dictionary">IDictionary</param>
    /// <returns>NameValueCollection</returns>
    public static NameValueCollection ToDictionary(this IDictionary<string, string> dictionary)
    {
        return dictionary.Aggregate(new NameValueCollection(), (seed, current) =>
        {
            seed.Add(current.Key, current.Value);
            return seed;
        });
    }

    /// <summary>
    /// IndexOf
    /// </summary>
    /// <param name="dictionary">IDictionary</param>
    /// <param name="key">Key of position</param>
    /// <returns>Position index</returns>
    public static int IndexOf<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        int i = 0;
        foreach (var pair in dictionary)
        {
            if (pair.Key is null)
            {
                break;
            }
            else if (pair.Key.Equals(key))
            {
                return i;
            }
            i++;
        }
        return -1;
    }

    /// <summary>
    /// IndexOf
    /// </summary>
    /// <param name="dictionary">IDictionary</param>
    /// <param name="value">Value of position</param>
    /// <returns>Position index</returns>
    public static int IndexOf<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
    {
        int i = 0;
        foreach (var pair in dictionary)
        {
            if (pair.Value is null)
            {
                break;
            }
            else if (pair.Value.Equals(value))
            {
                return i;
            }
            i++;
        }
        return -1;
    }

    /// <summary>
    /// Clear
    /// </summary>
    /// <param name="col">BlockingCollection</param>
    public static void Clear(this BlockingCollection<string> col)
    {
        while (col.Count > 0)
        {
            col.Take();
        }
    }
}
