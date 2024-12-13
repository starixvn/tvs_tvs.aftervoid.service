using Fanex.TVS.AfterVoid.Service.Constants;
using TicketVoidingSystem.Constants;

namespace Fanex.TVS.AfterVoid.Service.Utilities;

public static class Helper
{
    public static int GetMessageContentType(int issueTypeId, string messageContent)
    {
        if (IsAbnormalBet(issueTypeId) || IsAbnormalAccount(issueTypeId))
        {
            return MessageContentType.VoidAbnormal;
        }

        return messageContent.ToLowerInvariant().Contains("kindly double check your statement again") ? MessageContentType.VoidSettled : MessageContentType.VoidUnsettled;
    }

    public static bool IsAbnormalBet(int issueTypeId)
    {
        return IssueTypeConstant.AbnormalBet == issueTypeId;
    }

    public static bool IsAbnormalBetParlay(int issueTypeId, int? filterBy)
    {
        return IssueTypeConstant.AbnormalBet == issueTypeId && filterBy is FilterByConstant.MixParlay or FilterByConstant.SystemParlay;
    }

    public static bool IsAbnormalAccount(int issueTypeId)
    {
        return IssueTypeConstant.AbnormalAccount == issueTypeId;
    }

    public static bool IsArchivedTicket(int issueTypeId)
    {
        return IssueTypeConstant.ArchivedTicket == issueTypeId;
    }

    public static bool IsCanSendLetstalk(int issueTypeId)
    {
        return issueTypeId != IssueTypeConstant.ArchivedTicket;
    }
}