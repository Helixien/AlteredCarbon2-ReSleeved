using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace PawnSnapshot
{
    public static class CopyListHelper
    {
        public static List<T> CopyList<T>(this List<T> list)
        {
            if (list is null) return new List<T>();
            return list.ListFullCopy();
        }

        public static Dictionary<K, V> CopyDict<K, V>(this Dictionary<K, V> dict)
        {
            if (dict is null) return new Dictionary<K, V>();
            return dict.ToDictionary(x => x.Key, x => x.Value);
        }

        public static void CleanupList<T>(this List<T> list, Predicate<T> predicate = null)
        {
            if (list is null) return;

            if (predicate is null)
            {
                predicate = (x => x.IsNullValue());
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                }
            }
        }

        public static void CleanupDict<K, V>(this Dictionary<K, V> dict, Predicate<KeyValuePair<K, V>> predicate = null)
        {
            if (dict is null) return;
            if (predicate is null)
            {
                predicate = (x => x.Key.IsNullValue() || x.Value.IsNullValue());
            }
            dict.RemoveAll(predicate);
        }

        public static bool IsNullValue<T>(this T obj)
        {
            var isNull = obj is null;
            if (!isNull)
            {
                var field = typeof(T).GetField("def");
                if (field != null)
                {
                    if (field.GetValue(obj) is null)
                    {
                        isNull = true;
                    }
                }
            }
            return isNull;
        }

        public static T Clone<T>(this T obj)
        {
            var inst = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)inst?.Invoke(obj, null);
        }

        public static void CopyFrom(this OutfitForcedHandler dest, OutfitForcedHandler src)
        {
            dest.ForcedApparel.Clear();
            if (src != null)
            {
                foreach (var apparel in src.ForcedApparel)
                {
                    dest.ForcedApparel.Add(apparel);
                }
            }
        }
    }
}
