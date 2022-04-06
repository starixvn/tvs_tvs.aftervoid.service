using System;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Extensions;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services.Void
{
    public interface ITicketService
    {
        TicketEmailViewModel GetTicket(TicketEmailModel item, Match match, SiteEmail site);
    }

    public class TicketService : ITicketService
    {
        private readonly IBetListCoreService betListCoreService;

        public TicketService(IBetListCoreService betListCoreService)
        {
            this.betListCoreService = betListCoreService;
        }

        public TicketEmailViewModel GetTicket(TicketEmailModel item, Match match, SiteEmail site)
        {
            var ticket = betListCoreService.GetTicket(item, match, site);
            ticket.BetId = item.TransId.ToString();
            ticket.Member = item.UserName;
            ticket.IsVipMember = item.IsVipMember;
            ticket.OperatorID = item.OperatorID;
            ticket.TransDate = item.TransDate.ToString(FormatKey.LongDateFormatTimeWithSlash);
            ticket.Currency = item.CurrencyName;
            ticket.Score = item.IsLive && (!item.BetTypeId.HideScore() || item.IsMixParlay)
                ? $"{item.LiveHomeScore} - {item.LiveAwayScore}" : string.Empty;
            ticket.WinLoss = item.Status.IsRunningOrWaiting() ? "-" : Convert.ToDecimal(item.Winlost).ToString("#,##0.##");
            ticket.DepositName = item.UserName2;
            ticket.BetFrom = Helper.GetBetFrom(item.BetFrom);
            ticket.Stake = ticket.Stake.Replace("Rebate", "(Rebate)");

            return ticket;
        }
    }
}