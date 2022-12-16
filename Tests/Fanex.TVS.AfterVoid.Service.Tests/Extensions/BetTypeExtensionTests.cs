using Fanex.TVS.AfterVoid.Service.Extensions;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions
{
    public class BetTypeExtensionTests
    {
        [Theory]
        [InlineData(29, true)]
        [InlineData(1, false)]
        public void IsParlay_CheckId_ReturnRightBetTypeParlay(int id, bool expected)
        {
            var actual = id.IsParlayAndBetBuilder();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(29, true)]
        [InlineData(120, true)]
        [InlineData(1201, true)]
        [InlineData(2899, true)]
        [InlineData(1610, true)]
        public void HideScore_CheckId_IsNumberGameOrVS2(int id, bool expected)
        {
            var actual = id.HideScore();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(29, true)]
        [InlineData(81, true)]
        [InlineData(801, true)]
        [InlineData(1600, true)]
        public void HideHandicap_CheckId_IsParlayOrMetricGamingOrLucky3(int id, bool expected)
        {
            var actual = id.IsHideHandicap();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TranslateChineseSimplified_HasName_ReturnChineseSimplified()
        {
            const string name = "Next";
            var chinese = name.TranslateChineseSimplified();

            // Assert
            const string expected = "下一个";
            Assert.Equal(expected, chinese);
        }

        [Theory]
        [InlineData(82, true)]
        [InlineData(121, false)]
        [InlineData(0, false)]
        public void IsNumberGame_ReturnExpected(int id, bool expected)
        {
            var actual = id.IsNumberGame();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1601, true)]
        [InlineData(1900, false)]
        [InlineData(1, false)]
        public void IsLucky3_ReturnExpected(int id, bool expected)
        {
            var actual = id.IsLucky3();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(2702, true)]
        [InlineData(1202, true)]
        [InlineData(9999, false)]
        [InlineData(0, false)]
        public void IsVirtualSportBetType_ReturnExpected(int id, bool expected)
        {
            var actual = id.IsVirtualSportBetType();

            Assert.Equal(expected, actual);
        }
    }
}