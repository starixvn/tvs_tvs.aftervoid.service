using System;
using System.Collections.Generic;
using System.Linq;
using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class StatusExtension
{
    private static readonly IEnumerable<string> SettledStatus = new List<string> { TicketStatus.Draw, TicketStatus.Won, TicketStatus.Lose };
    private static readonly IEnumerable<string> RunningOrWaiting = new List<string> { TicketStatus.Running, TicketStatus.Waiting };

    public static bool IsRunningOrWaiting(this string status)
    {
        return !string.IsNullOrEmpty(status) && RunningOrWaiting.Contains(status.ToLowerInvariant());
    }

    public static bool IsVoided(this string status)
    {
        return status.Equals(TicketStatus.Void, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSettled(this string status)
    {
        return SettledStatus.Contains(status.ToLowerInvariant());
    }

    public static bool IsRunningOrEmpty(this string status)
    {
        return string.IsNullOrEmpty(status) || RunningOrWaiting.Contains(status.ToLowerInvariant());
    }
}