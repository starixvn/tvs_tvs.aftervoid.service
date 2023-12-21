using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Service.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.TicketVoidingSystem.Services;

public class SpecialPlatformServiceTests
{
    [Fact]
    public void GroupTicketSaba_HasOnlyNormalTickets_ReturnExpected()
    {
        // Arrange
        var tickets = new List<TicketEmailModel>
        {
            new() { LeagueGroupId = 42, IsChinaView = false, SpecialPlatform = 1 },
            new() { LeagueGroupId = 1, IsChinaView = true, SpecialPlatform = 1 },
            new() { LeagueGroupId = 1, IsChinaView = true, SpecialPlatform = SpecialPlatformConstant.JollySports },
            new() { LeagueGroupId = 1, IsChinaView = false, SpecialPlatform = SpecialPlatformConstant.JollySports }
        };
        var service = new SpecialPlatformServiceImp();

        // Act
        var (normalTickets, specialPlatformTickets) = service.GroupTicketSaba(tickets);

        // Assert
        AssertExtension.Equal(tickets, normalTickets);
        AssertExtension.Equal(new List<TicketEmailModel>(), specialPlatformTickets);
    }

    [Fact]
    public void GroupTicketSaba_HasNormalAndSpecialTickets_ReturnExpected()
    {
        // Arrange
        var tickets = new List<TicketEmailModel>
        {
            new() { LeagueGroupId = 42, IsChinaView = false, SpecialPlatform = 1 },
            new() { LeagueGroupId = 42, IsChinaView = true, SpecialPlatform = 1 },
            new() { LeagueGroupId = 42, IsChinaView = false, SpecialPlatform = SpecialPlatformConstant.JollySports },
            new() { LeagueGroupId = 1, IsChinaView = false, SpecialPlatform = SpecialPlatformConstant.JollySports }
        };
        var expectedNormalTickets = new List<TicketEmailModel>
        {
            new() { LeagueGroupId = 42, IsChinaView = false, SpecialPlatform = 1 },
            new() { LeagueGroupId = 1, IsChinaView = false, SpecialPlatform = SpecialPlatformConstant.JollySports }
        };
        var expectedSpecialTickets = new List<TicketEmailModel>
        {
            new() { LeagueGroupId = 42, IsChinaView = true, SpecialPlatform = 1 },
            new() { LeagueGroupId = 42, IsChinaView = false, SpecialPlatform = SpecialPlatformConstant.JollySports }
        };
        var service = new SpecialPlatformServiceImp();

        // Act
        var (normalTickets, specialPlatformTickets) = service.GroupTicketSaba(tickets);

        // Assert
        AssertExtension.Equal(expectedNormalTickets, normalTickets);
        AssertExtension.Equal(expectedSpecialTickets, specialPlatformTickets);
    }

    [Fact]
    public void GroupTicketSaba_HasOnlySpecialTickets_ReturnExpected()
    {
        // Arrange
        var tickets = new List<TicketEmailModel>
        {
            new() { LeagueGroupId = 42, IsChinaView = true, SpecialPlatform = 1 },
            new() { LeagueGroupId = 42, IsChinaView = false, SpecialPlatform = SpecialPlatformConstant.JollySports },
        };
        var service = new SpecialPlatformServiceImp();

        // Act
        var (normalTickets, specialPlatformTickets) = service.GroupTicketSaba(tickets);

        // Assert
        AssertExtension.Equal(tickets, specialPlatformTickets);
        AssertExtension.Equal(new List<TicketEmailModel>(), normalTickets);
    }

    [Fact]
    public void RemoveSabaInMessageContent_HasMatchIsNull_ReturnExpected()
    {
        // Arrange
        var message = new Message();
        var service = new SpecialPlatformServiceImp();

        // Act
        var actual = service.RemoveSabaInMessageContent(null, message);

        // Assert
        AssertExtension.Equal(message, actual);
    }

    [Fact]
    public void RemoveSabaInMessageContent_HasMatchNotNull_ReturnExpected()
    {
        // Arrange
        var match = new Match { OriginLeagueName = "saBa 123", OriginLeagueNameSimplified = "沙巴123" };
        var message = new Message { ContentEnglish = "aaa saBa 123 saBaa", ContentChineseSimplified = "aaa 沙巴123 沙巴12" };
        var expected = new Message { ContentEnglish = "aaa 123 saBaa", ContentChineseSimplified = "aaa 123 沙巴12" };
        var service = new SpecialPlatformServiceImp();

        // Act
        var actual = service.RemoveSabaInMessageContent(match, message);

        // Assert
        AssertExtension.Equal(expected, actual);
    }
}