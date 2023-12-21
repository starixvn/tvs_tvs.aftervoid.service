using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class BetTypeExtension
{
    public static bool IsParlay(this int id)
    {
        return id is BetTypeConstant.FirstParlay or BetTypeConstant.SecondParlay;
    }

    public static bool IsParlayAndBetBuilder(this int id)
    {
        return id.IsParlay() || id.IsSingleMatchParlay();
    }

    public static bool IsNumberGame(this int id)
    {
        return id is >= BetTypeConstant.FirstNumberGame and <= BetTypeConstant.SecondNumberGame;
    }

    public static bool IsMetricGaming(this int id)
    {
        return id is >= BetTypeConstant.FirstMetricGame and <= BetTypeConstant.SecondMetricGame;
    }

    public static bool IsLucky3(this int id)
    {
        return id is >= BetTypeConstant.FirstLucky3 and <= BetTypeConstant.SecondLucky3;
    }

    public static bool IsSingleMatchParlay(this int id)
    {
        const int betTypeSingleMatchParlay = 38;
        return id == betTypeSingleMatchParlay;
    }

    public static bool IsVirtualSportBetType(this int id)
    {
        return id is >= BetTypeConstant.FirstVirtualSport and <= BetTypeConstant.SecondVirtualSport or >= BetTypeConstant.ThirdVirtualSport and <= BetTypeConstant.FourthVirtualSport;
    }

    public static bool IsHideHandicap(this int id)
    {
        return id.IsParlayAndBetBuilder() || id.IsMetricGaming() || id.IsNumberGame() || id.IsLucky3();
    }

    public static bool HideScore(this int id)
    {
        return id.IsParlayAndBetBuilder() || id.IsNumberGame() || id.IsVirtualSportBetType() || id.IsLucky3();
    }

    public static string TranslateChineseSimplified(this string name)
    {
        return name.Replace("Next", "下一个");
    }
}