using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extension.TN.String;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.Resources;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Extensions
{
    public static class EmailExtension
    {
        public static bool Valid<T>(this MailModel<T> model)
        {
            return model.Tickets.Count > 0 && !string.IsNullOrEmpty(model.Site.ToList);
        }

        public static string JoinSites<T>(this IEnumerable<T> tickets, Func<T, string> filter)
        {
            var sites = tickets.Select(filter).Distinct().ToArray();
            return string.Join(", ", sites);
        }

        public static string GetNegativeBalanceUsers<T, U>(
            this MailVoidModel<T, U> model, EmailGroup group, Func<U, string> filter)
        {
            return GetNegativeBalanceUsers(model, group, filter, model.ViewModel);
        }

        public static string GetNegativeBalanceUsers<T, U>(
            this MailRecoverModel<T, U> model, EmailGroup group, Func<U, string> filter)
        {
            return GetNegativeBalanceUsers(model, group, filter, model.ViewModel);
        }

        private static string GetNegativeBalanceUsers<U>(MailModel<U> model, EmailGroup group, Func<U, string> filter, MailViewModel viewModel)
        {
            if (group == EmailGroup.CashOut)
            {
                return string.Empty;
            }

            var usersInSite = model.Tickets.Select(filter).ToList();
            var negativeBalanceUsers = viewModel.NegativeBalanceUsers;
            return negativeBalanceUsers.Intersected(usersInSite);
        }

        public static string GetBetFrom(this string betFrom)
        {
            switch (betFrom)
            {
                case "p":
                case "s":
                case "l":
                    return Labels.Phone;

                case "w":
                case "m":
                    return Labels.Wap;

                default:
                    return Labels.Internet;
            }
        }

        public static string ToOperatorsXML(this IEnumerable<int> agentIds)
        {
            if (agentIds == null || !agentIds.Any())
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder();
            foreach (var agentId in agentIds)
            {
                var line = $"<Operator AgentId='{agentId}' />";
                stringBuilder.AppendLine(line);
            }

            return $"<Operators>{stringBuilder.ToString().Trim()}</Operators>";
        }

        public static string ToVoidResultsXML(this IEnumerable<Result> results)
        {
            if (results == null || !results.Any())
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder();
            foreach (var result in results)
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorMsg))
                {
                    var line = $"<VoidResult Message='{result.ErrorMsg}' Type='{result.ErrorCode}' />";
                    stringBuilder.AppendLine(line);
                }
            }

            return $"<VoidResults>{stringBuilder.ToString().Trim()}</VoidResults>";
        }
    }
}