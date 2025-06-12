using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services;

public class MessageContentParameterServiceTests
{
    private readonly TVSWebSiteHttpClientService tvsWebSiteHttpClientService;
    private readonly MessageContentParameterService service;

    public MessageContentParameterServiceTests()
    {
        tvsWebSiteHttpClientService = Substitute.For<TVSWebSiteHttpClientService>();
        service = new MessageContentParameterServiceImp(tvsWebSiteHttpClientService);
    }

    [Fact]
    public void GetPlayerNamesSelected_IssueTypeIsNotParticipate_ReturnPlayerNamesSelected()
    {
        // Arrange
        var playerName = new PlayerName
        {
            PlayerNamesEnglish = "A",
            PlayerNamesChineseSimplified = "B",
            PlayerNamesChineseTraditional = "C"
        };
        var expected = new ContentByLanguageModel
        {
            ContentEnglish = "A",
            ContentChineseSimplified = "B",
            ContentNewChineseSimplified = "B",
            ContentChineseTraditional = "C"
        };

        // Act
        var actual = service.GetPlayerNamesSelected(IssueTypeConstant.NotParticipate, playerName);

        // Assert
        AssertExtension.Equal(expected, actual);
    }

    [Fact]
    public void GetPlayerNamesSelected_IssueTypeNotIsNotParticipate_ReturnNull()
    {
        // Arrange
        var playerName = new PlayerName
        {
            PlayerNamesEnglish = "A",
            PlayerNamesChineseSimplified = "B",
            PlayerNamesChineseTraditional = "C"
        };

        // Act
        var actual = service.GetPlayerNamesSelected(IssueTypeConstant.DangerAccepted, playerName);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void IsAllLiveTickets_AllTicketsIsLive_ReturnTrue()
    {
        // Arrange
        List<TicketEmailModel> normalTickets = [new() { IsLive = true, TransId = 1 }, new() { IsLive = true, TransId = 2 }];
        List<TicketEmailModel> specialTickets = [new() { IsLive = true, TransId = 3 }, new() { IsLive = true, TransId = 4 }];

        // Act
        var actual = service.IsAllLiveTickets(normalTickets, specialTickets);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void IsAllLiveTickets_HasTicketsIsNotLive_ReturnFalse()
    {
        // Arrange
        List<TicketEmailModel> normalTickets = [new() { IsLive = false, TransId = 1 }, new() { IsLive = true, TransId = 2 }];
        List<TicketEmailModel> specialTickets = [new() { IsLive = false, TransId = 3 }, new() { IsLive = true, TransId = 4 }];

        // Act
        var actual = service.IsAllLiveTickets(normalTickets, specialTickets);

        // Assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData(IssueTypeConstant.WrongMarketOffered)]
    [InlineData(IssueTypeConstant.TechnicalError)]
    public async Task GetSpecialBetTypeContent_HasOnlyBetType_ReturnContentSpecialBetTypeExpected(int issueTypeId)
    {
        // Arrange
        List<TicketEmailModel> normalTickets = [new() { IsMixParlay = false, TransId = 1, BetTypeId = 1 }, new() { IsMixParlay = true, TransId = 2, SubBetType = 1 }];
        List<TicketEmailModel> specialTickets = [new() { IsMixParlay = false, TransId = 3, BetTypeId = 1 }, new() { IsMixParlay = true, TransId = 4, SubBetType = 1 }];
        var expected = new ContentByLanguageModel
        {
            ContentEnglish = "A",
            ContentChineseSimplified = "B",
            ContentChineseTraditional = "C",
            ContentNewChineseSimplified = "C"
        };
        var model = new VoidBetAutomationInfo
        {
            RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = issueTypeId },
                EventByMatch = new RequestEventModel { SubIssueTypeId = 0 }
            }
        };
        tvsWebSiteHttpClientService.GetSpecialBetTypeContent(Arg.Any<IEnumerable<TicketEmailModel>>()).Returns(expected);

        // Act
        var actual = await service.GetSpecialBetTypeContent(model, normalTickets, specialTickets);

        // Assert
        AssertExtension.Equal(expected, actual);
    }

    [Fact]
    public async Task GetSpecialBetTypeContent_HasMoreBetType_ReturnNewContentSpecialBetType()
    {
        // Arrange
        List<TicketEmailModel> normalTickets = [new() { IsMixParlay = false, TransId = 1, BetTypeId = 1 }, new() { IsMixParlay = true, TransId = 2, SubBetType = 2 }];
        List<TicketEmailModel> specialTickets = [new() { IsMixParlay = false, TransId = 3, BetTypeId = 1 }, new() { IsMixParlay = true, TransId = 4, SubBetType = 1 }];
        var expected = new ContentByLanguageModel();
        var model = new VoidBetAutomationInfo
        {
            RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.WrongMarketOffered },
                EventByMatch = new RequestEventModel { SubIssueTypeId = SubIssueTypeConstant.TechnicalError_ProviderMistake }
            }
        };
        tvsWebSiteHttpClientService.GetSpecialBetTypeContent(Arg.Any<IEnumerable<TicketEmailModel>>()).Returns(expected);

        // Act
        var actual = await service.GetSpecialBetTypeContent(model, normalTickets, specialTickets);

        // Assert
        AssertExtension.Equal(expected, actual);
    }

    [Fact]
    public async Task GetSpecialBetTypeContent_IssueTypeNotWrongMarketOffered_ReturnNewContentSpecialBetType()
    {
        // Arrange
        List<TicketEmailModel> normalTickets = [new() { IsMixParlay = false, TransId = 1, BetTypeId = 1 }, new() { IsMixParlay = true, TransId = 2, SubBetType = 2 }];
        List<TicketEmailModel> specialTickets = [new() { IsMixParlay = false, TransId = 3, BetTypeId = 1 }, new() { IsMixParlay = true, TransId = 4, SubBetType = 1 }];
        var expected = new ContentByLanguageModel();
        var model = new VoidBetAutomationInfo
        {
            RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { SubIssueTypeId = 0 }
            }
        };
        // Act
        var actual = await service.GetSpecialBetTypeContent(model, normalTickets, specialTickets);

        // Assert
        AssertExtension.Equal(expected, actual);
    }
}