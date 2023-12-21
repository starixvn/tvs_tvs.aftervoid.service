using TicketVoidingSystem.Extensions;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.TicketVoidingSystem.Extensions;

public class LeagueExtensionTests
{
    [Fact]
    public void IsLeagueGroupIdSaBa_LeagueGroupIdIsInSaBaList_ReturnTrue()
    {
        // Arrange
        const int leagueGroupId = 42;

        // Act
        var result = leagueGroupId.IsLeagueGroupIdSaBa();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLeagueGroupIdSaBa_LeagueGroupIdIsNotInSaBaList_ReturnFalse()
    {
        // Arrange
        const int leagueGroupId = 88;

        // Act
        var result = leagueGroupId.IsLeagueGroupIdSaBa();

        // Assert
        Assert.False(result);
    }
}