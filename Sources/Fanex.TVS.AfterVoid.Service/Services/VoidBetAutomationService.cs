using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Criteria.MultiSiteDb;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface VoidBetAutomationService
{
    Task<(VoidBetAutomationInfo, bool, string, IEnumerable<EmailResultModel>)> GetVoidBetAutomationRunAfterVoid();

    Task<RequestDetailViewModel> GetRequestById(int requestId, int matchId);

    Task<RequestSimpleInfo> GetRequestSimpleInfo(int requestId, int matchId);

    Task UpdateServiceStatusByVoidingHistory(int requestId, int voidingHistoryId, int statusId);

    Task UpdateAfterVoidStep(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory);

    Task UpdateEmailResultByVoidingHistory(int requestId, int voidingHistoryId, string content);

    Task<IEnumerable<TicketEmailModel>> GetVoidedTicketsByVoidingHistory(int voidingHistoryId, int siteDbGroupId,
        bool isInsertRoutineTask);

    Task<IEnumerable<VoidDataXmlModel>> GetVoidDataFromVoidingHistory(int voidingHistoryId);

    Task<Match> GetMatchInfo(int matchId);

    Task UpdateTicketVoidingHistory(int voidingHistoryId, int? reasonId, int? messageId, int? messageIdSpecial);

    Task<IEnumerable<TicketRecoverEmailModel>> GetRecoveredTicketsByVoidingHistory(string singleTransIds, int voidingHistoryId, int siteDbGroupId);
}

public class VoidBetAutomationServiceImp : VoidBetAutomationService
{
    private readonly IDynamicRepository repository;
    private readonly MatchService matchService;

    public VoidBetAutomationServiceImp(IDynamicRepository repository, MatchService matchService)
    {
        this.repository = repository;
        this.matchService = matchService;
    }

    public async Task<(VoidBetAutomationInfo, bool, string, IEnumerable<EmailResultModel>)> GetVoidBetAutomationRunAfterVoid()
    {
        var criteria = new GetAutomationInfoAfterVoidCriteria();
        var (voidBetAutomationInfoList, voidingHistoryModels, emailResultsHistory) =
            await repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidingHistoryModel, EmailResultModel>(criteria);

        var isLatestInRequest = voidingHistoryModels.Count(x => x.VoidingHistoryStatusId is ExecuteStatus.Running || x.AfterVoidStatusId is ExecuteStatus.Running) == 1;
        var currentRequestStatus = GetCurrentRequestStatus(voidingHistoryModels);

        return (voidBetAutomationInfoList.FirstOrDefault(), isLatestInRequest, currentRequestStatus, emailResultsHistory);
    }

    private static string GetCurrentRequestStatus(IEnumerable<VoidingHistoryModel> voidingHistoryModels)
    {
        if (voidingHistoryModels.Any(x =>
                x.VoidingHistoryStatusId is ExecuteStatus.Failed || x.AfterVoidStatusId is ExecuteStatus.Failed))
        {
            return RequestStatusConstant.Failed;
        }

        return voidingHistoryModels.All(x => x.VoidingHistoryStatusId is ExecuteStatus.Running) ? RequestStatusConstant.Running : RequestStatusConstant.Completed;
    }

    public async Task<RequestDetailViewModel> GetRequestById(int requestId, int matchId)
    {
        var criteria = new SelectRequestCriteria
        {
            RequestId = requestId
        };
        var (requests, requestEvents, playerNames) = await repository.FetchMultipleAsync<RequestModel, RequestEventModel, PlayerName>(criteria);
        var firstRequest = requests.First();
        var viewModel = new RequestDetailViewModel
        {
            Request = firstRequest,
            EventByMatch = IsIssueTypeHasMatchId(firstRequest.IssueTypeId) ? requestEvents.FirstOrDefault(x => x.MatchId == matchId)
                : requestEvents.FirstOrDefault(),
            PlayerNamesSelected = firstRequest.IssueTypeId == IssueTypeConstant.NotParticipate ? GetPlayerNamesSelected(playerNames.First()) : null
        };

        return viewModel;
    }

    private static TemplateContent GetPlayerNamesSelected(PlayerName playerName)
    {
        var playerNamesSelected = new TemplateContent
        {
            MsgEnglish = playerName.PlayerNamesEnglish,
            MsgChineseSimplified = playerName.PlayerNamesChineseSimplified,
            MsgChineseNewSimplified = playerName.PlayerNamesChineseSimplified,
            MsgChineseTraditional = playerName.PlayerNamesChineseTraditional
        };

        return playerNamesSelected;
    }

    private static bool IsIssueTypeHasMatchId(int issueTypeId)
    {
        int[] issueTypeHasMatchId = { IssueTypeConstant.WrongHDP, IssueTypeConstant.Wrong1stHDP, IssueTypeConstant.WrongOU,
            IssueTypeConstant.Wrong1stOU, IssueTypeConstant.DangerAccepted, IssueTypeConstant.RedCard,
            IssueTypeConstant.EarlyKickOffTime, IssueTypeConstant.WrongKickOffTime };

        return issueTypeHasMatchId.Contains(issueTypeId);
    }

    public async Task<RequestSimpleInfo> GetRequestSimpleInfo(int requestId, int matchId)
    {
        var criteria = new GetRequestSimpleInfoCriteria
        {
            RequestId = requestId,
            MatchId = matchId
        };
        var request = await repository.GetAsync<RequestSimpleInfo>(criteria);

        return request;
    }

    public async Task UpdateServiceStatusByVoidingHistory(int requestId, int voidingHistoryId, int statusId)
    {
        await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateStatusByVoidingHistory, requestId, voidingHistoryId, statusId, null);
    }

    public async Task UpdateAfterVoidStep(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory)
    {
        await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateAfterVoidStep, requestId, voidingHistoryId, null, afterVoidHistory);
    }

    public async Task UpdateEmailResultByVoidingHistory(int requestId, int voidingHistoryId, string content)
    {
        var afterVoidHistory = new VoidBetAutomationInfo
        {
            VoidResults = content
        };

        await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateEmailsResultByVoidingHistory, requestId, voidingHistoryId, null, afterVoidHistory);
    }

    private async Task UpdateVoidBetAutomationServiceInfo(int updateType, int? requestId, int? voidingHistoryId, int? statusId, VoidBetAutomationInfo afterVoidHistory)
    {
        var criteria = new UpdateVoidBetAutomationInfoCriteria
        {
            UpdateType = updateType,
            RequestId = requestId,
            VoidingHistoryId = voidingHistoryId,
            StatusId = statusId
        };

        if (afterVoidHistory != null)
        {
            criteria.IsRunExternalService = afterVoidHistory.IsRunExternalService;
            criteria.MessageId = afterVoidHistory.MessageId;
            criteria.MessageIdSpecial = afterVoidHistory.MessageIdSpecial;
            criteria.ReasonId = afterVoidHistory.ReasonId;
            criteria.IsSentBatchMessage = afterVoidHistory.IsSentBatchMessage;
            criteria.IsSentMessageToAdminSite = afterVoidHistory.IsSentMessageToAdminSite;
            criteria.IsInsertedVoidedTransaction = afterVoidHistory.IsInsertedVoidedTransaction;
            criteria.IsUpdatedVoidingHistory = afterVoidHistory.IsUpdatedVoidingHistory;
            criteria.IsSentEmail = afterVoidHistory.IsSentEmail;
            criteria.IsSentLetsTalk = afterVoidHistory.IsSentLetsTalk;
            criteria.IsDeletedZzzBetTrans = afterVoidHistory.IsDeletedZzzBetTrans;
            criteria.IsDeletedMessage = afterVoidHistory.IsDeletedMessage;
            criteria.IsDeletedVoidedTransaction = afterVoidHistory.IsDeletedVoidedTransaction;
            criteria.VoidResults = afterVoidHistory.VoidResults;
        }

        await repository.ExecuteAsync(criteria);
    }

    public async Task<IEnumerable<TicketEmailModel>> GetVoidedTicketsByVoidingHistory(int voidingHistoryId, int siteDbGroupId, bool isInsertRoutineTask)
    {
        var checkITUser = await CheckRequestOfITUser(voidingHistoryId) ?? new CheckRequestOfITUserModel();
        var criteria = new GetTicketsEmailCriteria
        {
            SiteDbGroupId = siteDbGroupId,
            VoidingHistoryId = voidingHistoryId,
            IsITSupport = checkITUser.IsITSupport,
            IsITSupportCreatedRequest = checkITUser.IsITSupportCreatedRequest,
            IsInsertRoutineTask = isInsertRoutineTask
        };

        return await repository.FetchAsync<TicketEmailModel>(criteria);
    }

    private async Task<CheckRequestOfITUserModel> CheckRequestOfITUser(int voidingHistoryId)
    {
        var criteria = new CheckRequestOfITUserCriteria
        {
            VoidingHistoryId = voidingHistoryId
        };

        var result = await repository.GetAsync<CheckRequestOfITUserModel>(criteria);
        return result;
    }

    public async Task<IEnumerable<VoidDataXmlModel>> GetVoidDataFromVoidingHistory(int voidingHistoryId)
    {
        var criteria = new GetVoidingHistoryCriteria
        {
            VoidingHistoryIds = $"{voidingHistoryId}"
        };

        return await repository.FetchAsync<VoidDataXmlModel>(criteria);
    }

    public async Task<Match> GetMatchInfo(int matchId)
    {
        var match = await repository.GetAsync<Match>(new GetMatchInfoCriteria { MatchId = matchId, IsMatch3rd = false });
        match = matchService.UpdateMatchInfoFromRemotingRef(match);
        return match;
    }

    public async Task UpdateTicketVoidingHistory(int voidingHistoryId, int? reasonId, int? messageId, int? messageIdSpecial)
    {
        var command = new UpdateHistoryCommand
        {
            VoidingHistoryId = voidingHistoryId,
            StatusId = ExecuteStatus.Done,
            ReasonId = reasonId,
            MessageId = messageId,
            MessageIdSpecial = messageIdSpecial
        };

        await repository.ExecuteAsync(command);
    }

    public async Task<IEnumerable<TicketRecoverEmailModel>> GetRecoveredTicketsByVoidingHistory(string singleTransIds, int voidingHistoryId, int siteDbGroupId)
    {
        var checkITUser = await CheckRequestOfITUser(voidingHistoryId) ?? new CheckRequestOfITUserModel();
        var criteria = new GetTicketsRecoverEmailCriteria
        {
            SiteDbGroupId = siteDbGroupId,
            SingleTransIds = singleTransIds,
            IsITSupport = checkITUser.IsITSupport,
            IsITSupportCreatedRequest = checkITUser.IsITSupportCreatedRequest,
            IsInsertRoutineTask = true
        };

        return await repository.FetchAsync<TicketRecoverEmailModel>(criteria);
    }
}