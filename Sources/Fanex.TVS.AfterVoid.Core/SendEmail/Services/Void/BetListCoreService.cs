using AutoMapper;
using Fanex.BetList.Core.Entities;
using Fanex.BetList.Core.TicketHelper;
using Fanex.Common;
using Fanex.TVS.AfterVoid.Core.Extensions;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Extensions;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels;
using Fanex.TVS.AfterVoid.Core.Services;
using Newtonsoft.Json;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services.Void
{
    public interface IBetListCoreService
    {
        TicketEmailViewModel GetTicket(TicketEmailModel item, Match match, SiteEmail site);
    }

    public class BetListCoreService : IBetListCoreService
    {
        private readonly IMapper mapper;
        private readonly BetListService betListService;

        public BetListCoreService(BetListService betListService, IMapper mapper)
        {
            this.betListService = betListService;
            this.mapper = mapper;
        }

        public TicketEmailViewModel GetTicket(TicketEmailModel item, Match match, SiteEmail site)
        {
            var viewModel = new TicketEmailViewModel();
            var ticket = CreateTicket(item, match);
            var ticketHelper = new TicketHelper
            {
                IsShowOddsWithChoice = item.IsMixParlay,
                ShowSystemParlayDetail = false
            };
            viewModel.Odds = betListService.GetOdds(ticket, false);
            viewModel.Stake = betListService.BuildStake(ticket, ticketHelper);
            viewModel.BetType = betListService.GetBetTypeName(ticket);
            viewModel.BetTypeSimplified = betListService.GetBetTypeNameSimplified(ticket);
            viewModel.Status = betListService.GetStatus(ticket, ticketHelper, null, false);
            viewModel.Choice = BuildChoice(ticket, ticketHelper, item, match, site);
            viewModel.Handicap = betListService.GetHandicap(ticket, ticketHelper, null, false);

            return viewModel;
        }

        private Ticket CreateTicket(TicketEmailModel item, Match match)
        {
            var ticket = mapper.Map<Ticket>(item);
            ticket.Currency = item.CurrencyName;
            ticket.Winlost = Converter.ToDecimal(item.Winlost);
            ticket.OddsType = item.IsMixParlay ? string.Empty : item.OddsType.ToString();
            ticket.BetId = item.IsMixParlay ? default : item.BetId;
            ticket.WalletType = item.WalletType.SetWalletType(item.RefCode);
            ticket.RefCode = 0;
            ticket.SettlementStatus = item.IsMixParlay ? default(byte) : item.SettlementStatus;
            UpdateTicketWithMatch(ticket, match);

            return ticket;
        }

        private static void UpdateTicketWithMatch(Ticket ticket, Match match)
        {
            if (match != null)
            {
                ticket.HomeId = match.HomeId;
                ticket.AwayId = match.AwayId;
                ticket.SportTypeId = match.SportType;
                ticket.LeagueId = match.LeagueId;
                ticket.IsNeutral = match.IsNeutral;
                ticket.MatchCode = match.MatchCode;
            }
        }

        private string BuildChoice(ITicket ticket, TicketHelper ticketHelper, TicketEmailModel item, Match match, SiteEmail site)
        {
            string choice;
            if (item.IsMixParlay)
            {
                var mainChoice = betListService.BuildChoice(ticket, ticketHelper, null, false);
                SetValueSubTicket(ticket, item);
                var subChoice = betListService.BuildChoice(ticket, ticketHelper, null, false);
                return mainChoice.Replace("<div id=", $"<div id='dvFavorite_{item.SubTransId}'>{subChoice}</div><div id=");
            }

            choice = betListService.BuildChoiceExcel(ticket, ticketHelper, null, false).Replace("\n", "<br/>");

            if (match != null && match.LeagueGroupId.IsLeagueGroupIdSaBa() && site.IsChinaView)
            {
                var serializedChoice = JsonConvert.SerializeObject(choice);
                choice = JsonConvert.DeserializeObject<string>(serializedChoice.Replace("沙巴", "").Replace("SABA ", ""));
            }

            return choice;
        }

        private static void SetValueSubTicket(ITicket ticket, TicketEmailModel item)
        {
            ticket.BetCheck = item.SubBetCheck;
            ticket.BetTeam = item.SubBetTeam;
            ticket.BetTypeId = item.SubBetType;
            ticket.Handicap1 = item.SubHdp1;
            ticket.Handicap2 = item.SubHdp2;
            ticket.Odds = item.SubOdds;
            ticket.TransDesc = item.SubTransDesc;
        }
    }
}