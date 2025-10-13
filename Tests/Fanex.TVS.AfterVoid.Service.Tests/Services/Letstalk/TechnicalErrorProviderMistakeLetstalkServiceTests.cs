using System;
using System.Collections.Generic;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Client.Models;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Letstalk;

public class TechnicalErrorProviderMistakeLetstalkServiceTests : IClassFixture<AppSettingFixture>
{
    private readonly LetstalkClientService letstalkClientService;
    private readonly TechnicalErrorProviderMistakeLetstalkServiceImp service;

    public TechnicalErrorProviderMistakeLetstalkServiceTests()
    {
        letstalkClientService = Substitute.For<LetstalkClientService>();
        service = new TechnicalErrorProviderMistakeLetstalkServiceImp(letstalkClientService);
    }

    [Fact]
    public void Send_WhenNoTickets_ShouldReturnTrue()
    {
        // Arrange
        var model = new NotificationLetstalkModel();
        var tickets = new List<TicketEmailModel>();

        // Act
        var result = service.Send(model, tickets);

        // Assert
        Assert.True(result);
        letstalkClientService.DidNotReceiveWithAnyArgs().SendMessage(default);
    }

    [Fact]
    public void Send_WhenNonProdEnvironment_ShouldCreateCorrectLetstalkModel()
    {
        // Arrange
        var model = new NotificationLetstalkModel
        {
            RequestId = 123,
            Match = new Match
            {
                MatchId = 456,
                SportTypeName = "Football",
                EventDate = new DateTime(2024, 1, 15),
                KickOffTime = new DateTime(2024, 1, 15, 20, 0, 0),
                LeagueName = "Premier League",
                MatchEvent = "Team A vs Team B"
            },
            VoidFromTime = "19:00",
            VoidToTime = "20:00",
            TotalTicketsCount = 10
        };

        var tickets = Builder<TicketEmailModel>.CreateListOfSize(10).Build();
        letstalkClientService.SendMessage(default).ReturnsForAnyArgs(true);

        // Act
        service.Send(model, tickets);

        // Assert
        letstalkClientService.Received(1).SendMessage(Arg.Any<LetstalkModel>());
    }

    [Fact]
    public void Send_ShouldCreateCorrectMessageContent()
    {
        // Arrange
        var model = new NotificationLetstalkModel
        {
            RequestId = 123,
            Match = new Match
            {
                MatchId = 456,
                SportTypeName = "Football",
                EventDate = new DateTime(2024, 1, 15),
                KickOffTime = new DateTime(2024, 1, 15, 20, 0, 0),
                LeagueName = "Premier League",
                MatchEvent = "Team A vs Team B"
            },
            VoidFromTime = "19:00",
            VoidToTime = "20:00",
            TotalTicketsCount = 10
        };

        var tickets = Builder<TicketEmailModel>.CreateListOfSize(10).Build();
        letstalkClientService.SendMessage(default).ReturnsForAnyArgs(true);

        // Act
        service.Send(model, tickets);

        // Assert
        letstalkClientService.Received(1).SendMessage(Arg.Is<LetstalkModel>(x =>
            x.Content.Contains("[Football]") &&
            x.Content.Contains("TVS Request NO : 123") &&
            x.Content.Contains("Match ID: 456") &&
            x.Content.Contains("Event Date: 15 January 2024") &&
            x.Content.Contains("Kick Off: 8:00 PM") &&
            x.Content.Contains("League: Premier League") &&
            x.Content.Contains("Match: Team A vs Team B") &&
            x.Content.Contains("Void Reason: BC request VOID") &&
            x.Content.Contains("Total ticket: 10") &&
            x.Content.Contains("Void Time: 19:00 - 20:00")));
    }

    [Fact]
    public void Send_WhenEmptyTickets_ShouldHandleGracefully()
    {
        // Arrange
        var model = new NotificationLetstalkModel();
        var tickets = new List<TicketEmailModel>();

        // Act
        var result = service.Send(model, tickets);

        // Assert
        Assert.True(result);
        Assert.Equal(0, model.TotalTicketsCount);
        Assert.True(model.IsAllUusTicket);
    }

    [Fact]
    public void Send_WhenModelIsNull_ShouldThrowException()
    {
        // Arrange
        NotificationLetstalkModel model = null;
        var tickets = Builder<TicketEmailModel>.CreateListOfSize(5).Build();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => service.Send(model, tickets));
    }

    [Fact]
    public void Send_WhenMatchIsNull_ShouldHandleGracefully()
    {
        // Arrange
        var model = new NotificationLetstalkModel
        {
            RequestId = 123,
            Match = null,
            VoidFromTime = "19:00",
            VoidToTime = "20:00",
            TotalTicketsCount = 10
        };

        var tickets = Builder<TicketEmailModel>.CreateListOfSize(10).Build();
        letstalkClientService.SendMessage(default).ReturnsForAnyArgs(true);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => service.Send(model, tickets));
    }
}