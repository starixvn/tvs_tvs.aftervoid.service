using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.TicketVoidingSystem.Services;

public class MatchServiceTests
{
    private readonly RemotingRefWrapperService remotingRefWrapperService;
    private MatchService service;

    public MatchServiceTests()
    {
        remotingRefWrapperService = Substitute.For<RemotingRefWrapperService>();
    }

    [Fact]
    public void UpdateMatchInfoFromRemotingRef_HasMatchIsNull_ReturnNull()
    {
        // Arrange
        service = new MatchServiceImp(remotingRefWrapperService);

        // Act
        var actual = service.UpdateMatchInfoFromRemotingRef(null);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void UpdateMatchInfoFromRemotingRef_HasMatchNotNull_ReturnMatchUpdated()
    {
        // Arrange
        var query = new Match();
        remotingRefWrapperService.GetTeamName(default, default).ReturnsForAnyArgs("TeamName");
        remotingRefWrapperService.GetLeagueName(default, default).ReturnsForAnyArgs("LeagueName");
        remotingRefWrapperService.GetSportName(default, default).ReturnsForAnyArgs("SportName");
        service = new MatchServiceImp(remotingRefWrapperService);

        var expected = new Match
        {
            HomeName = "TeamName",
            HomeNameSimplified = "TeamName",
            HomeNameNewSimplified = "TeamName",
            HomeNameTraditional = "TeamName",
            AwayName = "TeamName",
            AwayNameSimplified = "TeamName",
            AwayNameNewSimplified = "TeamName",
            AwayNameTraditional = "TeamName",
            LeagueName = "LeagueName",
            LeagueNameSimplified = "LeagueName",
            LeagueNameNewSimplified = "LeagueName",
            LeagueNameTraditional = "LeagueName",
            SportTypeName = "SportName",
            SportTypeNameSimplified = "SportName",
            SportTypeNameNewSimplified = "SportName",
            SportTypeNameTraditional = "SportName",
            MatchEvent = "TeamName - vs - TeamName",
            MatchEventSimplified = "TeamName - vs - TeamName",
            MatchEventNewSimplified = "TeamName - vs - TeamName",
        };
        // Act
        var actual = service.UpdateMatchInfoFromRemotingRef(query);

        // Assert
        AssertExtension.Equal(expected.HomeName, actual.HomeName);
        AssertExtension.Equal(expected.AwayName, actual.AwayName);
        AssertExtension.Equal(expected.MatchEvent, actual.MatchEvent);
    }

    [Fact]
    public void UpdateMatchInfoFromRemotingRef_MatchHasIsOutrightAndIsOutrightIsTrue_ReturnMatchUpdated()
    {
        // Arrange
        var query = new Match { IsOutright = true, IsNeutral = true };
        remotingRefWrapperService.GetTeamName(default, default).ReturnsForAnyArgs("TeamName");
        remotingRefWrapperService.GetLeagueName(default, default).ReturnsForAnyArgs("LeagueName");
        remotingRefWrapperService.GetSportName(default, default).ReturnsForAnyArgs("SportName");
        service = new MatchServiceImp(remotingRefWrapperService);

        var expected = new Match
        {
            IsOutright = true,
            IsNeutral = true,
            HomeName = "TeamName" + " (N)",
            HomeNameSimplified = "TeamName" + " (中)",
            HomeNameNewSimplified = "TeamName" + " (中)",
            HomeNameTraditional = "TeamName" + " (中)",
            AwayName = "TeamName",
            AwayNameSimplified = "TeamName",
            AwayNameNewSimplified = "TeamName",
            AwayNameTraditional = "TeamName",
            LeagueName = "LeagueName",
            LeagueNameSimplified = "LeagueName",
            LeagueNameNewSimplified = "LeagueName",
            LeagueNameTraditional = "LeagueName",
            SportTypeName = "SportName",
            SportTypeNameSimplified = "SportName",
            SportTypeNameNewSimplified = "SportName",
            SportTypeNameTraditional = "SportName",
            MatchEvent = "TeamName (N) - LeagueName",
            MatchEventSimplified = "TeamName (中) - LeagueName",
            MatchEventNewSimplified = "TeamName (中) - LeagueName",
        };
        // Act
        var actual = service.UpdateMatchInfoFromRemotingRef(query);

        // Assert
        AssertExtension.Equal(expected.HomeName, actual.HomeName);
        AssertExtension.Equal(expected.AwayName, actual.AwayName);
        AssertExtension.Equal(expected.MatchEvent, actual.MatchEvent);
    }

    [Theory]
    [InlineData(160, "Lucky 3 Game No.", "", "")]
    [InlineData(161, "Number Game No. ", "", "")]
    [InlineData(164, "Happy 5 No .", "TeamName - vs - TeamName", "TeamName - vs - TeamName")]
    [InlineData(207, "BFName - vs - BFName", "", "")]
    public void UpdateMatchInfoFromRemotingRef_MatchHasSportType_MatchUpdatedMatchEvent(int sportType, string matchEvent
        , string matchEventSimplified, string matchEventNewSimplified)
    {
        // Arrange
        var query = new Match { SportType = sportType };
        remotingRefWrapperService.GetTeamName(default, default).ReturnsForAnyArgs("TeamName");
        remotingRefWrapperService.GetLeagueName(default, default).ReturnsForAnyArgs("LeagueName");
        remotingRefWrapperService.GetSportName(default, default).ReturnsForAnyArgs("SportName");
        remotingRefWrapperService.GetBFName(default).ReturnsForAnyArgs("BFName");
        service = new MatchServiceImp(remotingRefWrapperService);

        // Act
        var actual = service.UpdateMatchInfoFromRemotingRef(query);

        // Assert
        AssertExtension.Equal(matchEvent, actual.MatchEvent);
        AssertExtension.Equal(matchEventSimplified, actual.MatchEventSimplified);
        AssertExtension.Equal(matchEventNewSimplified, actual.MatchEventNewSimplified);
    }
}