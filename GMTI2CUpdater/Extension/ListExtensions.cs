using System;
using System.Collections.Generic;
using System.Linq;

public static class ListExtensions
{
    /// <summary>
    /// 將 items 加到 list，但以 Name (或指定 key) 去避免重複。
    /// </summary>
    public static void AddRangeDistinctBy<T, TKey>(
        this IList<T> list,
        IEnumerable<T> items,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        // 先把 list 裡面現有的 key 收集起來
        var existingKeys = new HashSet<TKey>(list.Select(keySelector), comparer);

        // 只加入 key 不重複的
        foreach (var item in items)
        {
            var key = keySelector(item);
            if (existingKeys.Add(key)) // Add 回傳 true 代表之前沒有這個 key
            {
                list.Add(item);
            }
        }
    }
}
