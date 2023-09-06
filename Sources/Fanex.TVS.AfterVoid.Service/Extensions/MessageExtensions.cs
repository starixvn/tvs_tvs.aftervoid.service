using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public interface IMessageExtensionsNonStatic
{
    string CreateTicketsXml(IEnumerable<TicketEmailModel> emailTickets);
}

public class MessageExtensionsNonStatic : IMessageExtensionsNonStatic
{
    public string CreateTicketsXml(IEnumerable<TicketEmailModel> emailTickets)
    {
        var custInfo = new XElement("root");
        foreach (var ticket in emailTickets)
        {
            var element = new XElement("r",
                new XAttribute("TransId", ticket.TransId),
                new XAttribute("CustId", ticket.CustId),
                new XAttribute("MatchId", ticket.MatchId));
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

        return  regex.Replace(text, Replacement).Trim();
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

    public static string CreateTicketsXml(this IEnumerable<TicketEmailModel> emailTickets)
    {
        return Implementation.CreateTicketsXml(emailTickets);
    }
}