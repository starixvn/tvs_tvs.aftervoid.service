using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Services.Tests
{
    public class AfterVoidEmailServiceImpTests
    {
        [Fact()]
        public async Task Execute_ReturnExpectedResults()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            fakeAutomationInfo.RequestInfo = new()
            {
                Request = new RequestModel { IssueTypeId = 11 },
                EventByMatch = new RequestEventModel { SubIssueTypeId = 20 }
            };
            var fakeTicketEmails = Builder<TicketEmailModel>.CreateListOfSize(3).Build();
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
            var fakeSubIssueTypes = Builder<SubIssueType>.CreateListOfSize(2).Random(1).With(x => x.SubIssueTypeId = 20).Build();

            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>()).Returns(fakeSubIssueTypes);
            var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
            ticketVoidingSystemHttpClientService.SendEmail(default, default, default, default, default, default).ReturnsForAnyArgs(
                (true, fakeResults));
            var service = new AfterVoidEmailServiceImp(ticketVoidingSystemHttpClientService, repository);

            var actual = await service.Execute(fakeAutomationInfo, fakeTicketEmails, fakeMatch, fakeReason, fakeMessage);

            await repository.Received(1).FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>());
            Assert.Equal(fakeResults, actual);
        }

        [Fact()]
        public async Task Execute_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            fakeAutomationInfo.RequestInfo = new()
            {
                Request = new RequestModel { IssueTypeId = 11 },
                EventByMatch = new RequestEventModel { SubIssueTypeId = 20 }
            };
            var fakeTicketEmails = Builder<TicketEmailModel>.CreateListOfSize(3).Build();
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
            var fakeSubIssueTypes = Builder<SubIssueType>.CreateListOfSize(2).Random(1).With(x => x.SubIssueTypeId = 20).Build();

            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>()).Returns(fakeSubIssueTypes);
            var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
            ticketVoidingSystemHttpClientService.SendEmail(default, default, default, default, default, default).ReturnsForAnyArgs(
                (false, fakeResults));
            var service = new AfterVoidEmailServiceImp(ticketVoidingSystemHttpClientService, repository);

            await Assert.ThrowsAsync<ExternalException>(async () =>
                 await service.Execute(fakeAutomationInfo, fakeTicketEmails, fakeMatch, fakeReason, fakeMessage));
        }

        [Fact()]
        public async Task GetContentExcelVoidedTicket_ReturnTrue()
        {
            var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
            ticketVoidingSystemHttpClientService.GetContentExcelVoidedTicket(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns((true, new byte[1]));
            var service = new AfterVoidEmailServiceImp(ticketVoidingSystemHttpClientService, null);

            var actual = await service.GetContentExcelVoidedTicket(1, 1, 1);

            await ticketVoidingSystemHttpClientService.Received(1).GetContentExcelVoidedTicket(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
            Assert.Single(actual);
        }

        [Fact()]
        public async Task GetContentExcelVoidedTicket_ThrowException()
        {
            var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
            ticketVoidingSystemHttpClientService.GetContentExcelVoidedTicket(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns((false, new byte[1]));
            var service = new AfterVoidEmailServiceImp(ticketVoidingSystemHttpClientService, null);

            try
            {
                await service.GetContentExcelVoidedTicket(1, 1, 1);
            }
            catch (Exception exception)
            {
                Assert.NotNull(exception);
            }
        }
    }
}