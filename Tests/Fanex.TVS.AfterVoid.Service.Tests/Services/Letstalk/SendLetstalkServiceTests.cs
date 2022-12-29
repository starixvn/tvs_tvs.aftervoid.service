using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Letstalk
{
    public class SendLetstalkServiceTests
    {
        [Fact]
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
            repository.FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>()).ReturnsForAnyArgs(fakeSubIssueTypes);
            var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
            ticketVoidingSystemHttpClientService.SendEmail(default, default, default, default, default, default).ReturnsForAnyArgs(
                (true, fakeResults));
            var service = new AfterVoidEmailServiceImp(ticketVoidingSystemHttpClientService, repository);

            var actual = await service.Execute(fakeAutomationInfo, fakeTicketEmails, fakeMatch, fakeReason, fakeMessage);

            await repository.Received(1).FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>());
            Assert.Equal(fakeResults, actual);
        }
    }
}