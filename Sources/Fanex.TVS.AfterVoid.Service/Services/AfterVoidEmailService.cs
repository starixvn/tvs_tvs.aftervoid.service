using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface AfterVoidEmailService
{
    Task<IEnumerable<Result>> Execute(SendEmailStageParameter parameter);
}

public class AfterVoidEmailServiceImp : AfterVoidEmailService
{
    private readonly TVSWebSiteHttpClientService tvsWebSiteHttpClientService;
    private readonly IDynamicRepository repository;

    public AfterVoidEmailServiceImp(TVSWebSiteHttpClientService tvsWebSiteHttpClientService, IDynamicRepository repository)
    {
        this.tvsWebSiteHttpClientService = tvsWebSiteHttpClientService;
        this.repository = repository;
    }

    public async Task<IEnumerable<Result>> Execute(SendEmailStageParameter parameter)
    {
        if (Helper.IsAbnormalBet(parameter.AutomationInfo.RequestInfo.Request.IssueTypeId))
        {
            var subIssueType = await GetSubIssueTypeById(parameter.AutomationInfo.RequestInfo.EventByMatch.SubIssueTypeId);
            parameter.VoidReason.ReasonEnglish = $"{parameter.VoidReason.ReasonEnglish}({subIssueType.SubIssueTypeName})";
            parameter.VoidReason.ReasonChineseSimplified = $"{parameter.VoidReason.ReasonChineseSimplified}({subIssueType.SubIssueTypeNameCS})";
        }

        var sendEmailApiModel = CreateSendEmailApiModel(parameter);
        var (isSentEmail, results) = await tvsWebSiteHttpClientService.SendEmail(sendEmailApiModel);
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

    private static SendEmailApiModel CreateSendEmailApiModel(SendEmailStageParameter parameter)
    {
        return new SendEmailApiModel
        {
            RequestId = parameter.AutomationInfo.RequestId,
            FilterBy = parameter.AutomationInfo.RequestInfo.EventByMatch.FilterBy,
            SportType = parameter.AutomationInfo.RequestInfo.SportType,
            IssueTypeId = parameter.AutomationInfo.RequestInfo.Request.IssueTypeId,
            VoidingHistoryId = parameter.AutomationInfo.VoidingHistoryId,
            NegativeBalanceUsers = parameter.AutomationInfo.NegativeBalanceUsers,
            Match = parameter.MatchInfo,
            Message = parameter.NormalMessage,
            SpecialMessage = parameter.SpecialMessage,
            VoidedReason = parameter.VoidReason,
            EmailTickets = parameter.EmailTickets
        };
    }
}