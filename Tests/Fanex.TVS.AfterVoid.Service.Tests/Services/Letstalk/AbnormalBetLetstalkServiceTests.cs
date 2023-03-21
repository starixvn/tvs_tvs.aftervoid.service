using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.LetstalkService.Client;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Models;
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

        await service.Send(default, new List<TicketEmailModel>());

        letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
    }

    [Theory]
    [InlineData(11, null, true)]
    [InlineData(15, null, true)]
    [InlineData(11, 1, false)]
    public async Task SendToAbnormalBetGroup_WhenRequestHaveUsername_ShouldRunLetstalkServiceFail(int issueId, int? filterBy, bool IsPRODEnvironment)
    {
        AppConfigService.AppConfig = new ConfigSettingModel { IsPRODEnvironment = IsPRODEnvironment };
        var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
        fakeNotificationLetstalkModel.IssueTypeId = issueId;
        fakeNotificationLetstalkModel.FilterBy = filterBy;
        fakeNotificationLetstalkModel.Match = new Match();
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
            .Random(2).With(x => x.IsCashOut = true)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
            .Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        letstalkService.SendMessage(default).ReturnsForAnyArgs(false);
        var sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        var service = new AbnormalBetLetstalkServiceImp(sendLetstalkServiceStage, letstalkService);

        var actual = await service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
        Assert.False(actual);
    }

    [Fact]
    public async Task SendToAbnormalBetGroup_ShouldReturnTrueWhenHaveNotContent()
    {
        AppConfigService.AppConfig = new ConfigSettingModel { IsPRODEnvironment = false };
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
            .Random(2).With(x => x.IsCredit = true)
            .Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        var sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        var service = new AbnormalBetLetstalkServiceImp(sendLetstalkServiceStage, letstalkService);

        var actual = await service.Send(new NotificationLetstalkModel(), fakeTicketEmail);

        Assert.True(actual);
    }

    [Fact]
    public async Task SendToAbnormalBetGroup_ShouldReturnTrue()
    {
        AppConfigService.AppConfig = new ConfigSettingModel { IsPRODEnvironment = false };
        var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
        fakeNotificationLetstalkModel.IssueTypeId = 11;
        fakeNotificationLetstalkModel.FilterBy = 1;
        fakeNotificationLetstalkModel.Match = new Match();
        var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
            .Random(2).With(x => x.IsCashOut = true)
            .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
            .Build();
        var letstalkService = Substitute.For<LetstalkClientService>();
        letstalkService.SendMessage(default).ReturnsForAnyArgs(true);
        var sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        var service = new AbnormalBetLetstalkServiceImp(sendLetstalkServiceStage, letstalkService);

        var actual = await service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

        letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
        Assert.True(actual);
    }
}