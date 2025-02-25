using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;

#nullable enable
namespace Extensions
{
    public static class CollectionsUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(this IList<T> list, System.Index indexA, System.Index indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static int Count<T>(this IEnumerable<T> enumerable, T item)
        {
            int count = 0;
            foreach (var itemA in enumerable)
            {
                if (itemA?.Equals(item) ?? false) count++;
            }
            return count;
        }

        public static Dictionary<T, int> Counts<T>(this IEnumerable<T> enumerable)
        {
            Dictionary<T, int> result = new();
            foreach (var item in enumerable)
            {
                if (result.ContainsKey(item)) result[item]++;
                else result[item] = 1;
            }
            return result;
        }

        public static IEnumerable<List<T>> InSetsOf<T>(this IEnumerable<T> data, int maxSetSize)
        {
            List<T> group = new();
            foreach (var item in data)
            {
                group.Add(item);
                if (group.Count > maxSetSize)
                {
                    yield return group;
                    group = new();
                }
            }
            if (group.Count > 0)
            {
                yield return group;
            }
        }

        public static int FindIndex<T>(this IEnumerable<T> data, T? value) {
            int idx = 0;
            if (value == null)
            {
                foreach (var item in data)
                {
                    if (item == null) return idx;
                    idx++;
                }
            }
            else
            {
                foreach (var item in data)
                {
                    if (item?.Equals(value) ?? false) return idx;
                    idx++;
                }
            }
            
            return -1;
        }
        
        public static bool HasDuplicates<T>(this IEnumerable<T> data, [NotNullWhen(true)] out T? duplicate)
        {
            HashSet<T> found = new();
            foreach (var item in data)
            {
                if (item == null) continue;
                if (found.Contains(item)) { duplicate = item; return true; }
                found.Add(item);
            }
            duplicate = default;
            return false;
        }

        public static IEnumerable<T> InterleavedZip<T>(this IEnumerable<IEnumerable<T>> sources)
        {
            var enumerators = sources.Select(a => a.GetEnumerator()).ToArray();
            try
            {
                while (enumerators.Any(e => e.MoveNext()))
                {
                    foreach (var e in enumerators)
                    {
                        if (e.Current != null)
                        {
                            yield return e.Current;
                        }
                    }
                }
            }
            finally
            {
                foreach (var e in enumerators)
                {
                    e.Dispose();
                }
            }
        }

        public static IEnumerable<(T, K)> Zip<T, K>(this IEnumerable<T> first, IEnumerable<K> second)
        {
            return first.Zip(second, (a, b) => (a, b));
        }

        public static IEnumerable<(T, K)> ZipAround<T, K>(this IEnumerable<T> elements, IList<K> loopAround)
        {
            if (loopAround == null || loopAround.Count == 0)
            {
                throw new ArgumentException("Cannot ZipLoop with an empty or null loopAround collection.", nameof(loopAround));
            }

            int loopLength = loopAround.Count;

            int index = 0;
            foreach (var element in elements)
            {
                yield return (element, loopAround[index]);
                index = (index + 1) % loopLength; // Cycle back to the start if needed
            }
        }

        public static IEnumerable<(T,K)> ZipAround<T,K>(this IEnumerable<T> elements, IEnumerable<K> loopAround)
        {
            return elements.Zip(loopAround.Loop());
        }

        public static IEnumerable<T> Loop<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return LoopInternal(source);
        }

        private static IEnumerable<T> LoopInternal<T>(IEnumerable<T> source)
        {
            List<T> cache = new();
            foreach (var item in source)
            {
                cache.Add(item);
                yield return item;
            }

            if (cache.Count == 0) throw new ArgumentException($"Loop source cannot be empty", nameof(source));

            while (true)
            {
                foreach (var item in cache)
                {
                    yield return item;
                }
            }
        }
    }
}