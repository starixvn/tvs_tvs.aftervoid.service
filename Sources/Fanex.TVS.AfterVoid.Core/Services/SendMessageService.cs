using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Criteria;
using Fanex.TVS.AfterVoid.Core.Extensions;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface SendMessageService
    {
        Task<Message> InitializeMessageContentForAutoVoid(Match match, bool isHasSettledTicket, int issueTypeId, int? filterBy);

        Message InitializeMessageContentForManualVoid(VoidDataXmlModel voidData);

        Task<Message> GetMessage(int messageId);

        Task<int> InsertMessage(Message message, int matchId, int? leagueGroupId, int issueTypeId);

        Task<VoidedReason> InitializeVoidReason(bool isAutoVoid, int subIssueTypeId, int sportType, VoidDataXmlModel voidData);

        Task<int> InsertReason(string reasonEnglish, string reasonChineseSimplified);

        Task<VoidedReason> GetVoidedReason(int reasonId);

        Task SendMessage(IEnumerable<TicketEmailModel> tickets, int messageId, int issueTypeId, int creatorId);

        Task InsertVoidedTransaction(IEnumerable<TicketEmailModel> tickets, int messageId, int reasonId, int issueTypeId, int subIssueTypeId, int matchId);

        Task InsertMessageIntoAdminSite(Message message);
    }

    public class SendMessageServiceImp : SendMessageService
    {
        private readonly IDynamicRepository repository;
        private readonly MessageItemService messageItemService;
        private readonly BetListService betListService;

        public SendMessageServiceImp(IDynamicRepository repository, MessageItemService messageItemService, BetListService betListService)
        {
            this.repository = repository;
            this.messageItemService = messageItemService;
            this.betListService = betListService;
        }

        public async Task SendMessage(IEnumerable<TicketEmailModel> tickets, int messageId, int issueTypeId, int creatorId)
        {
            var excludedCN88Tickets = ExcludeCN88Tickets(tickets, Helper.IsLikelyAbnormalBet(issueTypeId));
            var receivedUsers = GetListSendMessageDb(excludedCN88Tickets);
            var messageXml = messageItemService.CreateMessageItems(receivedUsers, messageId);
            if (!string.IsNullOrWhiteSpace(messageXml))
            {
                await InsertBatchMessage(creatorId, messageXml);
            }
        }

        public async Task InsertVoidedTransaction(IEnumerable<TicketEmailModel> tickets, int messageId, int reasonId, int issueTypeId, int subIssueTypeId, int matchId)
        {
            var criteria = new InsertVoidedTransactionCommand
            {
                MessageId = messageId,
                ReasonId = reasonId,
                IssueTypeId = issueTypeId,
                SubIssueTypeId = subIssueTypeId,
                XmlCustInfo = tickets.CreateCustInfoXml()
            };
            var mainTransIds = string.Join(",", tickets
                                     .Where(x => !x.IsMixParlay)
                                     .Select(x => x.TransId.ToString()));
            var subTransIds = string.Join(",", tickets
                                    .Where(x => x.IsMixParlay)
                                    .Select(x => x.TransId.ToString()));
            if (!string.IsNullOrEmpty(mainTransIds))
            {
                criteria.TransIds = mainTransIds;
                criteria.MatchId = 0;
                await repository.ExecuteAsync(criteria);
            }
            if (!string.IsNullOrEmpty(subTransIds))
            {
                criteria.TransIds = subTransIds;
                criteria.MatchId = matchId;
                await repository.ExecuteAsync(criteria);
            }
        }

        public async Task<Message> InitializeMessageContentForAutoVoid(Match match, bool isHasSettledTicket, int issueTypeId, int? filterBy)
        {
            var templateId = Helper.IsAbnormalBetRealMember(issueTypeId) || (Helper.IsAbnormalBet(issueTypeId) && filterBy == null)
                ? MessageTemplateConstant.AbnormalBet
                : MessageTemplateConstant.AbnormalAccount;
            var templateModels = await repository.FetchAsync<TemplateModel>(new GetTemplatesCriteria()).ConfigureAwait(false);
            var template = templateModels.FirstOrDefault(t => t.TemplateId == templateId);
            ReplaceMessageTemplate(template, match, isHasSettledTicket, issueTypeId, filterBy);

            return new Message
            {
                CreatorId = AutoVoidConstant.ActorId,
                CreatorName = AutoVoidConstant.ActorName,
                ContentEnglish = template.MsgEnglish,
                ContentChineseTraditional = template.MsgChineseTraditional,
                ContentChineseSimplified = template.MsgChineseSimplified,
                ContentNewChineseSimplified = template.MsgChineseNewSimplified
            };
        }

        private void ReplaceMessageTemplate(TemplateModel model, Match match, bool isHasSettledTicket, int issueTypeId, int? filterBy)
        {
            model.MsgEnglish = model.MsgEnglish.Replace("{{Sport type}}", match?.SportTypeName);
            model.MsgChineseTraditional = model.MsgChineseTraditional.Replace("{{Sport type}}", match?.SportTypeNameTraditional);
            model.MsgChineseSimplified = model.MsgChineseSimplified.Replace("{{Sport type}}", match?.SportTypeNameSimplified);
            model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace("{{Sport type}}", match?.SportTypeNameNewSimplified);

            model.MsgEnglish = model.MsgEnglish.Replace("{{Home team name}}", match?.HomeName);
            model.MsgChineseTraditional = model.MsgChineseTraditional.Replace("{{Home team name}}", match?.HomeNameTraditional);
            model.MsgChineseSimplified = model.MsgChineseSimplified.Replace("{{Home team name}}", match?.HomeNameSimplified);
            model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace("{{Home team name}}", match?.HomeNameNewSimplified);

            model.MsgEnglish = model.MsgEnglish.Replace("{{Away team name}}", match?.AwayName);
            model.MsgChineseTraditional = model.MsgChineseTraditional.Replace("{{Away team name}}", match?.AwayNameTraditional);
            model.MsgChineseSimplified = model.MsgChineseSimplified.Replace("{{Away team name}}", match?.AwayNameSimplified);
            model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace("{{Away team name}}", match?.AwayNameNewSimplified);

            model.MsgEnglish = model.MsgEnglish.Replace("{{League name}}", match?.LeagueName);
            model.MsgChineseTraditional = model.MsgChineseTraditional.Replace("{{League name}}", match?.LeagueNameTraditional);
            model.MsgChineseSimplified = model.MsgChineseSimplified.Replace("{{League name}}", match?.LeagueNameSimplified);
            model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace("{{League name}}", match?.LeagueNameNewSimplified);

            model.MsgEnglish = model.MsgEnglish.Replace("{{Event Date}}", match?.EventDate.ToString("dd/MM"));
            model.MsgChineseTraditional = model.MsgChineseTraditional.Replace("{{Event Date}}", match?.EventDate.ToString("dd/MM"));
            model.MsgChineseSimplified = model.MsgChineseSimplified.Replace("{{Event Date}}", match?.EventDate.ToString("dd/MM"));
            model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace("{{Event Date}}", match?.EventDate.ToString("dd/MM"));

            string checkStatmentEnglish = isHasSettledTicket ? "Kindly double check your statement again. " : "";
            string checkStatmentChineseTraditional = isHasSettledTicket ? "請重新檢查您的帳目. " : "";
            string checkStatmentChineseSimplified = isHasSettledTicket ? "请重新检查您的帐目. " : "";
            string checkStatmentChineseNewSimplified = isHasSettledTicket ? "请重新检查您的帐目. " : "";
            model.MsgEnglish = model.MsgEnglish.Replace("{{CheckStatement}}", checkStatmentEnglish);
            model.MsgChineseTraditional = model.MsgChineseTraditional.Replace("{{CheckStatement}}", checkStatmentChineseTraditional);
            model.MsgChineseSimplified = model.MsgChineseSimplified.Replace("{{CheckStatement}}", checkStatmentChineseSimplified);
            model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace("{{CheckStatement}}", checkStatmentChineseNewSimplified);

            string parlayEnglish = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[Parlay]" : "";
            string parlayChineseTraditional = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[串關]" : "";
            string parlayChineseSimplified = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[串关]" : "";
            string parlayChineseNewSimplified = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[串关]" : "";
            model.MsgEnglish = model.MsgEnglish.Replace("{{Parlay}}", parlayEnglish);
            model.MsgChineseTraditional = model.MsgChineseTraditional.Replace("{{Parlay}}", parlayChineseTraditional);
            model.MsgChineseSimplified = model.MsgChineseSimplified.Replace("{{Parlay}}", parlayChineseSimplified);
            model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace("{{Parlay}}", parlayChineseNewSimplified);
        }

        public Message InitializeMessageContentForManualVoid(VoidDataXmlModel voidData)
        {
            return new Message
            {
                ContentChineseTraditional = voidData.ContentChineseTraditional,
                ContentChineseSimplified = voidData.ContentChineseSimplified,
                ContentNewChineseSimplified = voidData.ContentNewChineseSimplified,
                ContentEnglish = voidData.ContentEnglish,
                ContentSpanish = voidData.ContentSpanish,
                ContentItalian = voidData.ContentItalian,
                ContentJapanese = voidData.ContentJapanese,
                ContentKorean = voidData.ContentKorean,
                ContentThai = voidData.ContentThai,
                CreatorId = voidData.CreatorId,
                CreatorName = voidData.CreatorName
            };
        }

        public async Task<Message> GetMessage(int messageId)
        {
            var criteria = new GetMessageCriteria
            {
                MessageId = messageId
            };

            return await repository.GetAsync<Message>(criteria);
        }

        public async Task<int> InsertMessage(Message message, int matchId, int? leagueGroupId, int issueTypeId)
        {
            var messageContentType = Helper.GetMessageContentType(issueTypeId, message.ContentEnglish);
            var criteria = new CreateMessageCriteria
            {
                MatchId = matchId,
                LeagueGroupId = leagueGroupId,
                Creator = message.CreatorId,
                CreatorName = message.CreatorName,
                Content_en = message.ContentEnglish.InsertSpace(),
                Content_ch = message.ContentChineseTraditional.InsertSpace(),
                Content_cs = message.ContentChineseSimplified.InsertSpace(),
                Content_zhcn = message.ContentNewChineseSimplified.InsertSpace(),
                Content_it = message.ContentItalian.InsertSpace(),
                Content_jp = message.ContentJapanese.InsertSpace(),
                Content_th = message.ContentThai.InsertSpace(),
                Content_ko = message.ContentKorean.InsertSpace(),
                Content_es = message.ContentSpanish.InsertSpace(),
                MsgContentsType = messageContentType
            };

            return await repository.GetAsync<int>(criteria);
        }

        public async Task<VoidedReason> InitializeVoidReason(bool isAutoVoid, int subIssueTypeId, int sportType, VoidDataXmlModel voidData)
        {
            if (isAutoVoid)
            {
                var criteria = new GetReasonNameCriteria
                {
                    SportType = sportType,
                    SubIssueTypeId = subIssueTypeId
                };
                var reason = await repository.GetAsync<VoidedReason>(criteria);
                if (reason == null)
                {
                    return new VoidedReason();
                }
                var sportEn = "";
                var sportCS = "";
                if (sportType > 0)
                {
                    sportEn = $"{betListService.GetSportTypeName(sportType, LanguageCode.English)} - ";
                    sportCS = $"{betListService.GetSportTypeName(sportType, LanguageCode.ChineseSimplified)} - ";
                }
                reason.ReasonEnglish = $"{sportEn}{reason.ReasonEnglish}";
                reason.ReasonChineseSimplified = $"{sportCS}{reason.ReasonChineseSimplified}";

                return reason;
            }

            return new VoidedReason
            {
                ReasonEnglish = voidData.Reason,
                ReasonChineseSimplified = voidData.ReasonSC
            };
        }

        public async Task<int> InsertReason(string reasonEnglish, string reasonChineseSimplified)
        {
            var criteria = new InsertReasonCommand
            {
                ReasonEnglish = reasonEnglish,
                ReasonChineseSimplified = reasonChineseSimplified
            };

            await repository.ExecuteAsync(criteria);
            return criteria.ReasonId;
        }

        public async Task<VoidedReason> GetVoidedReason(int reasonId)
        {
            var criteria = new GetVoidedReasonCriteria
            {
                ReasonId = reasonId
            };

            return await repository.GetAsync<VoidedReason>(criteria);
        }

        public async Task InsertBatchMessage(int userId, string message)
        {
            var command = new SendMessageCommand
            {
                CreatorId = userId,
                XmlMessage = message
            };
            await repository.ExecuteAsync(command);
        }

        public async Task InsertMessageIntoAdminSite(Message message)
        {
            var command = new InsertMessageIntoAdminSiteCriteria
            {
                men = message.ContentEnglish,
                mch = message.ContentChineseTraditional,
                member = true,
                agent = true,
                ma = true,
                sma = true,
                creator = message.CreatorId
            };
            await repository.ExecuteAsync(command);
        }

        private IEnumerable<TicketEmailModel> ExcludeCN88Tickets(IEnumerable<TicketEmailModel> tickets, bool isAbnormalBet)
        {
            if (isAbnormalBet)
            {
                return tickets.Where(x => !x.Site.ToLowerInvariant().Equals("cn88"));
            }

            return tickets;
        }

        public IEnumerable<ReceivedUser> GetListSendMessageDb<T>(IEnumerable<T> tickets) where T : SendMessageDbModel
        {
            return tickets.Select(item => new ReceivedUser
            {
                CustId = item.CustId,
                CustUserName = item.UserName,
                Recommend = item.Recommend,
                AgUserName = item.AgUserName,
                Mrecommend = item.Mrecommend,
                MagUserName = item.MagUserName,
                Srecommend = item.Srecommend,
                SagUserName = item.SagUserName,
                TransId = item.TransId
            });
        }
    }
}