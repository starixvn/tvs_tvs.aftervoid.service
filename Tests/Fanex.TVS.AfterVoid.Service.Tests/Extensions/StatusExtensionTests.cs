using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions;

public class StatusExtensionTests
{
    [Fact]
    public void IsRunningOrWaiting_ReturnTrue()
    {
        const string status = TicketStatus.Running;
        var actual = status.IsRunningOrWaiting();

        Assert.True(actual);
    }

    [Fact]
    public void IsRunningOrWaiting_StatusIsNull_ReturnFalse()
    {
        const string status = null;
        var actual = status.IsRunningOrWaiting();

        Assert.False(actual);
    }

    [Fact]
    public void IsVoided_ReturnTrue()
    {
        const string status = TicketStatus.Void;
        var actual = status.IsVoided();

        Assert.True(actual);
    }

    [Theory]
    [InlineData(TicketStatus.Running)]
    [InlineData("")]
    public void IsRunningOrEmpty_ReturnTrue(string status)
    {
        var actual = status.IsRunningOrEmpty();

        Assert.True(actual);
    }
}