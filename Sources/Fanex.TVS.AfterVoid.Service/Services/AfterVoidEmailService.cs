using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface AfterVoidEmailService
{
    Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo,
        IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message);
}

public class AfterVoidEmailServiceImp : AfterVoidEmailService
{
    private readonly TVSNewSiteHttpClientService tvsNewSiteHttpClientService;
    private readonly IDynamicRepository repository;

    public AfterVoidEmailServiceImp(TVSNewSiteHttpClientService tvsNewSiteHttpClientService, IDynamicRepository repository)
    {
        this.tvsNewSiteHttpClientService = tvsNewSiteHttpClientService;
        this.repository = repository;
    }

    public async Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message)
    {
        if (Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
        {
            var subIssueType = await GetSubIssueTypeById(automationInfo.RequestInfo.EventByMatch.SubIssueTypeId);
            reason.ReasonEnglish = $"{reason.ReasonEnglish}({subIssueType.SubIssueTypeName})";
            reason.ReasonChineseSimplified = $"{reason.ReasonChineseSimplified}({subIssueType.SubIssueTypeNameCS})";
        }

        var sendEmailApiModel = CreateSendEmailApiModel(automationInfo, emailTickets, match, reason, message);
        var (isSentEmail, results) = await tvsNewSiteHttpClientService.SendEmail(sendEmailApiModel);
        if (isSentEmail)
        {
            return results;
        }

        throw new ExternalException(nameof(AfterVoidEmailService.Execute));
    }

    private async Task<SubIssueType> GetSubIssueTypeById(int subId)
    {
        var subIssueTypes = await repository.FetchAsync<SubIssueType>(new GetSubIssueTypeCriteria());

        return subIssueTypes.FirstOrDefault(x => x.SubIssueTypeId == subId);
    }

    private static SendEmailApiModel CreateSendEmailApiModel(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message)
    {
        return new SendEmailApiModel
        {
            RequestId = automationInfo.RequestId,
            FilterBy = automationInfo.RequestInfo.EventByMatch.FilterBy,
            SportType = automationInfo.RequestInfo.Request.SportType,
            IssueTypeId = automationInfo.RequestInfo.Request.IssueTypeId,
            VoidingHistoryId = automationInfo.VoidingHistoryId,
            NegativeBalanceUsers = automationInfo.NegativeBalanceUsers,
            Match = match,
            Message = message,
            VoidedReason = reason,
            EmailTickets = emailTickets
        };
    }
}