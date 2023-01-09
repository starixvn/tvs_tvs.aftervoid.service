using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.Caching;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services
{
    public interface VoidResultCacheService
    {
        Task<bool> IsExistVoidResult(string key);

        Task SetVoidResultCache(string key, IList<Result> results);
    }

    public class VoidResultCacheServiceImp : VoidResultCacheService
    {
        private readonly ICache cacheService;

        public VoidResultCacheServiceImp(ICache cacheService)
        {
            this.cacheService = cacheService;
        }

        public async Task<bool> IsExistVoidResult(string key)
        {
            return await cacheService.ContainAsync(key);
        }

        public async Task SetVoidResultCache(string key, IList<Result> results)
        {
            var cachePolicy = new CacheItemOptions().SetSlidingExpiration(TimeSpan.FromHours(1));
            await cacheService.SetAsync(key, results, cachePolicy);
        }
    }
}