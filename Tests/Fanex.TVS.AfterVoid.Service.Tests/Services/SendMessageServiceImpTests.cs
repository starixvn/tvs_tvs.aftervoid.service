using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services
{
    public class SendMessageServiceImpTests
    {
        [Fact]
        public async Task InitializeMessageContentForAutoVoid_ShouldReturnExpectedData()
        {
            var fakeMatch = new Match { MatchCode = "ppp00" };
            var fakeIssueTypeId = 111;
            var fakeTemplate = Builder<TemplateModel>.CreateListOfSize(5)
                .Random(1).With(x => x.TemplateId = 37).Build();
            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchAsync<TemplateModel>(Arg.Any<GetTemplatesCriteria>()).Returns(fakeTemplate);
            var service = new SendMessageServiceImp(repository, null, null);

            var actual = await service.InitializeMessageContentForAutoVoid(fakeMatch, true, fakeIssueTypeId, 1);

            await repository.Received(1).FetchAsync<TemplateModel>(Arg.Any<GetTemplatesCriteria>());
            Assert.Equal(AutoVoidConstant.ActorId, actual.CreatorId);
        }

        [Fact]
        public void InitializeMessageContentForManualVoid_ShouldReturnExpectedMessage()
        {
            var fakeVoidDataXmlModel = Builder<VoidDataXmlModel>.CreateNew().Build();
            var service = new SendMessageServiceImp(null, null, null);

            var actual = service.InitializeMessageContentForManualVoid(fakeVoidDataXmlModel);

            Assert.Equal(fakeVoidDataXmlModel.ContentEnglish, actual.ContentEnglish);
        }

        [Fact]
        public async Task GetMessage_ReturnMessageModel()
        {            // Arrange
            var getMessageCriteria = new GetMessageCriteria { MessageId = 12345 };
            var expected = new Message
            {
                MessageId = 12345,
            };
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<Message>(getMessageCriteria).ReturnsForAnyArgs(expected);

            var service = new SendMessageServiceImp(repository, null, null);

            // Act
            var actual = await service.GetMessage(getMessageCriteria.MessageId);

            // Assert
            Assert.Equal(expected, actual);
            await repository.Received(1).GetAsync<Message>(Arg.Compat.Is<GetMessageCriteria>(x => x.MessageId == 12345));
        }

        [Fact]
        public async Task InsertMessage_ShouldReturnExpectedId()
        {
            var fakeMatchId = 11;
            var fakeLeagueGroupId = 12;
            var fakeIssueTypeId = 22;
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<int>(Arg.Any<CreateMessageCriteria>()).Returns(99);
            var service = new SendMessageServiceImp(repository, null, null);

            var actual = await service.InsertMessage(fakeMessage, fakeMatchId, fakeLeagueGroupId, fakeIssueTypeId);

            Assert.StrictEqual(99, actual);
            await repository.Received(1).GetAsync<int>(Arg.Is<CreateMessageCriteria>(x => x.MatchId == fakeMatchId
            && x.LeagueGroupId == fakeLeagueGroupId && x.Creator == fakeMessage.CreatorId));
        }

        [Fact]
        public async Task InsertVoidedTransaction_ShouldCallExecuteAsync()
        {            // Arrange
            var fakeData = new
            {
                MessageId = 12345,
                ReasonId = 111,
                IssueTypeId = 11,
                SubIssueTypeId = 27,
                MatchId = 113,
                XmlCustInfo = "CustInfoXml",
                MainTransIds = "12345679",
                SubTransIds = "98754321"
            };
            var repository = Substitute.For<IDynamicRepository>();
            var messageItemService = Substitute.For<MessageItemService>();
            var ticketVoidingApi = Substitute.For<TicketVoidingSystemHttpClientService>();
            var service = new SendMessageServiceImp(repository, messageItemService, ticketVoidingApi);

            // Act
            await service.InsertVoidedTransaction(fakeTicketEmailModels, fakeData.MessageId, fakeData.ReasonId, fakeData.IssueTypeId, fakeData.SubIssueTypeId, fakeData.MatchId);

            // Assert
            await repository.Received(2).ExecuteAsync(Arg.Any<InsertVoidedTransactionCommand>());
        }

        [Fact]
        public async Task SendMessage_ShouldCallInsertBatchMessage()
        {   // Arrange
            var messageXml = "<Messages>Content</Messages>";
            var messageId = 1111;
            var repository = Substitute.For<IDynamicRepository>();
            var messageItemService = Substitute.For<MessageItemService>();
            messageItemService.CreateMessageItems(Arg.Any<IEnumerable<ReceivedUser>>(), Arg.Compat.Any<int>()).ReturnsForAnyArgs(messageXml);

            var service = new SendMessageServiceImp(repository, messageItemService, null);

            // Act
            await service.SendMessage(fakeTicketEmailModels, messageId, 11, 8);

            // Assert
            await repository.Received(1).ExecuteAsync(Arg.Is<SendMessageCommand>(x => x.CreatorId == 8));
        }

        [Fact]
        public async Task InitializeVoidReason_WhenIsNotAutoVoid()
        {
            var fakeVoidData = Builder<VoidDataXmlModel>.CreateNew().Build();
            var service = new SendMessageServiceImp(null, null, null);

            var actual = await service.InitializeVoidReason(false, 0, 0, fakeVoidData);

            // Assert
            Assert.Equal(fakeVoidData.Reason, actual.ReasonEnglish);
            Assert.Equal(fakeVoidData.ReasonSC, actual.ReasonChineseSimplified);
        }

        [Fact]
        public async Task InitializeVoidReason_WhenIsAutoVoid_ReturnEmptyVoidReason()
        {
            var fakeSubIssueType = 11;
            var fakeSportType = 22;
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<VoidedReason>(Arg.Any<GetReasonNameCriteria>()).ReturnsNull();
            var service = new SendMessageServiceImp(repository, null, null);

            var actual = await service.InitializeVoidReason(true, fakeSubIssueType, fakeSportType, null);

            Assert.Null(actual.ReasonEnglish);
            Assert.Null(actual.ReasonChineseSimplified);
            await repository.Received(1).GetAsync<VoidedReason>(Arg.Is<GetReasonNameCriteria>(x => x.SubIssueTypeId == fakeSubIssueType && x.SportType == fakeSportType));
        }

        [Fact]
        public async Task InitializeVoidReason_WhenIsAutoVoid_ReturnExpectedVoidReason()
        {
            var fakeSubIssueType = 11;
            var fakeSportType = 22;
            var fakeReason = Builder<VoidedReason>.CreateNew().Build();
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<VoidedReason>(Arg.Any<GetReasonNameCriteria>()).Returns(fakeReason);
            var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
            ticketVoidingSystemHttpClientService.BetListGetSportTypeName(fakeSportType, Arg.Any<string>()).Returns("a");
            var service = new SendMessageServiceImp(repository, null, ticketVoidingSystemHttpClientService);

            var actual = await service.InitializeVoidReason(true, fakeSubIssueType, fakeSportType, null);

            Assert.NotNull(actual.ReasonEnglish);
            Assert.NotNull(actual.ReasonChineseSimplified);
            await repository.Received(1).GetAsync<VoidedReason>(Arg.Is<GetReasonNameCriteria>(x => x.SubIssueTypeId == fakeSubIssueType && x.SportType == fakeSportType));
            await ticketVoidingSystemHttpClientService.Received(2).BetListGetSportTypeName(Arg.Is<int>(x => x == fakeSportType), Arg.Any<string>());
        }

        [Fact]
        public async Task InsertReason_ShouldReturnReasonId()
        {
            // Arrange
            var criteria = new InsertReasonCommand { ReasonEnglish = "Reason English", ReasonChineseSimplified = "Reason Chinese Simplified", ReasonId = 12345 };
            var repository = Substitute.For<IDynamicRepository>();
            repository.When(repo => repo.ExecuteAsync(criteria)).Do(repo => { criteria.ReasonId = 12345; });

            repository.ExecuteAsync(Arg.Compat.Do<InsertReasonCommand>(x =>
            {
                x.ReasonEnglish = criteria.ReasonEnglish;
                x.ReasonChineseSimplified = criteria.ReasonChineseSimplified;
                x.ReasonId = criteria.ReasonId;
            })).Returns(999);

            var service = new SendMessageServiceImp(repository, null, null);

            // Act
            var actual = await service.InsertReason("Reason English", "Reason Chinese Simplifie");

            // Assert
            Assert.Equal(12345, actual);
        }

        [Fact]
        public async Task GetVoidedReason_ReturnVoidedReasonModel()
        {      // Arrange
            var getVoidedReasonCriteria = new GetVoidedReasonCriteria { ReasonId = 12345 };
            var expected = new VoidedReason
            {
                ReasonId = 12345,
                ReasonEnglish = "Reason English",
                ReasonChineseSimplified = "Reason Chinese Simplified"
            };
            var repository = Substitute.For<IDynamicRepository>();
            repository.GetAsync<VoidedReason>(getVoidedReasonCriteria).ReturnsForAnyArgs(expected);

            var service = new SendMessageServiceImp(repository, null, null);

            // Act
            var actual = await service.GetVoidedReason(12345);

            // Assert
            Assert.Equal(expected, actual);
            await repository.Received(1).GetAsync<VoidedReason>(Arg.Compat.Is<GetVoidedReasonCriteria>(x => x.ReasonId == 12345));
        }

        [Fact]
        public void InsertBatchMessage_()
        {
        }

        [Fact]
        public async Task InsertMessageIntoAdminSite_ShouldCallRepository()
        {            // Arrange
            var message = new Message
            {
                ContentEnglish = "Content English",
                ContentChineseTraditional = "Content Chinese",
                CreatorId = 8
            };
            var repository = Substitute.For<IDynamicRepository>();
            var messageItemService = Substitute.For<MessageItemService>();

            var service = new SendMessageServiceImp(repository, messageItemService, null);

            // Act
            await service.InsertMessageIntoAdminSite(message);

            // Assert
            await repository.Received(1).ExecuteAsync(Arg.Compat.Is<InsertMessageIntoAdminSiteCriteria>(x => x.men == message.ContentEnglish
                && x.mch == message.ContentChineseTraditional));
        }

        [Fact]
        public void GetListSendMessageDb_()
        {
        }

        private readonly IEnumerable<TicketEmailModel> fakeTicketEmailModels = new List<TicketEmailModel>
        {
            new TicketEmailModel
            {
                Site = "cn88",
                CustId = 1111,
                UserName = "sa",
                Recommend = 111,
                AgUserName = "AgUserName",
                Mrecommend = 222,
                MagUserName = "MagUserName",
                Srecommend = 333,
                SagUserName = "SagUserName",
                TransId = 123456789,
                IsMixParlay = false
            },
            new TicketEmailModel
            {
                Site = "aaaa",
                CustId = 12345,
                UserName = "sa",
                Recommend = 111,
                AgUserName = "AgUserName",
                Mrecommend = 222,
                MagUserName = "MagUserName",
                Srecommend = 333,
                SagUserName = "SagUserName",
                TransId = 987654321,
                IsMixParlay = true
            }
        };
    }
}