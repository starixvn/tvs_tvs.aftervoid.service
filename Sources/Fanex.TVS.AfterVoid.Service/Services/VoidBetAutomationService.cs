using System;
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

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface VoidBetAutomationService
{
    Task<(VoidBetAutomationInfo, bool, string, IEnumerable<EmailResultModel>)> GetVoidBetAutomationRunAfterVoid();

    Task<RequestDetailViewModel> GetRequestById(int requestId, int matchId);

    Task<RequestSimpleInfo> GetRequestSimpleInfo(int requestId, int matchId);

    Task UpdateServiceStatusByVoidingHistory(int requestId, int voidingHistoryId, int statusId);

    Task UpdateMessageIdByVoidingHistory(int requestId, int voidingHistoryId, int messageId);

    Task UpdateAfterVoidStep(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory);

    Task UpdateAllAfterVoidSteps(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory);

    Task UpdateEmailResultByVoidingHistory(int requestId, int voidingHistoryId, string content);

    Task<IEnumerable<TicketEmailModel>> GetVoidedTicketsByVoidingHistory(int voidingHistoryId, int siteDbGroupId,
        bool isInsertRoutineTask);

    Task<VoidDataXmlModel> GetVoidDataFromVoidingHistory(int voidingHistoryId);

    Task<Match> GetMatchInfo(int matchId);

    Task UpdateTicketVoidingHistory(int voidingHistoryId, int? statusId, int? reasonId, int? messageId,
        string transIds, DateTime? minWinLostDate, DateTime? maxWinLostDate);
}

public class VoidBetAutomationServiceImp : VoidBetAutomationService
{
    private readonly IDynamicRepository repository;

    public VoidBetAutomationServiceImp(IDynamicRepository repository)
    {
        this.repository = repository;
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
        var (requests, requestEvents, _) = await repository.FetchMultipleAsync<RequestModel, RequestEventModel, Message>(criteria);

        var firstRequest = requests.FirstOrDefault();
        var viewModel = new RequestDetailViewModel
        {
            Request = firstRequest,
            EventByMatch = IsIssueTypeHasMatchId(firstRequest.IssueTypeId) ? requestEvents.FirstOrDefault(x => x.MatchId == matchId)
                : requestEvents.FirstOrDefault()
        };

        return viewModel;
    }

    private static bool IsIssueTypeHasMatchId(int issueTypeId)
    {
        var isWrongHdpGroup = issueTypeId is IssueTypeConstant.WrongHDP or IssueTypeConstant.Wrong1stHDP;
        var isWrongOuGroup = issueTypeId is IssueTypeConstant.WrongOU or IssueTypeConstant.Wrong1stOU;
        return issueTypeId is IssueTypeConstant.DangerAccepted or IssueTypeConstant.RedCard
               || isWrongHdpGroup || isWrongOuGroup;
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

    public async Task UpdateMessageIdByVoidingHistory(int requestId, int voidingHistoryId, int messageId)
    {
        var afterVoidHistory = new VoidBetAutomationInfo
        {
            MessageId = messageId
        };

        await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateMessageIdByVoidingHistory, requestId, voidingHistoryId, null, afterVoidHistory);
    }

    public async Task UpdateAfterVoidStep(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory)
    {
        await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateAfterVoidStep, requestId, voidingHistoryId, null, afterVoidHistory);
    }

    public async Task UpdateAllAfterVoidSteps(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory)
    {
        await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateAllAfterVoidSteps, requestId, voidingHistoryId, null, afterVoidHistory);
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
            criteria.ReasonId = afterVoidHistory.ReasonId;
            criteria.IsSentBatchMessage = afterVoidHistory.IsSentBatchMessage;
            criteria.IsSentMessageToAdminSite = afterVoidHistory.IsSentMessageToAdminSite;
            criteria.IsInsertedVoidedTransaction = afterVoidHistory.IsInsertedVoidedTransaction;
            criteria.IsUpdatedVoidingHistory = afterVoidHistory.IsUpdatedVoidingHistory;
            criteria.IsSentEmail = afterVoidHistory.IsSentEmail;
            criteria.IsSentLetsTalk = afterVoidHistory.IsSentLetsTalk;
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

    public async Task<VoidDataXmlModel> GetVoidDataFromVoidingHistory(int voidingHistoryId)
    {
        var criteria = new GetVoidingHistoryCriteria
        {
            VoidingHistoryIds = $"{voidingHistoryId}"
        };

        return await repository.GetAsync<VoidDataXmlModel>(criteria);
    }

    public async Task<Match> GetMatchInfo(int matchId)
    {
        return await repository.GetAsync<Match>(new GetMatchInfoCriteria { MatchId = matchId, IsMatch3rd = false });
    }

    public async Task UpdateTicketVoidingHistory(int voidingHistoryId, int? statusId, int? reasonId, int? messageId,
        string transIds, DateTime? minWinLostDate, DateTime? maxWinLostDate)
    {
        var command = new UpdateHistoryCommand
        {
            VoidingHistoryId = voidingHistoryId,
            StatusId = statusId,
            ReasonId = reasonId,
            MessageId = messageId,
            TransIds = transIds,
            MinWinLostDate = minWinLostDate,
            MaxWinLostDate = maxWinLostDate
        };

        await repository.ExecuteAsync(command);
    }
}