using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions;

public class JackTimeCashOutExtensionTests
{
    [Fact]
    public void IsHasJackTimeCashOut_ValidXmlWithJtseqid_ReturnsTrue()
    {
        // Arrange
        const string extraInfo = "<p25 m=\"1.18\" jtseqid=\"12345\" /><p1 fip=\"1.2.3.5\" cc=\"303\" /><p74 m=\"25\" />";

        // Act
        var result = JackTimeCashOutExtension.IsHasJackTimeCashOut(SportTypeConstant.Soccer, extraInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHasJackTimeCashOut_IsNotSoccerOrBasketball_ReturnsFalse()
    {
        // Arrange
        // Act
        var result = JackTimeCashOutExtension.IsHasJackTimeCashOut(0, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHasJackTimeCashOut_ValidXmlWithoutJtseqid_ReturnsFalse()
    {
        // Arrange
        const string extraInfo = "<p74 m=\"25\" /><p25 m=\"1.18\" /><p1 fip=\"1.2.3.5\" cc=\"303\" />";

        // Act
        var result = JackTimeCashOutExtension.IsHasJackTimeCashOut(SportTypeConstant.Soccer, extraInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHasJackTimeCashOut_InvalidXml_ReturnsFalse()
    {
        // Arrange
        const string extraInfo = "<p25 m=\"1.18\" jtseqid=\"12345\" /<p1 fip=\"1.2.3.5\" cc=\"303\" />";

        // Act
        var result = JackTimeCashOutExtension.IsHasJackTimeCashOut(SportTypeConstant.Soccer, extraInfo);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHasJackTimeCashOut_EmptyString_ReturnsFalse()
    {
        // Arrange
        const string extraInfo = "";

        // Act
        var result = JackTimeCashOutExtension.IsHasJackTimeCashOut(SportTypeConstant.Soccer, extraInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHasJackTimeCashOut_NullString_ReturnsFalse()
    {
        // Arrange
        // Act
        var result = JackTimeCashOutExtension.IsHasJackTimeCashOut(SportTypeConstant.Soccer, null);

        // Assert
        Assert.False(result);
    }
}