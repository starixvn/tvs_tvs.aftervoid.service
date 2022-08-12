using System;
using System.IO;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Resources;
using Newtonsoft.Json;

namespace Fanex.TVS.AfterVoid.Service.Utilities
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

        public static T DeserializeObjectFromFile<T>(string filePath)
        {
            var path = GetFilePath(filePath);
            var jsonString = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private static string GetFilePath(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}