using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.LetstalkService.Client;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Letstalk;

public class AbnormalBetLetstalkServiceTests
{
    [Fact]
    public async Task SendToAbnormalBetGroup_ShouldNotRunCreateTemplate()
    {
        var letstalkService = Substitute.For<LetstalkClientService>();
        var sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        var service = new AbnormalBetLetstalkServiceImp(sendLetstalkServiceStage, letstalkService);

        await service.Send(default, []);

        letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
    }

    [Fact]
    public async Task SendToAbnormalBetGroup_ShouldReturnTrueWhenHaveNotContent()
    {
        AppSettingService.AppSetting = new AppSettingModel 
        { 
            LetstalkConfig = new LetstalkConfig 
            { 
                IsProdEnv = true, 
                TargetName = new LetstalkTargetName 
                { 
                    TicketVoidingSystem = "Ticket-Voiding-System",
                    BCAutoVoid = ""
                } 
            } 
        };
        var fakeTicketEmail = new List<TicketEmailModel>
        {
            new(){ IsInstantSettlementTicket = true, BetTypeId = 1, IsCashOut = false },
            new(){ IsInstantSettlementTicket = false, BetTypeId = 1, IsCashOut = false }
        };
        var letstalkService = Substitute.For<LetstalkClientService>();
        var sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        var service = new AbnormalBetLetstalkServiceImp(sendLetstalkServiceStage, letstalkService);

        var actual = await service.Send(new NotificationLetstalkModel(), fakeTicketEmail);

        Assert.True(actual);
    }

    [Fact]
    public async Task SendToAbnormalBetGroup_ShouldReturnTrue()
    {
        AppSettingService.AppSetting = new AppSettingModel 
        { 
            LetstalkConfig = new LetstalkConfig 
            { 
                IsProdEnv = false, 
                TargetName = new LetstalkTargetName 
                { 
                    TicketVoidingSystem = "Ticket-Voiding-System",
                    BCAutoVoid = ""
                } 
            } 
        };
        var fakeNotificationLetstalkModel =
            Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
        fakeNotificationLetstalkModel.IssueTypeId = 11;
        fakeNotificationLetstalkModel.FilterBy = 1;
        fakeNotificationLetstalkModel.RequestId = 123;
        fakeNotificationLetstalkModel.RequestedBy = "TestUser";
        fakeNotificationLetstalkModel.UserName = "VoidUser";
        fakeNotificationLetstalkModel.Match = new Match
        {
            MatchId = 123,
            EventDate = DateTime.Now,
            KickOffTime = DateTime.Now,
            LeagueName = "Test League",
            MatchEvent = "Team A vs Team B"
        };
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false)
            .And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false)
            .And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
            .Random(2).With(x => x.IsCashOut = true)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true)
            .And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
            .Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        letstalkService.SendMessage(default).ReturnsForAnyArgs(true);
        var sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        sendLetstalkServiceStage.GetContentExcelVoidedTicket(default, default, default, default).ReturnsForAnyArgs([]);
        var service = new AbnormalBetLetstalkServiceImp(sendLetstalkServiceStage, letstalkService);

        var actual = await service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
        Assert.True(actual);
    }
}