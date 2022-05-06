using System;
using System.Collections.Generic;
using Fanex.LetstalkService.Client;
using Fanex.RemotingRef;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using FizzWare.NBuilder;
using NSubstitute;
using Xunit;

namespace Fanex.TVS.AfterVoid.Core.Services.Tests
{
    public class NotificationLetstalkServiceImpTests
    {
        private void SetupTestEnv()
        {
            RefDataTypeContainer
                .ConnectTo(new Uri("http://ha.nexdev.net:7258/Main/"))
                .FromApplication("NAP-SpecialDelete-LOCAL")
                .RegisterSupportedLanguages("en-US", "zh-TW", "zh-CN");
        }

        [Fact()]
        public void Send_ShouldNotRunCreateTemplate()
        {
            var letstalkService = Substitute.For<LetstalkClientService>();
            var service = new NotificationLetstalkServiceImp(letstalkService);

            service.Send(default, new List<TicketEmailModel>());

            letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
        }

        [Fact()]
        public void Send_WhenRequestHaveUsername_ShouldNotRunLetstalkService()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = true };
            var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = true).Build();
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(5)
                .All().With(x => x.IsCashOut = false).Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            var service = new NotificationLetstalkServiceImp(letstalkService);

            service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
        }

        [Fact()]
        public void Send_WhenRequestHaveUsername_ShouldRunLetstalkServiceFail()
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
            var service = new NotificationLetstalkServiceImp(letstalkService);

            var actual = service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
            Assert.False(actual);
        }

        [Fact()]
        public void Send_WhenRequestHaveUsername_ShouldRunLetstalkServiceSuccessMultipleTimes()
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
            var service = new NotificationLetstalkServiceImp(letstalkService);

            var actual = service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(2).SendMessage(default);
            Assert.True(actual);
        }

        [Fact()]
        public void Send_WhenRequestNotHaveUsername_ShouldNotRunLetstalkService()
        {
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = true };
            var fakeNotificationLetstalkModel = Builder<NotificationLetstalkModel>.CreateNew().With(x => x.IsMemberTemplate = false).Build();
            var fakeTicketEmail = Builder<TicketEmailModel>.CreateListOfSize(5)
                .All().With(x => x.IsCashOut = false).Build();
            var letstalkService = Substitute.For<LetstalkClientService>();
            var service = new NotificationLetstalkServiceImp(letstalkService);

            service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.DidNotReceiveWithAnyArgs().SendMessage(default);
        }

        [Fact()]
        public void Send_WhenRequestNotHaveUsername_ShouldRunLetstalkServiceSuccess()
        {
            SetupTestEnv();
            AppConfigService.AppConfig = new ConfigSettingModel() { IsPRODEnvironment = true };
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
            var service = new NotificationLetstalkServiceImp(letstalkService);

            var actual = service.Send(fakeNotificationLetstalkModel, fakeTicketEmail);

            letstalkService.ReceivedWithAnyArgs(1).SendMessage(default);
            Assert.True(actual);
        }
    }
}