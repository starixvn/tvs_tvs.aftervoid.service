using System;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Resources;

namespace Fanex.TVS.AfterVoid.Core.Utilities
{
    public static class Helper
    {
        public static string GetBetFrom(string betFrom)
        {
            string result;
            switch (betFrom)
            {
                case "p":
                case "s":
                case "l":
                    result = Labels.Phone;
                    break;

                case "w":
                case "m":
                    result = Labels.Wap;
                    break;

                default:
                    result = Labels.Internet;
                    break;
            }

            return result;
        }

        public static int GetMessageContentType(int issueTypeId, string messageContent)
        {
            if (IsLikelyAbnormalBet(issueTypeId) || IsAbnormalAccount(issueTypeId))
            {
                return MessageContentType.VoidAbnormal;
            }

            if (messageContent.ToLower().Contains("kindly double check your statement again"))
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
            var FILTER_BY_MIX_PARLAY = 1;
            return IssueTypeConstant.AbnormalBet == issueTypeId && filterBy == FILTER_BY_MIX_PARLAY;
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

        public static string GetTransDescElementByName(string transDesc, string name)
        {
            if (!string.IsNullOrWhiteSpace(transDesc))
            {
                var descriptions = transDesc.Split(';');
                foreach (var description in descriptions)
                {
                    if (description.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var indexOfEqual = description.IndexOf('=') + 1;

                        return description.Substring(indexOfEqual, description.Length - indexOfEqual);
                    }
                }
            }

            return string.Empty;
        }
    }
}