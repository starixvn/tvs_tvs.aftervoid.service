using System;

namespace Fanex.TVS.AfterVoid.Service.Services.Notification;

public static class MessageHelper
{
    public static string CreateVoidSuccessfully(int requestId)
    {
        return $"<strong>Request {requestId}</strong> voided successfully";
    }

    public static string CreateRecoverSuccessfully(int requestId, bool isHasResettledTicket)
    {
        var messageSuccessful = $"<strong>Request {requestId}</strong> recovered successfully";
        if (isHasResettledTicket)
        {
            messageSuccessful += $"{Environment.NewLine}Please be aware that some tickets have been resettled automatically";
        }

        return messageSuccessful;
    }

    public static string CreateFailed(int requestId)
    {
        return $"<strong>Request {requestId}</strong> has an unknown error, please contact IT for support";
    }
}