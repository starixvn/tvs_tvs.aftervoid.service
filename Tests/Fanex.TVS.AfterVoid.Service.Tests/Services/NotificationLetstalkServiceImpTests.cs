using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.LetstalkService.Client;
using Fanex.RemotingRef;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using FizzWare.NBuilder;
using NSubstitute;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Services.Tests
{
    public class NotificationLetstalkServiceImpTests
    {
        private void SetupTestEnv()
        {
            RefDataTypeContainer
                .ConnectTo(new Uri("http://remotingref-api.nexdev.net/main/"))
                .FromApplication("NAP-SpecialDelete-LOCAL")
                .RegisterSupportedLanguages("en-US", "zh-TW", "zh-CN");
        }

        [Fact()]
        public void SendToSettledBetGroup_ShouldNotRunCreateTemplate()
        {
            var letstalkService = Substitute.For<LetstalkClientService>();
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            service.SendToSettledBetGroup(default, new List<TicketEmailModel>());

            letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
        }

        [Fact()]
        public void SendToSettledBetGroup_WhenRequestHaveUsername_ShouldNotRunLetstalkService()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = true };
            var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(5)
                .All().With(x => x.IsCashOut = false).Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            service.SendToSettledBetGroup(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
        }

        [Fact()]
        public void SendToSettledBetGroup_WhenRequestHaveUsername_ShouldRunLetstalkServiceFail()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = true };
            var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
                .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
                .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
                .Random(2).With(x => x.IsCashOut = true)
                .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
                .Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            letstalkService.SendMessage(default).ReturnsForAnyArgs(false);
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            var actual = service.SendToSettledBetGroup(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
            Assert.False(actual);
        }

        [Fact()]
        public void SendToSettledBetGroup_WhenRequestHaveUsername_ShouldRunLetstalkServiceSuccessMultipleTimes()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = true };
            var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(200)
                .Random(40).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
                .Random(40).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
                .Random(40).With(x => x.IsCashOut = true)
                .Random(40).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
                .Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            letstalkService.SendMessage(default).ReturnsForAnyArgs(true);
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            var actual = service.SendToSettledBetGroup(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(2).SendMessage(default);
            Assert.True(actual);
        }

        [Fact()]
        public void SendToSettledBetGroup_WhenRequestNotHaveUsername_ShouldNotRunLetstalkService()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = true };
            var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = false).Build();
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(5)
                .All().With(x => x.IsCashOut = false).Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            service.SendToSettledBetGroup(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SendToSettledBetGroup_WhenRequestNotHaveUsername_ShouldRunLetstalkServiceSuccess(bool isPRODEnvironment)
        {
            SetupTestEnv();
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = isPRODEnvironment };
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = false).Build();
            fakeNotificationLetstalkModel.Match = fakeMatch;
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
                .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = false)
                .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = false).And(x => x.Status = TicketStatus.Won).And(x => x.IsInstantSettlementTicket = true)
                .Random(2).With(x => x.IsCashOut = true)
                .Random(2).With(x => x.IsCashOut = false).And(x => x.IsMixParlay = true).And(x => x.Status = TicketStatus.Won).And(x => x.BetTypeId = 29)
                .Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            letstalkService.SendMessage(default).ReturnsForAnyArgs(true);
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            var actual = service.SendToSettledBetGroup(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
            Assert.True(actual);
        }

        [Fact()]
        public async Task SendToAbnormalBetGroup_ShouldNotRunCreateTemplate()
        {
            var letstalkService = Substitute.For<LetstalkClientService>();
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            await service.SendToAbnormalBetGroup(default, new List<TicketEmailModel>());

            letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
        }

        [Theory]
        [InlineData(11, null, true)]
        [InlineData(15, null, true)]
        [InlineData(11, 1, false)]
        public async Task SendToAbnormalBetGroup_WhenRequestHaveUsername_ShouldRunLetstalkServiceFail(int issueId, int? filterBy, bool IsPRODEnvironment)
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = IsPRODEnvironment };
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
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            var actual = await service.SendToAbnormalBetGroup(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
            Assert.False(actual);
        }

        [Fact()]
        public async Task SendToAbnormalBetGroup_ShouldReturnTrueWhenHaveNotContent()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = false };
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(10)
                .Random(2).With(x => x.IsCredit = true)
                .Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            var actual = await service.SendToAbnormalBetGroup(new NotificationLetstalkModel(), fakeTicketEmail);

            Assert.True(actual);
        }

        [Fact()]
        public async Task SendToAbnormalBetGroup_ShouldReturnTrue()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = false };
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
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            var service = new NotificationLetstalkServiceImp(letstalkService, afterVoidEmailService);

            var actual = await service.SendToAbnormalBetGroup(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
            Assert.True(actual);
        }
    }
}