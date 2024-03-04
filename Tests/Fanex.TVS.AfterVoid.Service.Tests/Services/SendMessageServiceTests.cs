using System;
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
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services;

public class SendMessageServiceTests
{
    private readonly IEnumerable<TicketEmailModel> fakeTicketEmailModels = new List<TicketEmailModel>
    {
        new()
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
            IsMixParlay = false,
            MatchId = 111,
            WinLostDate = DateTimeOffset.MinValue.DateTime,
            Status = "Done",
            BetTypeId = 1,
            Stake = 1,
            WinLost = "100",
            VoidingHistoryId = 1,
            SiteDbGroupId = 0
        },
        new()
        {
            Site = "a",
            CustId = 12345,
            UserName = "sa",
            Recommend = 111,
            AgUserName = "AgUserName",
            Mrecommend = 222,
            MagUserName = "MagUserName",
            Srecommend = 333,
            SagUserName = "SagUserName",
            TransId = 987654321,
            IsMixParlay = true,
            MatchId = 1111,
            WinLostDate = DateTimeOffset.MinValue.DateTime,
            Status = "Running",
            BetTypeId = 1,
            Stake = 1,
            WinLost = "100",
            VoidingHistoryId = 1,
            SiteDbGroupId = 0
        }
    };

    private readonly MessageItemService messageItemService = Substitute.For<MessageItemService>();
    private readonly IDynamicRepository repository = Substitute.For<IDynamicRepository>();
    private readonly TVSWebSiteHttpClientService ticketVoidingSystemHttpClientService = Substitute.For<TVSWebSiteHttpClientService>();

    [Theory]
    [InlineData(IssueTypeConstant.AbnormalBet, 1)]
    [InlineData(IssueTypeConstant.AbnormalBet, null)]
    [InlineData(IssueTypeConstant.AbnormalBetRealMember, 1)]
    [InlineData(IssueTypeConstant.DangerAccepted, 1)]
    [InlineData(IssueTypeConstant.DuplicateTickets, null)]
    [InlineData(IssueTypeConstant.DuplicateTickets, 1)]
    [InlineData(IssueTypeConstant.AbnormalAccount, 1)]
    [InlineData(IssueTypeConstant.ReducedOver, 1)]
    [InlineData(IssueTypeConstant.NotParticipate, 1)]
    public async Task InitializeMessageContentForAutoVoid_ShouldReturnExpectedData(int issueTypeId, int? filterBy)
    {
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel>
        {
            new()
            {
                IsHasSettledTicket = true, TotalTicket = 7,
                VoidFromTime = "2022/02/02 02:02:02",
                VoidToTime = "2022/02/02 02:04:04"
            }
        };
        var fakeMatch = new Match { MatchCode = "ppp00" };
        var fakeTemplate = Builder<TemplateModel>.CreateListOfSize(8)
            .Random(1).With(x => x.TemplateId = 37)
            .Random(1).With(x => x.TemplateId = 34)
            .Random(1).With(x => x.TemplateId = 1)
            .Random(1).With(x => x.TemplateId = 64)
            .Random(1).With(x => x.TemplateId = 65)
            .Random(1).With(x => x.TemplateId = 37)
            .Random(1).With(x => x.TemplateId = 38)
            .Random(1).With(x => x.TemplateId = 63)
            .Build();
        repository.FetchAsync<TemplateModel>(Arg.Any<GetTemplatesCriteria>()).Returns(fakeTemplate);
        var requestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = issueTypeId },
            EventByMatch = new RequestEventModel { SubIssueTypeId = 1, FilterBy = filterBy, KickOffTime = DateTimeOffset.Now.DateTime },
            PlayerNamesSelected = new TemplateContent()
        };
        var service = new SendMessageServiceImp(repository, null, null);

        var actual =
            await service.InitializeMessageContentForAutoVoid(fakeMatch, allVoidDataInCurrentVoidTimes, requestInfo);

        await repository.Received(1).FetchAsync<TemplateModel>(Arg.Any<GetTemplatesCriteria>());
        Assert.Equal(AutoVoidConstant.ActorId, actual.CreatorId);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public async Task InitializeMessageContentForAutoVoid_HasNotMatch_ShouldReturnExpectedData(int subIssueTypeId, int sportType)
    {
        var fakeTemplateSetting = new List<TemplateSetting>
        {
            new (){SubIssueTypeId = 1, SportType = 1, TemplateId = 1},
            new (){SubIssueTypeId = 1, SportType = null, TemplateId = 2}
        };
        var fakeTemplate = Builder<TemplateModel>.CreateListOfSize(8)
            .Random(1).With(x => x.TemplateId = 37)
            .Random(1).With(x => x.TemplateId = 34)
            .Random(1).With(x => x.TemplateId = 1)
            .Build();
        repository.FetchAsync<TemplateModel>(Arg.Any<GetTemplatesCriteria>()).Returns(fakeTemplate);
        repository.FetchAsync<TemplateSetting>(Arg.Any<GetTemplatesCriteria>()).Returns(fakeTemplateSetting);
        var requestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = 1, SportType = sportType },
            EventByMatch = new RequestEventModel
            {
                SubIssueTypeId = subIssueTypeId,
                KickOffTime = DateTimeOffset.Now.DateTime
            },
            PlayerNamesSelected = new TemplateContent
            {
                MsgEnglish = "a",
                MsgChineseNewSimplified = "a",
                MsgChineseSimplified = "a",
                MsgChineseTraditional = "a"
            }
        };
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel>
        {
            new()
            {
                VoidFromTime = "2022/2/2",
                VoidToTime = "2022/2/2"
            },
            new()
            {
                VoidFromTime = "2022/3/2",
                VoidToTime = "2022/3/2"
            }
        };
        var service = new SendMessageServiceImp(repository, null, null);

        var actual = await service.InitializeMessageContentForAutoVoid(null, allVoidDataInCurrentVoidTimes, requestInfo);
        await repository.Received(1).FetchAsync<TemplateSetting>(Arg.Any<GetTemplateSettingCriteria>());
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
    {
        // Arrange
        var getMessageCriteria = new GetMessageCriteria { MessageId = 12345 };
        var expected = new Message
        {
            MessageId = 12345
        };
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
        const int fakeMatchId = 11;
        const int fakeLeagueGroupId = 12;
        const int fakeIssueTypeId = 22;
        var fakeMessage = Builder<Message>.CreateNew().Build();
        repository.GetAsync<int>(Arg.Any<CreateMessageCriteria>()).Returns(99);
        var service = new SendMessageServiceImp(repository, null, null);

        var actual = await service.InsertMessage(fakeMessage, fakeMatchId, fakeLeagueGroupId, fakeIssueTypeId);

        Assert.StrictEqual(99, actual);
        await repository.Received(1).GetAsync<int>(Arg.Is<CreateMessageCriteria>(x => x.MatchId == fakeMatchId
            && x.LeagueGroupId == fakeLeagueGroupId && x.Creator == fakeMessage.CreatorId));
    }

    [Fact]
    public async Task InsertVoidedTransaction_ShouldCallExecuteAsync()
    {
        // Arrange
        var fakeData = new
        {
            MessageId = 12345,
            ReasonId = 111,
            IssueTypeId = 11,
            SubIssueTypeId = 27,
            MatchId = 113,
            XmlCustInfo = "InfoXml",
            MainTransIds = "12345679",
            SubTransIds = "98754321"
        };
        var ticketVoidingApi = Substitute.For<TVSWebSiteHttpClientService>();
        var service = new SendMessageServiceImp(repository, messageItemService, ticketVoidingApi);

        // Act
        await service.InsertVoidedTransaction(fakeTicketEmailModels, fakeData.MessageId, fakeData.ReasonId,
            fakeData.IssueTypeId, fakeData.SubIssueTypeId, fakeData.MatchId);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<InsertVoidedTransactionCommand>());
    }

    [Theory]
    [InlineData(IssueTypeConstant.AbnormalBet)]
    [InlineData(IssueTypeConstant.AbnormalBetRealMember)]
    [InlineData(IssueTypeConstant.DangerAccepted)]
    public async Task SendMessage_ShouldCallInsertBatchMessage(int issueTypeId)
    {
        // Arrange
        const string messageXml = "<Messages>Content</Messages>";
        const int messageId = 1111;
        messageItemService.CreateMessageItems(Arg.Any<IEnumerable<ReceivedUser>>(), Arg.Compat.Any<int>())
            .ReturnsForAnyArgs(messageXml);

        var service = new SendMessageServiceImp(repository, messageItemService, null);

        // Act
        await service.SendMessage(fakeTicketEmailModels, messageId, issueTypeId, 8);

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
        const int fakeSubIssueType = 11;
        const int fakeSportType = 22;
        repository.GetAsync<VoidedReason>(Arg.Any<GetReasonNameCriteria>()).ReturnsNull();
        var service = new SendMessageServiceImp(repository, null, null);

        var actual = await service.InitializeVoidReason(true, fakeSubIssueType, fakeSportType, null);

        Assert.Null(actual.ReasonEnglish);
        Assert.Null(actual.ReasonChineseSimplified);
        await repository.Received(1).GetAsync<VoidedReason>(Arg.Is<GetReasonNameCriteria>(x =>
            x.SubIssueTypeId == fakeSubIssueType && x.SportType == fakeSportType));
    }

    [Fact]
    public async Task InitializeVoidReason_WhenIsAutoVoid_ReturnExpectedVoidReason()
    {
        const int fakeSubIssueType = 11;
        const int fakeSportType = 22;
        var fakeReason = Builder<VoidedReason>.CreateNew().Build();
        repository.GetAsync<VoidedReason>(Arg.Any<GetReasonNameCriteria>()).Returns(fakeReason);
        ticketVoidingSystemHttpClientService.GetSportTypeNameFromBetList(fakeSportType, Arg.Any<string>()).Returns("a");
        var service = new SendMessageServiceImp(repository, null, ticketVoidingSystemHttpClientService);

        var actual = await service.InitializeVoidReason(true, fakeSubIssueType, fakeSportType, null);

        Assert.NotNull(actual.ReasonEnglish);
        Assert.NotNull(actual.ReasonChineseSimplified);
        await repository.Received(1).GetAsync<VoidedReason>(Arg.Is<GetReasonNameCriteria>(x =>
            x.SubIssueTypeId == fakeSubIssueType && x.SportType == fakeSportType));
        await ticketVoidingSystemHttpClientService.Received(2)
            .GetSportTypeNameFromBetList(Arg.Is<int>(x => x == fakeSportType), Arg.Any<string>());
    }

    [Fact]
    public async Task InsertReason_ShouldReturnReasonId()
    {
        // Arrange
        var criteria = new InsertReasonCommand
        {
            ReasonEnglish = "Reason English",
            ReasonChineseSimplified = "Reason Chinese Simplified",
            ReasonId = 12345
        };
        repository.When(repo => repo.ExecuteAsync(criteria)).Do(_ => { criteria.ReasonId = 12345; });

        repository.ExecuteAsync(Arg.Compat.Do<InsertReasonCommand>(x =>
        {
            x.ReasonEnglish = criteria.ReasonEnglish;
            x.ReasonChineseSimplified = criteria.ReasonChineseSimplified;
            x.ReasonId = criteria.ReasonId;
        })).Returns(999);

        var service = new SendMessageServiceImp(repository, null, null);

        // Act
        var actual = await service.InsertReason("Reason English", "Reason Chinese Simplified");

        // Assert
        Assert.Equal(12345, actual);
    }

    [Fact]
    public async Task GetVoidedReason_ReturnVoidedReasonModel()
    {
        // Arrange
        var getVoidedReasonCriteria = new GetVoidedReasonCriteria { ReasonId = 12345 };
        var expected = new VoidedReason
        {
            ReasonId = 12345,
            ReasonEnglish = "Reason English",
            ReasonChineseSimplified = "Reason Chinese Simplified"
        };
        repository.GetAsync<VoidedReason>(getVoidedReasonCriteria).ReturnsForAnyArgs(expected);

        var service = new SendMessageServiceImp(repository, null, null);

        // Act
        var actual = await service.GetVoidedReason(12345);

        // Assert
        Assert.Equal(expected, actual);
        await repository.Received(1)
            .GetAsync<VoidedReason>(Arg.Compat.Is<GetVoidedReasonCriteria>(x => x.ReasonId == 12345));
    }

    [Fact]
    public async Task InsertMessageIntoAdminSite_ShouldCallRepository()
    {
        // Arrange
        var message = new Message
        {
            ContentEnglish = "Content English",
            ContentChineseTraditional = "Content Chinese",
            CreatorId = 8
        };

        var service = new SendMessageServiceImp(repository, messageItemService, null);

        // Act
        await service.InsertMessageIntoAdminSite(message);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Compat.Is<InsertMessageIntoAdminSiteCriteria>(x =>
            x.men == message.ContentEnglish
            && x.mch == message.ContentChineseTraditional));
    }
}