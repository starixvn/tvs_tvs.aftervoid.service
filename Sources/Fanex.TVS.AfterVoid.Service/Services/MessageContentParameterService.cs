using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface MessageContentParameterService
{
    ContentByLanguageModel GetPlayerNamesSelected(int issueTypeId, PlayerName playerName);

    bool IsAllLiveTickets(IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets);

    Task<ContentByLanguageModel> GetSpecialBetTypeContent(int issueTypeId, IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets);
}

public class MessageContentParameterServiceImp(TVSWebSiteHttpClientService tvsWebSiteHttpClientService)
    : MessageContentParameterService
{
    public ContentByLanguageModel GetPlayerNamesSelected(int issueTypeId, PlayerName playerName)
    {
        if (issueTypeId != IssueTypeConstant.NotParticipate)
        {
            return null;
        }

        var playerNamesSelected = new ContentByLanguageModel
        {
            ContentEnglish = playerName.PlayerNamesEnglish,
            ContentChineseSimplified = playerName.PlayerNamesChineseSimplified,
            ContentNewChineseSimplified = playerName.PlayerNamesChineseSimplified,
            ContentChineseTraditional = playerName.PlayerNamesChineseTraditional
        };

        return playerNamesSelected;
    }

    public bool IsAllLiveTickets(IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        return normalTickets.All(x => x.IsLive) && specialTickets.All(x => x.IsLive);
    }

    public async Task<ContentByLanguageModel> GetSpecialBetTypeContent(int issueTypeId, IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        if (issueTypeId is IssueTypeConstant.WrongMarketOffered or IssueTypeConstant.TechnicalError)
        {
            return await GetSpecialBetTypeContent(normalTickets, specialTickets);
        }

        return new ContentByLanguageModel();
    }

    private async Task<ContentByLanguageModel> GetSpecialBetTypeContent(IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        var betTypeNormal = normalTickets.Select(x => x.IsMixParlay ? x.SubBetType : x.BetTypeId);
        var betTypeSpecial = specialTickets.Select(x => x.IsMixParlay ? x.SubBetType : x.BetTypeId);
        var distinctBetType = betTypeNormal.Union(betTypeSpecial).Distinct().ToList();

        if (distinctBetType.Count != 1)
        {
            return new ContentByLanguageModel();
        }

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
}