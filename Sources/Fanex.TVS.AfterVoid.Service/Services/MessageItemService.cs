using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface MessageItemService
{
    string CreateMessageItems(IEnumerable<ReceivedUser> receivedUsers, int messageId);
}

public class MessageItemServiceImp : MessageItemService
{
    private const int DummyAccountMaxId = 100;

    public string CreateMessageItems(IEnumerable<ReceivedUser> receivedUsers, int messageId)
    {
        var messages = new StringBuilder();
        var creditTranses = receivedUsers;
        var customer = CustomerItem(creditTranses, messageId);
        if (customer.Length > 0)
        {
            messages.Append(customer);
        }

        var agent = AgentItem(creditTranses, messageId);
        if (agent.Length > 0)
        {
            messages.Append(agent);
        }

        var master = MasterItem(creditTranses, messageId);
        if (master.Length > 0)
        {
            messages.Append(master);
        }

        var super = SuperItem(creditTranses, messageId);
        if (super.Length > 0)
        {
            messages.Append(super);
        }

        return $"<Messages>{messages}</Messages>";
    }

    private StringBuilder CustomerItem(IEnumerable<ReceivedUser> creditTranses, int messageId)
    {
        var messageItems = new StringBuilder();
        var users = creditTranses.Where(c => c.CustId > DummyAccountMaxId)
            .Select(c => new { c.CustId, UserName = c.CustUserName }).Distinct();
        foreach (var user in users)
        {
            var customer = new UserInfo { CustId = user.CustId, UserName = user.UserName };
            var transIds = creditTranses.Where(c => c.CustId == user.CustId).Select(c => c.TransId).OrderBy(t => t).ToArray();
            var messageItem = SetValueMessageItem(customer, transIds, messageId);

            messageItems.Append(messageItem.CreateItem());
        }

        return messageItems;
    }

    private StringBuilder AgentItem(IEnumerable<ReceivedUser> creditTranses, int messageId)
    {
        var messageItems = new StringBuilder();
        var users = creditTranses.Where(c => c.Recommend > DummyAccountMaxId)
            .Select(c => new { CustId = c.Recommend, UserName = c.AgUserName }).Distinct();
        foreach (var item in users)
        {
            var agent = new UserInfo { CustId = item.CustId, UserName = item.UserName };
            var transIds = creditTranses.Where(c => c.Recommend == item.CustId).Select(c => c.TransId).OrderBy(t => t).ToArray();
            var messageItem = SetValueMessageItem(agent, transIds, messageId);

            messageItems.Append(messageItem.CreateItem());
        }

        return messageItems;
    }

    private StringBuilder MasterItem(IEnumerable<ReceivedUser> creditTranses, int messageId)
    {
        var messageItems = new StringBuilder();
        var users = creditTranses.Where(c => c.Mrecommend > DummyAccountMaxId)
            .Select(c => new { CustId = c.Mrecommend, UserName = c.MagUserName }).Distinct();
        foreach (var item in users)
        {
            var master = new UserInfo { CustId = item.CustId, UserName = item.UserName };
            var transIds = creditTranses.Where(c => c.Mrecommend == item.CustId).Select(c => c.TransId).OrderBy(t => t).ToArray();
            var messageItem = SetValueMessageItem(master, transIds, messageId);

            messageItems.Append(messageItem.CreateItem());
        }

        return messageItems;
    }

    private StringBuilder SuperItem(IEnumerable<ReceivedUser> creditTranses, int messageId)
    {
        var messageItems = new StringBuilder();
        var users = creditTranses.Where(c => c.Srecommend > DummyAccountMaxId)
            .Select(c => new { CustId = c.Srecommend, UserName = c.SagUserName }).Distinct();
        foreach (var user in users)
        {
            var super = new UserInfo { CustId = user.CustId, UserName = user.UserName };
            var transIds = creditTranses.Where(c => c.Srecommend == user.CustId).Select(c => c.TransId).OrderBy(t => t).ToArray();
            var messageItem = SetValueMessageItem(super, transIds, messageId);

            messageItems.Append(messageItem.CreateItem());
        }

        return messageItems;
    }

    private MessageItem SetValueMessageItem(UserInfo user, IEnumerable<long> transIdList, int messageId)
    {
        return new MessageItem
        {
            BetTransId = string.Join(", ", transIdList),
            TargetName = user.UserName,
            TargetId = user.CustId,
            MessageId = messageId
        };
    }
}