using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface MessageContentParameterService
{
    TemplateContent GetPlayerNamesSelected(int issueTypeId, PlayerName playerName);

    bool IsAllLiveTickets(IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets);

    Task<TemplateContent> GetSpecialBetTypeContent(int issueTypeId, IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets);
}

public class MessageContentParameterServiceImp : MessageContentParameterService
{
    private readonly TVSWebSiteHttpClientService tvsWebSiteHttpClientService;

    public MessageContentParameterServiceImp(TVSWebSiteHttpClientService tvsWebSiteHttpClientService)
    {
        this.tvsWebSiteHttpClientService = tvsWebSiteHttpClientService;
    }

    public TemplateContent GetPlayerNamesSelected(int issueTypeId, PlayerName playerName)
    {
        if (issueTypeId == IssueTypeConstant.NotParticipate)
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

        return null;
    }

    public bool IsAllLiveTickets(IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        return normalTickets.All(x => x.IsLive) && specialTickets.All(x => x.IsLive);
    }

    public async Task<TemplateContent> GetSpecialBetTypeContent(int issueTypeId, IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        if (issueTypeId is IssueTypeConstant.WrongMarketOffered or IssueTypeConstant.TechnicalError)
        {
            return await GetSpecialBetTypeContent(normalTickets, specialTickets);
        }

        return new TemplateContent();
    }

    private async Task<TemplateContent> GetSpecialBetTypeContent(IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        var betTypeNormal = normalTickets.Select(x => x.IsMixParlay ? x.SubBetType : x.BetTypeId);
        var betTypeSpecial = specialTickets.Select(x => x.IsMixParlay ? x.SubBetType : x.BetTypeId);
        var distinctBetType = betTypeNormal.Union(betTypeSpecial).Distinct().ToList();

        if (distinctBetType.Count == 1)
        {
            var emailTickets = normalTickets.Union(specialTickets).Select(x => new TicketEmailModel
            {
                IsMixParlay = x.IsMixParlay,
                BetTypeId = x.BetTypeId,
                BetId = x.BetId,
                SubBetType = x.SubBetType,
                SubBetCheck = x.SubBetCheck
            }).DistinctBy(x => (x.IsMixParlay, x.BetTypeId, x.BetId, x.SubBetType, x.SubBetCheck));

            return await tvsWebSiteHttpClientService.GetSpecialBetTypeContent(emailTickets);
        }

        return new TemplateContent();
    }
}