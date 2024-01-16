using System;
using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Service.Extensions;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions;

public class MessageExtensionsTests
{
    [Fact]
    public void InsertSpace_ReturnExpected()
    {
        const string text = "111@@111";
        var actual = text.InsertSpace();

        Assert.Equal("111 @@ 111", actual);
    }

    [Fact]
    public void InsertSpace_ReturnStringEmpty()
    {
        const string text = "";
        var actual = text.InsertSpace();

        Assert.Equal(string.Empty, actual);
    }

    [Fact]
    public void InsertSpace_ReturnStringThatWasInsertedSpace()
    {
        const string expected = "\"a @@ b\"";
        const string text = "\"a  @@  b\"";

        var actual = text.InsertSpace();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MessageExtensionsNonStaticCreateInfoXml_ReturnExpected()
    {
        var tickets = new List<TicketEmailModel> { new()
        {
            TransId = 12312312, CustId = 1, MatchId = 1,
            WinLostDate = DateTimeOffset.MinValue.DateTime,
            Status = "Done",
            BetTypeId = 1,
            Stake = 1,
            WinLost = "100",
            VoidingHistoryId = 1,
            SiteDbGroupId = 0
        } };
        const string expected = "<root>\r\n  <r TransId=\"12312312\" CustId=\"1\" MatchId=\"1\" WinLostDate=\"0001-01-01T00:00:00\" Status=\"Done\" " +
                                "BetTypeId=\"1\" Stake=\"1\" WinLost=\"100\" VoidingHistoryId=\"1\" SiteDbGroupId=\"0\" />\r\n</root>";

        var actual = tickets.CreateTicketsXml();

        Assert.Equal(expected, actual);
    }
}