using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncCacheManagerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: Implement sample
        }
    }

    // Sources can be found here: 
    // https://github.com/MyToolkit/MyToolkit/blob/master/src/MyToolkit/Data/AsyncCacheManager.cs

    /// <summary>A cache manager with supports asynchronous, task based item creation functions.</summary>
    /// <typeparam name="TKey">The type of the key/identifier of an item.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public class AsyncCacheManager<TKey, TItem>
    {
        private readonly object _lock = new object();

        private readonly Dictionary<TKey, Task<TItem>> _cache = new Dictionary<TKey, Task<TItem>>();

        /// <summary>Gets an existing item or asynchronously creates a new one.</summary>
        /// <param name="key">The key of the item.</param>
        /// <param name="itemCreator">The item creator.</param>
        /// <returns>The item.</returns>
        public Task<TItem> GetOrCreateAsync(TKey key, Func<Task<TItem>> itemCreator)
        {
            lock (_lock)
            {
                if (!_cache.ContainsKey(key))
                {
                    var task = itemCreator();
                    _cache[key] = task;
                    return task;
                }

                return _cache[key];
            }
        }
    }
}
