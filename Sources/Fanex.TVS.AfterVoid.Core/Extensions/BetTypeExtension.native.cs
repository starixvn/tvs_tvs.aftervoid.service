using Fanex.TVS.AfterVoid.Core.Constants;

namespace Fanex.TVS.AfterVoid.Core.Extensions
{
    public static class BetTypeExtension
    {
        public static bool IsParlay(this int id)
        {
            return id == 29 || id == 2799;
        }

        public static bool IsParlayAndBetBuilder(this int id)
        {
            return id.IsParlay() || id.IsSingleMatchParlay();
        }

        public static bool IsNumberGame(this int id)
        {
            return BetTypeConstant.FirstNumberGame <= id && id <= BetTypeConstant.SecondNumberGame;
        }

        public static bool IsMetricGaming(this int id)
        {
            return BetTypeConstant.FirstMetricGame <= id && id <= BetTypeConstant.SecondMetricGame;
        }

        public static bool IsLucky3(this int id)
        {
            return BetTypeConstant.FirstLucky3 <= id && id <= BetTypeConstant.SecondLucky3;
        }

        public static bool IsSingleMatchParlay(this int id)
        {
            return id == 38;
        }

        public static bool IsVirtualSportBetType(this int id)
        {
            return BetTypeConstant.FirstVirtualSport <= id && id <= BetTypeConstant.SecondVirtualSport || BetTypeConstant.ThirdVirtualSport <= id && id <= BetTypeConstant.FourthVirtualSport;
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
}