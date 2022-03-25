using System.Collections.Generic;
using Fanex.BetList.Core.Builder;
using Fanex.BetList.Core.Entities;
using Fanex.BetList.Core.Factories;
using Fanex.BetList.Core.Templates;
using Fanex.BetList.Core.TicketHelper;
using Fanex.TVS.AfterVoid.Core.Extensions;
using Fanex.TVS.AfterVoid.Core.Utilities;
using HtmlAgilityPack;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface BetListService
    {
        Choice_Template BuildChoiceBlock(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap);

        string BuildChoice(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap);

        string BuildChoiceExcel(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap);

        string BuildStake(ITicket ticket, ITicketHelper ticketHelper);

        string GetOdds(ITicket ticket, bool isParlay);

        string GetBettype(object bettypeId, string language);

        string GetBetTypeName(ITicket ticket);

        string GetBetTypeNameSimplified(ITicket ticket);

        string GetBetTypeName(int id, long refNo);

        string GetBetTeam(Choice_Template choice);

        string GetStatus(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowVnIp);

        string GetComboParlayName(ITicket ticket);

        string[] GetComboName(string betTeam, string betCheck, decimal handicap1, decimal handicap2);

        string GetMatchCodeBetradar(string matchCode);

        string GetSportTypeName(int sportType, string languageCode);

        string GetHandicap(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap);
    }

    public class BetListServiceImp : BetListService
    {
        private static BetListHTMLBuilder Builder
        {
            get
            {
                var factory = new BetListHTMLFactory();
                return factory.CreateBuilder();
            }
        }

        private static BetListExcelBuilder ExcelBuilder
        {
            get
            {
                var excelFactory = new BetListExcelFactory();
                return excelFactory.CreateBuilder();
            }
        }

        public Choice_Template BuildChoiceBlock(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap)
        {
            return Builder.BuildChoiceBlock(ticket, ticketHelper, ticketData, isShowScoreMap);
        }

        public string BuildChoice(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap)
        {
            return Builder.BuildChoice(ticket, ticketHelper, ticketData, isShowScoreMap);
        }

        public string BuildChoiceExcel(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap)
        {
            var choiceBuilder = ExcelBuilder.CreateChoiceBuilder(ticket.BetTypeId);
            var choices = choiceBuilder.RenderRTF(ticket, ticketHelper, ticketData, isShowScoreMap, ExcelBuilder.RTFHelper);
            return choices[0].ToString();
        }

        public string BuildStake(ITicket ticket, ITicketHelper ticketHelper)
        {
            return Builder.BuildStake(ticket, ticketHelper);
        }

        public string GetOdds(ITicket ticket, bool isParlay)
        {
            GetCachePropertyById oddsTypeName = RemotingWrapper.GetOddsTypeName;
            var oddHtml = Builder.BuildOdds(ticket, null, oddsTypeName);
            var oddName = OddsText.GetOddsName(oddHtml);
            oddName = isParlay ? string.Empty : oddName;
            return string.IsNullOrWhiteSpace(oddName)
                ? oddHtml.Replace("<br />", " ") : oddHtml.Replace("<br />", " (") + ")";
        }

        public string GetBettype(object bettypeId, string language)
        {
            return RemotingWrapper.GetBettype(bettypeId, language);
        }

        public string GetBetTypeName(ITicket ticket)
        {
            var ticketHelper = new TicketHelper();
            var factory = new BetListBaseBuilder();
            var builder = factory.CreateChoiceBuilder(ticket.BetTypeId);
            return builder.GetBetTypeName(ticket, ticketHelper);
        }

        public string GetBetTypeNameSimplified(ITicket ticket)
        {
            var ticketHelper = new TicketHelper(new ChineseSimplifiedCacheRef());
            var factory = new BetListBaseBuilder();
            var builder = factory.CreateChoiceBuilder(ticket.BetTypeId);
            var betTypeName = builder.GetBetTypeName(ticket, ticketHelper);

            return betTypeName.TranslateChineseSimplified();
        }

        public string GetBetTypeName(int id, long refNo)
        {
            var betTypeId = RemotingWrapper.GetParentIdByBetTypeId(id);
            if (string.IsNullOrWhiteSpace(betTypeId))
            {
                betTypeId = id.ToString();
            }

            var betName = RemotingWrapper.GetBettype(betTypeId, refNo);
            const int FistNextBet = 86;
            const int SecondNextBet = 85;
            return id == FistNextBet || id == SecondNextBet ? $"Next {betName}" : betName;
        }

        public string GetBetTeam(Choice_Template choice)
        {
            var temp = choice;
            temp.ScoreMapIcon = null;
            temp.BetType = null;
            temp.Match = null;
            temp.League = null;
            temp.Handicap = null;
            temp.Score = null;

            return temp;
        }

        public string GetStatus(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowVnIp)
        {
            var factory = new BetListBaseBuilder();
            ticketHelper.HideStatusCashOutHistory = true;
            var statusBuilder = factory.CreateStatusBuilder(ticket.BetTypeId);
            var status = statusBuilder.Render(ticket, ticketHelper, ticketData, isShowVnIp);
            status.CashOutHistory = null;
            status.ShowIP = null;
            status.StatusResult = null;

            return status;
        }

        public string GetComboParlayName(ITicket ticket)
        {
            var comboNames = string.IsNullOrEmpty(ticket.BetCheck) ?
                null : RemotingWrapper.GetComboParlayName(ticket.BetTeam, ticket.BetCheck, ticket.Handicap1, ticket.Handicap2);
            var html = string.Empty;
            var count = comboNames?.Length ?? 0;
            for (var i = 0; i < count; i++)
            {
                html = comboNames?[i] == string.Empty ?
                    html : html + $" <div><span class='combo-name'>{comboNames?[i]}</span></div>";
            }

            return html;
        }

        public string[] GetComboName(string betTeam, string betCheck, decimal handicap1, decimal handicap2)
        {
            return RemotingWrapper.GetComboParlayName(betTeam, betCheck, handicap1, handicap2);
        }

        public string GetMatchCodeBetradar(string matchCode)
        {
            return RemotingWrapper.GetMatchCodeBetradar(matchCode);
        }

        public string GetSportTypeName(int sportType, string languageCode)
        {
            return RemotingWrapper.GetSportName(sportType.ToString(), languageCode);
        }

        public string GetHandicap(ITicket ticket, ITicketHelper ticketHelper, List<ITicketData> ticketData, bool isShowScoreMap)
        {
            var choiceBlock = Builder.BuildChoiceBlock(ticket, ticketHelper, ticketData, isShowScoreMap);
            if (choiceBlock.Handicap?.handicap == null || ticket.BetTypeId.IsHideHandicap())
            {
                return string.Empty;
            }

            var html = choiceBlock.Handicap.handicap;
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return document.DocumentNode.FirstChild?.InnerText;
        }
    }
}