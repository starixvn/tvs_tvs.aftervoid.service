using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Service.Extensions;
using NSubstitute;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions;

public class MessageExtensionsTests
{
    [Fact]
    public void InsertSpace_ReturnStringEmpty()
    {
        const string text = "";
        var actual = text.InsertSpace();

        Assert.NotNull(actual);
    }

    [Fact]
    public void InsertSpace_ReturnStringThatWasInsertedSpace()
    {
        const string expected = "\" @@ \"";
        const string text = "\"@@\"";

        var actual = text.InsertSpace();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CreateCustInfoXml_ImplementationHasCalled()
    {
        const string expected = "";
        var tickets = new List<TicketEmailModel>();
        var messageExtensionsNonStatic = Substitute.For<IMessageExtensionsNonStatic>();
        messageExtensionsNonStatic.CreateTicketsXml(default).ReturnsForAnyArgs(expected);
        MessageExtensions.MockImplementationForUnitTest(messageExtensionsNonStatic);

        var actual = tickets.CreateTicketsXml();

        messageExtensionsNonStatic.Received(1).CreateTicketsXml(Arg.Any<IEnumerable<TicketEmailModel>>());
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MessageExtensionsNonStaticCreateCustInfoXml_ReturnExpected()
    {
        var tickets = new List<TicketEmailModel> { new() };
        var staticExtension = new MessageExtensionsNonStatic();

        var actual = staticExtension.CreateTicketsXml(tickets);

        Assert.NotNull(actual);
    }
}