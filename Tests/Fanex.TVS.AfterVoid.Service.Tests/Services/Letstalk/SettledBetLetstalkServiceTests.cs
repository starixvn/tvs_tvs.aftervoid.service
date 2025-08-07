using System;
using System.Collections.Generic;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Client.Models;
using Fanex.RemotingRef;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Letstalk;

public class SettledBetLetstalkServiceTests : IClassFixture<AppSettingFixture>
{
    private static void SetupTestEnv()
    {
        RefDataTypeContainer
            .ConnectTo(new Uri("http://remotingref-api-core.nexdev.net/"))
            .FromApplication("NAP-SpecialDelete-LOCAL")
            .RegisterSupportedLanguages("en-US", "zh-TW", "zh-CN");
    }

    [Fact]
    public void SendToSettledBetGroup_ShouldNotRunCreateTemplate()
    {
        var letstalkService = Substitute.For<LetstalkClientService>();
        var service = new SettledBetLetstalkServiceImp(letstalkService);

        service.Send(new NotificationLetstalkModel(), new List<TicketEmailModel>());

        letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
    }

    [Fact]
    public void SendToSettledBetGroup_WhenRequestHaveUsername_ShouldNotRunLetstalkService()
    {
        var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(5)
            .All().With(x => x.IsCashOut = false).Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        var service = new SettledBetLetstalkServiceImp(letstalkService);

        service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
    }

    [Fact]
    public void SendToSettledBetGroup_WhenRequestHaveUsername_ShouldRunLetstalkServiceFail()
    {
        var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
            .Random(2).With(x => x.IsCashOut = true)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
            .Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        letstalkService.SendMessage(default).ReturnsForAnyArgs(false);
        var service = new SettledBetLetstalkServiceImp(letstalkService);

        var actual = service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
        Assert.False(actual);
    }

    [Fact]
    public void SendToSettledBetGroup_WhenRequestHaveUsername_ShouldRunLetstalkServiceSuccessMultipleTimes()
    {
        var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(200)
            .Random(40).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
            .Random(40).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
            .Random(40).With(x => x.IsCashOut = true)
            .Random(40).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
            .Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        letstalkService.SendMessage(default).ReturnsForAnyArgs(true);
        var service = new SettledBetLetstalkServiceImp(letstalkService);

        var actual = service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.ReceivedWithAnyArgs(2).SendMessage(default);
        Assert.True(actual);
    }

    [Fact]
    public void SendToSettledBetGroup_WhenRequestNotHaveUsername_ShouldNotRunLetstalkService()
    {
        var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = false).Build();
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(5)
            .All().With(x => x.IsCashOut = false).Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        var service = new SettledBetLetstalkServiceImp(letstalkService);

        service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
    }

    [Fact]
    public void SendToSettledBetGroup_WhenRequestNotHaveUsername_ShouldRunLetstalkServiceSuccess()
    {
        SetupTestEnv();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = false).Build();
        fakeNotificationLetstalkModel.Match = fakeMatch;
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.IsCancelCashOut = false)
            .And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
            .Random(2).With(x => x.IsCashOut = true)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
            .Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        letstalkService.SendMessage(default).ReturnsForAnyArgs(true);
        var service = new SettledBetLetstalkServiceImp(letstalkService);

        var actual = service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.ReceivedWithAnyArgs(1).SendMessage(Arg.Any<LetstalkModel>());
        Assert.True(actual);
    }
}