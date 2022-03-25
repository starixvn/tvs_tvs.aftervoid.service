using System.Collections.Generic;
using System.Xml;
using Fanex.TVS.AfterVoid.Core.Constants;

namespace Fanex.TVS.AfterVoid.Core.Utilities
{
    public static class OddsText
    {
        private static readonly IList<int> BetTypeOfDecimalOdds;

        static OddsText()
        {
            BetTypeOfDecimalOdds = new List<int> { 1, 2, 3, 7, 12, 8, 20, 124, 125, 801, 802, 803, 804, 805, 806 };

            for (var i = 81; i <= 88; i++)
            {
                BetTypeOfDecimalOdds.Add(i);
            }

            for (var i = 91; i <= 120; i++)
            {
                BetTypeOfDecimalOdds.Add(i);
            }
        }

        public static decimal GetOdds(decimal odds, int betType, int oddsType)
        {
            if (BetTypeOfDecimalOdds.Contains(betType) && oddsType == OddsTypeConstant.Decimal)
            {
                return odds + 1;
            }

            return BetTypeOfDecimalOdds.Contains(betType) && oddsType == OddsTypeConstant.Us ? odds * 100 : odds;
        }

        private static int GetOddsType(int bettype, int oddsType)
        {
            return BetTypeOfDecimalOdds.Contains(bettype) ? oddsType : 0;
        }

        public static string GetOddsTypeName(int betType, int oddsType)
        {
            oddsType = GetOddsType(betType, oddsType);
            return RemotingWrapper.GetOddsTypeName(oddsType);
        }

        public static string GetOddsName(string oddsHtml)
        {
            var valueNode = string.Empty;
            var xmlDoc = new XmlDocument();
            oddsHtml = "<div>" + oddsHtml + "</div>";
            xmlDoc.LoadXml(oddsHtml);
            var foundSpanNodes = xmlDoc.SelectNodes("//span");

            if (foundSpanNodes != null && foundSpanNodes.Count == 2)
            {
                valueNode = foundSpanNodes[1].InnerText.Trim();
            }

            return valueNode;
        }
    }
}