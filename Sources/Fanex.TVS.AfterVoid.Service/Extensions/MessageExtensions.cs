using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Extensions
{
    public interface IMessageExtensionsNonStatic
    {
        string CreateCustInfoXml(IEnumerable<TicketEmailModel> emailTickets);
    }

    internal class MessageExtensionsNonStatic : IMessageExtensionsNonStatic
    {
        public string CreateCustInfoXml(IEnumerable<TicketEmailModel> emailTickets)
        {
            var custInfo = new XElement("CustInfo");
            foreach (var ticket in emailTickets)
            {
                var element = new XElement("Item",
                                        new XAttribute("TransId", ticket.TransId),
                                        new XAttribute("CustId", ticket.CustId));
                custInfo.Add(element);
            }

            return custInfo.ToString();
        }
    }

    public static class MessageExtensions
    {
        private static IMessageExtensionsNonStatic Implementation = new MessageExtensionsNonStatic();

        public static void MockImplementationForUnitTest(IMessageExtensionsNonStatic implementation)
        {
            Implementation = implementation;
        }

        public static string InsertSpace(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            const string Pattern = "\\s*@@\\s*";
            const string Replacement = " @@ ";
            var regex = new Regex(Pattern);
            var result = regex.Replace(text, Replacement);
            return string.IsNullOrEmpty(result) ? string.Empty : result.Trim();
        }

        public static XElement CreateItem(this MessageItem messageItem)
        {
            var element = new XElement(
                    "Item",
                    new XAttribute("MessageId", messageItem.MessageId),
                    new XAttribute("TargetId", messageItem.TargetId),
                    new XAttribute("TargetName", messageItem.TargetName),
                    new XAttribute("BetTransId", messageItem.BetTransId));

            return element;
        }

        public static string CreateCustInfoXml(this IEnumerable<TicketEmailModel> emailTickets)
        {
            return Implementation.CreateCustInfoXml(emailTickets);
        }
    }
}