using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Core.Criteria;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using FizzWare.NBuilder;
using NSubstitute;
using Xunit;

namespace Fanex.TVS.AfterVoid.Core.Services.Tests
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
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            ticketVoidingSystemApi.SendEmail(default, default, default, default, default, default).ReturnsForAnyArgs(
                (true, fakeResults));
            var service = new AfterVoidEmailServiceImp(ticketVoidingSystemApi, repository);

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
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            ticketVoidingSystemApi.SendEmail(default, default, default, default, default, default).ReturnsForAnyArgs(
                (false, fakeResults));
            var service = new AfterVoidEmailServiceImp(ticketVoidingSystemApi, repository);

            await Assert.ThrowsAsync<ExternalException>(async () =>
                 await service.Execute(fakeAutomationInfo, fakeTicketEmails, fakeMatch, fakeReason, fakeMessage));
        }
    }
}