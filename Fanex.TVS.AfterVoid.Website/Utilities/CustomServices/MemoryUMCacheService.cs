using System;
using Microsoft.Extensions.Caching.Memory;
using ONEbook.UM.Helpers;

namespace Fanex.TVS.AfterVoid.Website.Utilities.CustomServices;

public class MemoryUMCacheService : IUMCacheService
{
    private readonly IMemoryCache memoryCache;

    public MemoryUMCacheService(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    public T Get<T>(string key) => memoryCache.Get<T>(key)
    ;

    public void Set<T>(string key, T value, TimeSpan absoluteExpiration)
        => memoryCache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(absoluteExpiration));
}