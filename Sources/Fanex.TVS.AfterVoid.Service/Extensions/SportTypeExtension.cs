using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class SportTypeExtension
{
    public static bool IsHaveJackTimeCashOutTickets(this int sportType)
    {
        return sportType is SportTypeConstant.Soccer or SportTypeConstant.Basketball;
    }
}