

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts.System.Combiner;
using UnityEngine;
using Random = System.Random;

namespace Internal.Scripts.System
{
    public static class LinqUtils
    {
       public static bool AllNull<T>(this IEnumerable<T> seq)
{
    foreach (var item in seq)
    {
        if (item != null && !item.Equals(null))
        {
            return false;
        }
    }
    return true;
}


      public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> seq)
{
    foreach (var item in seq)
    {
        if (item != null && !item.Equals(null))
        {
            yield return item;
        }
    }
}

     public static T TryGetElement<T>(this IEnumerable<T> seq, int index, T ifnull = default(T))
{
    int count = 0;
    foreach (var element in seq)
    {
        if (count == index)
        {
            return element;
        }
        count++;
    }
    return ifnull;
}


     public static IEnumerable<T> ForEachY<T>(this IEnumerable<T> seq, Action<T> action)
{
    foreach (var item in seq)
    {
        action(item);
    }
    return seq;
}

    
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            Random rnd = new Random();
            return source.OrderBy<T, int>((item) => rnd.Next());
        }


        public static TemplateOfItem GetElement(this TemplateOfItem[] seq, int x, int y)
        {
            return seq.First(i => i.position == new Vector2(x, y));
        }
    
       public static T ElementAtOrDefault<T>(this IList<T> list, int index, T @default)
{
    return index >= 0 && index < list.Count ? list[index] : @default;
}

    
        public static T Addd<T>(this List<T> list, T newItem)
        {
            list.Add(newItem);
            return newItem;
        }
    public static T NextRandom<T>(this IEnumerable<T> source)
{
    List<T> list = new List<T>(source);
    Random gen = new Random((int)DateTime.Now.Ticks);
    int index = gen.Next(0, list.Count);

    return list[index];
}


        public static IEnumerable<T> SelectRandom<T>(this IEnumerable<T> source)
        {
            List<T> Remaining = new List<T>(source);
            while (Remaining.Count >= 1)
            {
                T temp = NextRandom(Remaining);
                Remaining.Remove(temp);
                yield return temp;
            }
        }
        
   public static IEnumerable<TSource> DistinctBy<TSource, TKey>
    (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
{
    HashSet<TKey> seenKeys = new HashSet<TKey>();

    foreach (TSource element in source)
    {
        if (seenKeys.Add(keySelector(element)))
        {
            yield return element;
        }
    }
}

    }
}