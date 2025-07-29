using System;
using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class JackTimeCashOutExtension
{
    public static bool IsHasJackTimeCashOut(int sportType, string extraInfo)
    {
        return sportType is SportTypeConstant.Basketball
               || sportType is SportTypeConstant.Soccer && IsHasJackTimeText(extraInfo);
    }

    private static bool IsHasJackTimeText(string extraInfo)
    {
        return !string.IsNullOrWhiteSpace(extraInfo) && extraInfo.Contains("jtseqid", StringComparison.OrdinalIgnoreCase);
    }
}