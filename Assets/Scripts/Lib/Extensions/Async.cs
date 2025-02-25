using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable
namespace Extensions
{
    public static class AsyncExtensions
    {
        public static async Task ToTask(this AsyncOperation operation)
        {
            await operation;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                yield return value;
            }
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> values, CancellationToken cancellationToken = default)
        {
            List<T> result = new();
            await foreach (var item in values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(item);
            }
            return result;
        }
    }
}