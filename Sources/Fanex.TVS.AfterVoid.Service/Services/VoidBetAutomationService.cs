using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Criteria.MultiSiteDb;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface VoidBetAutomationService
{
    Task<(VoidBetAutomationInfo, bool, string, IEnumerable<EmailResultModel>)> GetVoidBetAutomationRunAfterVoid();

    Task<RequestDetailViewModel> GetRequestViewModelById(int requestId, int matchId);

    Task<RequestSimpleInfo> GetRequestSimpleInfo(int requestId, int? matchId, int? requestDetailId);

    Task UpdateServiceStatusByVoidingHistory(int requestId, int voidingHistoryId, int statusId);

    Task UpdateAfterVoidStep(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory);

    Task UpdateEmailResultByVoidingHistory(int requestId, int voidingHistoryId, string content);

    Task<IEnumerable<TicketEmailModel>> GetVoidedTicketsByVoidingHistory(int voidingHistoryId, int siteDbGroupId,
        bool isInsertRoutineTask);

    Task<VoidDataXmlModel> GetVoidDataFromVoidingHistory(int voidingHistoryId);

    Task<IEnumerable<VoidDataXmlModel>> GetAllVoidDataFromMultiVoidingHistory(IEnumerable<int> voidingHistoryIds);

    Task<Match> GetMatchInfo(int matchId);

    Task UpdateTicketVoidingHistory(int voidingHistoryId, int? reasonId, int? messageId, int? messageIdSpecial);

    Task<IEnumerable<TicketRecoverEmailModel>> GetRecoveredTicketsByVoidingHistory(int requestId, int voidingHistoryId, int siteDbGroupId);
}

public class VoidBetAutomationServiceImp(
    IDynamicRepository repository,
    MatchService matchService,
    MessageContentParameterService messageContentParameterService)
    : VoidBetAutomationService
{
    public async Task<(VoidBetAutomationInfo, bool, string, IEnumerable<EmailResultModel>)> GetVoidBetAutomationRunAfterVoid()
    {
        var criteria = new GetAutomationInfoAfterVoidCriteria
        {
            OnlineServerId = string.Join(",", SiteDbGroupConstant.ServerList.Select(x => x.Id))
        };
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

    public async Task<RequestDetailViewModel> GetRequestViewModelById(int requestId, int matchId)
    {
        var (requests, requestEvents, playerNames) = await GetRequestById(requestId);
        var firstRequest = requests.First();
        var viewModel = new RequestDetailViewModel
        {
            Request = firstRequest,
            EventByMatch = IsIssueTypeHasMatchId(firstRequest.IssueTypeId) ? requestEvents.FirstOrDefault(x => x.MatchId == matchId)
                : requestEvents.FirstOrDefault(),
            PlayerNamesSelected = messageContentParameterService.GetPlayerNamesSelected(firstRequest.IssueTypeId, playerNames.First()),
            SportType = requestEvents.Select(x => x.SportType).Distinct().Count() == 1
                ? requestEvents.First().SportType : null
        };

        return viewModel;
    }

    private static bool IsIssueTypeHasMatchId(int issueTypeId)
    {
        int[] issueTypeHasMatchId =
        [
            IssueTypeConstant.WrongHDP, IssueTypeConstant.Wrong1stHDP, IssueTypeConstant.WrongOU,
            IssueTypeConstant.Wrong1stOU, IssueTypeConstant.DangerAccepted, IssueTypeConstant.RedCard,
            IssueTypeConstant.EarlyKickOffTime, IssueTypeConstant.WrongKickOffTime
        ];

        return issueTypeHasMatchId.Contains(issueTypeId);
    }

    public async Task<RequestSimpleInfo> GetRequestSimpleInfo(int requestId, int? matchId, int? requestDetailId)
    {
        var criteria = new GetRequestSimpleInfoCriteria
        {
            RequestId = requestId,
            MatchId = matchId,
            RequestDetailId = requestDetailId
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
            criteria.IsDeletedZzzBetTrans = afterVoidHistory.IsDeletedZzzBetTrans;
            criteria.IsDeletedMessage = afterVoidHistory.IsDeletedMessage;
            criteria.IsDeletedVoidedTransaction = afterVoidHistory.IsDeletedVoidedTransaction;
            criteria.IsSentToMTS = afterVoidHistory.IsSentToMTS;
            criteria.VoidResults = afterVoidHistory.VoidResults;
        }

        await repository.ExecuteAsync(criteria);
    }

    public async Task<IEnumerable<TicketEmailModel>> GetVoidedTicketsByVoidingHistory(int voidingHistoryId, int siteDbGroupId, bool isInsertRoutineTask)
    {
        var checkItUser = await CheckRequestOfItUser(voidingHistoryId) ?? new CheckRequestOfITUserModel();
        var criteria = new GetTicketsEmailCriteria
        {
            SiteDbGroupId = siteDbGroupId,
            VoidingHistoryId = voidingHistoryId,
            IsITSupport = checkItUser.IsITSupport,
            IsITSupportCreatedRequest = checkItUser.IsITSupportCreatedRequest,
            IsInsertRoutineTask = isInsertRoutineTask
        };

        return await repository.FetchAsync<TicketEmailModel>(criteria);
    }

    private async Task<CheckRequestOfITUserModel> CheckRequestOfItUser(int voidingHistoryId)
    {
        var criteria = new CheckRequestOfITUserCriteria
        {
            VoidingHistoryId = voidingHistoryId
        };

        var result = await repository.GetAsync<CheckRequestOfITUserModel>(criteria);
        return result;
    }

    public async Task<VoidDataXmlModel> GetVoidDataFromVoidingHistory(int voidingHistoryId)
    {
        var criteria = new GetVoidingHistoryCriteria
        {
            VoidingHistoryIds = $"{voidingHistoryId}"
        };

        var results = await repository.FetchAsync<VoidDataXmlModel>(criteria);
        return results.First();
    }

    public async Task<IEnumerable<VoidDataXmlModel>> GetAllVoidDataFromMultiVoidingHistory(IEnumerable<int> voidingHistoryIds)
    {
        var criteria = new GetVoidingHistoryCriteria
        {
            VoidingHistoryIds = string.Join(",", voidingHistoryIds)
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

    public async Task<IEnumerable<TicketRecoverEmailModel>> GetRecoveredTicketsByVoidingHistory(int requestId, int voidingHistoryId, int siteDbGroupId)
    {
        var checkItUser = await CheckRequestOfItUser(voidingHistoryId) ?? new CheckRequestOfITUserModel();
        var criteria = new GetTicketsRecoverEmailCriteria
        {
            RequestId = requestId,
            SiteDbGroupId = siteDbGroupId,
            IsITSupport = checkItUser.IsITSupport,
            IsITSupportCreatedRequest = checkItUser.IsITSupportCreatedRequest,
            IsInsertRoutineTask = true
        };

        return await repository.FetchAsync<TicketRecoverEmailModel>(criteria);
    }

    public async Task<(IEnumerable<RequestModel>, IEnumerable<RequestEventModel>, IEnumerable<PlayerName>)> GetRequestById(int requestId)
    {
        var criteria = new SelectRequestCriteria
        {
            RequestId = requestId
        };
        var (requests, requestEvents, playerNames) = await repository.FetchMultipleAsync<RequestModel, RequestEventModel, PlayerName>(criteria);
        UpdateRequestDetailProperties(requests.First().IssueTypeId, requestEvents);

        return (requests, requestEvents, playerNames);
    }

    public void UpdateRequestDetailProperties(int issueTypeId, IEnumerable<RequestEventModel> models)
    {
        if (IsIssueTypesHaveExtraInfoJsonFormat(issueTypeId))
        {
            return;
        }

        foreach (var model in models)
        {
            var root = model.RequestExtraInfo.ParseXmlToObject<RequestExtraInfoXml>();

            model.CurrencyIds = root.CurrencyIds;
            model.LicenseeNames = root.LicenseeNames;
            model.WrongName = root.WrongName;
            model.ActualName = root.ActualName;
            model.WrongNameT = root.WrongNameT;
            model.ActualNameT = root.ActualNameT;
            model.WrongNameS = root.WrongNameS;
            model.ActualNameS = root.ActualNameS;
        }
    }

    private static bool IsIssueTypesHaveExtraInfoJsonFormat(int issueTypeId)
    {
        var issueTypesHasJsonFormat = new[] { IssueTypeConstant.WrongMarketOffered, IssueTypeConstant.TechnicalError, IssueTypeConstant.MatchEarlyCompleted };
        return issueTypesHasJsonFormat.Contains(issueTypeId);
    }
}