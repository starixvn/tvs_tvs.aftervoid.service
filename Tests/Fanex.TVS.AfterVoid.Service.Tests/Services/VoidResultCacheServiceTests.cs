using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.Caching;
using Fanex.TVS.AfterVoid.Service.Services;
using NSubstitute;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services;

public class VoidResultCacheServiceTests
{
    [Fact]
    public async Task IsExistVoidResult_ReturnTrue()
    {
        var cacheService = Substitute.For<ICache>();
        cacheService.ContainAsync(default).ReturnsForAnyArgs(true);
        var service = new VoidResultCacheServiceImp(cacheService);

        var actual = await service.IsExistVoidResult("");

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public async Task SetVoidResultCache_ExecuteDone()
    {
        var cacheService = Substitute.For<ICache>();
        var service = new VoidResultCacheServiceImp(cacheService);

        await service.SetVoidResultCache(null, null);

        // Assert
        await cacheService.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<IList<Result>>(), Arg.Any<CacheItemOptions>());
    }
}