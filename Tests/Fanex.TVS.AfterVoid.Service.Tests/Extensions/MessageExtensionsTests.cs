using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Service.Extensions;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions;

public class MessageExtensionsTests
{
    [Fact]
    public void InsertSpace_ReturnStringEmpty()
    {
        const string text = "111@@111";
        var actual = text.InsertSpace();

        Assert.Equal("111 @@ 111", actual);
    }

    [Fact]
    public void InsertSpace_ReturnStringThatWasInsertedSpace()
    {
        const string expected = "\"a @@ b\"";
        const string text = "\"a  @@  b\"";

        var actual = text.InsertSpace();

        Assert.Equal(expected, actual);
    }

    //[Fact]
    //public void CreateInfoXml_ImplementationHasCalled()
    //{
    //    const string expected = "";
    //    var messageExtensionsNonStatic = Substitute.For<IMessageExtensionsNonStatic>();
    //    messageExtensionsNonStatic.CreateTicketsXml(Arg.Any<TicketEmailModel>()).ReturnsForAnyArgs(expected);
    //    MessageExtensions.MockImplementationForUnitTest(messageExtensionsNonStatic);

    //    var actual = new List<TicketEmailModel>().CreateTicketsXml();

    //    messageExtensionsNonStatic.Received(1).CreateTicketsXml(Arg.Any<IEnumerable<TicketEmailModel>>());
    //    Assert.Equal(expected, actual);
    //}

    [Fact]
    public void MessageExtensionsNonStaticCreateInfoXml_ReturnExpected()
    {
        var tickets = new List<TicketEmailModel> { new() { TransId = 12312312, CustId = 1, MatchId = 1 } };
        var staticExtension = new MessageExtensionsNonStatic();

        var actual = staticExtension.CreateTicketsXml(tickets);

        Assert.NotNull(actual);
    }
}