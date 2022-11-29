using Fanex.TVS.AfterVoid.Service.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Utilities
{
    public static class Helper
    {
        public static int GetMessageContentType(int issueTypeId, string messageContent)
        {
            if (IsLikelyAbnormalBet(issueTypeId) || IsAbnormalAccount(issueTypeId))
            {
                return MessageContentType.VoidAbnormal;
            }

            if (messageContent.ToLowerInvariant().Contains("kindly double check your statement again"))
            {
                return MessageContentType.VoidSettled;
            }

            return MessageContentType.VoidUnsettled;
        }

        public static bool IsLikelyAbnormalBet(int issueTypeId)
        {
            return IsAbnormalBet(issueTypeId) || IsAbnormalBetRealMember(issueTypeId);
        }

        public static bool IsAbnormalBet(int issueTypeId)
        {
            return IssueTypeConstant.AbnormalBet == issueTypeId;
        }

        public static bool IsAbnormalBetMixParlay(int issueTypeId, int? filterBy)
        {
            const int filterByMixParlay = 1;
            return IssueTypeConstant.AbnormalBet == issueTypeId && filterBy == filterByMixParlay;
        }

        public static bool IsAbnormalAccount(int issueTypeId)
        {
            return IssueTypeConstant.AbnormalAccount == issueTypeId;
        }

        public static bool IsAbnormalBetRealMember(int issueTypeId)
        {
            return IssueTypeConstant.AbnormalBetRealMember == issueTypeId;
        }

        public static bool IsArchivedTicket(int issueTypeId)
        {
            return IssueTypeConstant.ArchivedTicket == issueTypeId;
        }
        }

        public static bool IsNotHaveMessage(Message message)
        {
            return message == null || message.MessageId == 0;
        }

        public static bool IsNotHaveVoidedReason(VoidedReason voidedReason)
        {
            return voidedReason == null || voidedReason.ReasonId == 0;
    }
}