using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Service.Extensions;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions;

public class XmlExtensionTests
{
    [Fact]
    public void ToVoidResultsXML_ReturnEmptyWhenArgEmpty()
    {
        var actual = new List<Result>().ToVoidResultsXML();

        Assert.Empty(actual);
    }

    [Fact]
    public void ToVoidResultsXML_ReturnEmptyWhenArgNull()
    {
        var actual = ((List<Result>)null).ToVoidResultsXML();

        Assert.Empty(actual);
    }

    [Fact]
    public void ToVoidResultsXML_ReturnResultWhenHasError()
    {
        var result = new Result
        {
            ErrorCode = 1,
            ErrorMsg = "ErrorMsg"
        };
        const string expected = "<VoidResults><VoidResult Message='ErrorMsg' Type='1' Licensees='' /></VoidResults>";
        var results = new List<Result> { result };

        var actual = results.ToVoidResultsXML();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToVoidResultsXML_ReturnResultWhenHasNoError()
    {
        var result = new Result();
        const string expected = "<VoidResults></VoidResults>";
        var results = new List<Result> { result };

        var actual = results.ToVoidResultsXML();

        Assert.Equal(expected, actual);
    }
}