using Fanex.RemotingRef;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.TicketVoidingSystem.Services;

public class RemotingRefWrapperServiceTests
{
    private readonly IRemotingRefService remotingRefService;
    private readonly RemotingRefWrapperServiceImp service;

    public RemotingRefWrapperServiceTests()
    {
        remotingRefService = Substitute.For<IRemotingRefService>();
        service = new RemotingRefWrapperServiceImp(remotingRefService);
    }

    [Fact]
    public void GetBetType_RemotingRefServiceCalledGetBetTypeName()
    {
        // Arrange

        // Act
        _ = service.GetBetType("1", "1");

        // Assert
        remotingRefService.Received(1).GetBetTypeName("1", "1");
    }

    [Fact]
    public void GetDisplayedBetTypeName_RemotingRefServiceCalledGetDisplayedBetTypeName()
    {
        // Arrange

        // Act
        _ = service.GetDisplayedBetTypeName("1", "1");

        // Assert
        remotingRefService.Received(1).GetDisplayedBetTypeName("1", string.Empty, 1, LanguageCode.English, string.Empty);
    }

    [Fact]
    public void GetBetType_ParamHasNotTypeString_RemotingRefServiceCalledGetBetTypeName()
    {
        // Arrange

        // Act
        _ = service.GetBetType(1, 1);

        // Assert
        remotingRefService.Received(1).GetBetTypeName("1", LanguageCode.English, string.Empty, string.Empty, 1);
    }

    [Fact]
    public void GetParentIdByBetTypeId_RemotingRefServiceCalledGetParentIdByBetTypeId()
    {
        // Arrange

        // Act
        _ = service.GetParentIdByBetTypeId(1);

        // Assert
        remotingRefService.Received(1).GetParentIdByBetTypeId("1", "1");
    }

    [Fact]
    public void GetLeagueName_RemotingRefServiceCalledGetLeagueName()
    {
        // Arrange

        // Act
        _ = service.GetLeagueName(1, 1, "1");

        // Assert
        remotingRefService.Received(1).GetLeagueName("1", "1", string.Empty);
    }

    [Fact]
    public void GetBFName_RemotingRefServiceCalledGetLeagueName()
    {
        // Arrange

        // Act
        _ = service.GetBFName(1);

        // Assert
        remotingRefService.Received(1).GetTeamName3rd("1", LanguageCode.English);
    }

    [Fact]
    public void GetOddsTypeName_RemotingRefServiceCalledGetOddsTypeName()
    {
        // Arrange

        // Act
        _ = service.GetOddsTypeName(1);

        // Assert
        remotingRefService.Received(1).GetOddsTypeName("1", LanguageCode.English);
    }

    [Fact]
    public void GetTeamName_RemotingRefServiceCalledGetTeamName()
    {
        // Arrange

        // Act
        _ = service.GetTeamName(1, 1, "1");

        // Assert
        remotingRefService.Received(1).GetTeamName("1", "1", string.Empty);
    }

    [Fact]
    public void GetComboParlayName_RemotingRefServiceCalledGetCombinationTypeCombo()
    {
        // Arrange

        // Act
        _ = service.GetComboParlayName("1", "1", 1, 1);

        // Assert
        remotingRefService.Received(1).GetCombinationTypeCombo("1", "1", 1, 1, LanguageCode.English);
    }

    [Fact]
    public void GetSportName_RemotingRefServiceCalledGetSportName()
    {
        // Arrange

        // Act
        _ = service.GetSportName("1", "1");

        // Assert
        remotingRefService.Received(1).GetSportName("1", "1");
    }
}