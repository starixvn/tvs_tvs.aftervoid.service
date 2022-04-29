using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Criteria;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using NSubstitute;
using Xunit;

namespace Fanex.TVS.AfterVoid.Core.Services.Tests
{
    public class VoidBetAutomationServiceTests
    {
        [Fact()]
        public async Task GetVoidBetAutomationStatistic_ReturnExpectedData()
        {
            var expected = new AutomationStatistic
            {
                RequestsWillUpdateStatus = new List<VoidBetAutomationInfo>() { new() { RequestId = 2 } },
                RequestsWillRunAfterVoid = new List<VoidBetAutomationInfo>() { new() { RequestId = 1 } }
            };
            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidBetAutomationInfo>(Arg.Any<GetAllVoidBetAutomationInfoCriteria>())
                .Returns((expected.RequestsWillUpdateStatus, expected.RequestsWillRunAfterVoid));

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            var actual = await voidBetAutomationService.GetVoidBetAutomationStatistic();

            Assert.StrictEqual(expected.RequestsWillUpdateStatus, actual.RequestsWillUpdateStatus);
            Assert.StrictEqual(expected.RequestsWillRunAfterVoid, actual.RequestsWillRunAfterVoid);
        }

        [Fact()]
        public async Task GetRequestById_ReturnDataEventWithExpectedMatch()
        {
            var fakeRequests = new List<RequestModel>
            {
                new() {IssueTypeId = IssueTypeConstant.DangerAccepted},
                new() {IssueTypeId = IssueTypeConstant.AbnormalBet}
            };
            var fakeRequestEvents = new List<RequestEventModel>
            {
                new() {MatchId = 100},
                new() {MatchId = 113}
            };

            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchMultipleAsync<RequestModel, RequestEventModel, Message>(Arg.Any<SelectRequestCriteria>())
                .Returns((fakeRequests, fakeRequestEvents, null));

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            var actual = await voidBetAutomationService.GetRequestById(0, 113);

            Assert.StrictEqual(fakeRequests[0], actual.Request);
            Assert.StrictEqual(fakeRequestEvents[1], actual.EventByMatch);
        }

        [Fact()]
        public async Task GetRequestById_ReturnDataWithFirstEvent()
        {
            var fakeRequests = new List<RequestModel>
            {
                new() {IssueTypeId = IssueTypeConstant.AbnormalBet},
                new() {IssueTypeId = IssueTypeConstant.DangerAccepted}
            };
            var fakeRequestEvents = new List<RequestEventModel>
            {
                new() {MatchId = 100},
                new() {MatchId = 113}
            };

            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchMultipleAsync<RequestModel, RequestEventModel, Message>(Arg.Any<SelectRequestCriteria>())
                .Returns((fakeRequests, fakeRequestEvents, null));

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            var actual = await voidBetAutomationService.GetRequestById(0, 113);

            Assert.StrictEqual(fakeRequests[0], actual.Request);
            Assert.StrictEqual(fakeRequestEvents[0], actual.EventByMatch);
        }

        [Fact()]
        public async Task GetRequestSimpleInfo_ReturnExpectedData()
        {
            const int fakeRequestId = 113;
            const int fakeVoidingHistoryId = 211;
            var expectedRequestInfo = new RequestSimpleInfo { MatchId = 111 };
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<RequestSimpleInfo>(Arg.Is<GetRequestSimpleInfoCriteria>(x => x.RequestId == fakeRequestId
                    && x.VoidingHistoryId == fakeVoidingHistoryId)).Returns(expectedRequestInfo);

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            var actual = await voidBetAutomationService.GetRequestSimpleInfo(fakeRequestId, fakeVoidingHistoryId);

            Assert.StrictEqual(expectedRequestInfo, actual);
        }

        [Fact()]
        public async Task UpdateServiceStatusByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
        {
            const int fakeRequestId = 113;
            const int fakeVoidingHistoryId = 211;
            const int fakeStatusId = 311;
            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(fakeRequestId, fakeVoidingHistoryId, fakeStatusId);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
               x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateStatusByVoidingHistory && x.RequestId == fakeRequestId
                && x.VoidingHistoryId == fakeVoidingHistoryId && x.StatusId == fakeStatusId));
        }

        [Fact()]
        public async Task UpdateServiceStatusBySession_ShouldCallUpdateVoidBetAutomationServiceInfo()
        {
            const int fakeSessionId = 113;
            const int fakeStatusId = 311;
            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateServiceStatusBySession(fakeSessionId, fakeStatusId);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
                x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateStatusBySession && x.SessionId == fakeSessionId
                && x.StatusId == fakeStatusId));
        }

        [Fact()]
        public async Task UpdateMessageIdByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
        {
            const int fakeRequestId = 113;
            const int fakeVoidingHistoryId = 211;
            const int fakeMessageId = 331;
            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateMessageIdByVoidingHistory(fakeRequestId, fakeVoidingHistoryId, fakeMessageId);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
                x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateMessageIdByVoidingHistory && x.RequestId == fakeRequestId
                && x.VoidingHistoryId == fakeVoidingHistoryId && x.MessageId == fakeMessageId));
        }

        [Fact()]
        public async Task UpdateMessageIdBySession_ShouldCallUpdateVoidBetAutomationServiceInfo()
        {
            const int fakeSessionId = 211;
            const int fakeMessageId = 331;
            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateMessageIdBySession(fakeSessionId, fakeMessageId);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
                x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateMessageIdBySession && x.SessionId == fakeSessionId
                && x.MessageId == fakeMessageId));
        }

        [Fact()]
        public async Task UpdateAfterVoidStep_ShouldCallUpdateVoidBetAutomationServiceInfo()
        {
            const int fakeRequestId = 211;
            const int fakeVoidingHistoryId = 331;
            var fakeAfterVoidHistory = new VoidBetAutomationInfo { ReasonId = 444 };
            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateAfterVoidStep(fakeRequestId, fakeVoidingHistoryId, fakeAfterVoidHistory);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
                x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateAfterVoidStep && x.RequestId == fakeRequestId
                && x.VoidingHistoryId == fakeVoidingHistoryId && x.ReasonId == fakeAfterVoidHistory.ReasonId));
        }

        [Fact()]
        public async Task UpdateAllAfterVoidSteps_ShouldCallUpdateVoidBetAutomationServiceInfo()
        {
            const int fakeRequestId = 211;
            const int fakeVoidingHistoryId = 331;
            var fakeAfterVoidHistory = new VoidBetAutomationInfo { ReasonId = 444 };
            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateAllAfterVoidSteps(fakeRequestId, fakeVoidingHistoryId, fakeAfterVoidHistory);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
                x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateAllAfterVoidSteps && x.RequestId == fakeRequestId
                && x.VoidingHistoryId == fakeVoidingHistoryId && x.ReasonId == fakeAfterVoidHistory.ReasonId));
        }

        [Fact()]
        public async Task UpdateEmailResultByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
        {
            const int fakeRequestId = 211;
            const int fakeVoidingHistoryId = 331;
            const string fakeVoidResultContent = "abc";

            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateEmailResultByVoidingHistory(fakeRequestId, fakeVoidingHistoryId, fakeVoidResultContent);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
                x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateEmailsResultByVoidingHistory && x.RequestId == fakeRequestId
                && x.VoidingHistoryId == fakeVoidingHistoryId && x.VoidResults == fakeVoidResultContent));
        }

        [Fact()]
        public async Task GetVoidedTicketsForSendEmail_ReturnExpectedModel()
        {
            const int fakeVoidingHistoryId = 331;
            var fakeTicketEmail = new List<TicketEmailModel>() { new() { BetId = 113 } };
            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchAsync<TicketEmailModel>(
                Arg.Is<GetTicketsEmailCriteria>(x => x.VoidingHistoryId == fakeVoidingHistoryId)).Returns(fakeTicketEmail);

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            var actual = await voidBetAutomationService.GetVoidedTicketsForSendEmail(fakeVoidingHistoryId);

            Assert.StrictEqual(fakeTicketEmail, actual);
        }

        [Fact()]
        public async Task GetVoidDataFromVoidingHistory_ReturnExpectedModel()
        {
            const int fakeVoidingHistoryId = 331;
            var fakeVoidDataXml = new VoidDataXmlModel() { ContentEnglish = "zbc" };
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<VoidDataXmlModel>(
                Arg.Is<GetVoidingHistoryCriteria>(x => x.VoidingHistoryId == fakeVoidingHistoryId)).Returns(fakeVoidDataXml);

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            var actual = await voidBetAutomationService.GetVoidDataFromVoidingHistory(fakeVoidingHistoryId);

            Assert.StrictEqual(fakeVoidDataXml, actual);
        }

        [Fact()]
        public async Task GetMatchInfo_ReturnExpectedModel()
        {
            const int fakeMatchId = 331;
            var fakeMatch = new Match() { MatchCode = "ppp00" };
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<Match>(
                Arg.Is<GetMatchInfoCriteria>(x => x.MatchId == fakeMatchId)).Returns(fakeMatch);

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            var actual = await voidBetAutomationService.GetMatchInfo(fakeMatchId);

            Assert.StrictEqual(fakeMatch, actual);
        }

        [Fact()]
        public async Task UpdateTicketVoidingHistory_ShouldCallExecuteAsync()
        {
            const byte fakeStatusId = 1;
            const int fakeVoidingHistoryId = 331;
            const int fakeReasonId = 111;
            const int fakeMessageId = 222;
            var repository = Substitute.For<IDynamicRepository>();
            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);
            await voidBetAutomationService.UpdateTicketVoidingHistory(fakeVoidingHistoryId, fakeStatusId, fakeReasonId, fakeMessageId);

            await repository.Received(1).ExecuteAsync(Arg.Is<UpdateHistoryCommand>(x =>
                x.StatusId == fakeStatusId && x.VoidingHistoryId == fakeVoidingHistoryId
                && x.ReasonId == fakeReasonId && x.MessageId == fakeMessageId));
        }
    }
}