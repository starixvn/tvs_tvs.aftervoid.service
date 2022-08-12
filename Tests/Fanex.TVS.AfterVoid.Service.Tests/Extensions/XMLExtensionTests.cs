using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions
{
    public class XMLExtensionTests
    {
        [Fact]
        public void ToVoidResultsXML_ReturnEmptyWhenArgEmpty()
        {
            var results = new List<Result>();

            var actual = results.ToVoidResultsXML();

            Assert.Empty(actual);
        }

        [Fact]
        public void ToVoidResultsXML_ReturnEmptyWhenArgNull()
        {
            List<Result> results = null;

            var actual = results.ToVoidResultsXML();

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
            var expected = "<VoidResults><VoidResult Message='ErrorMsg' Type='1' /></VoidResults>";
            var results = new List<Result> { result };

            var actual = results.ToVoidResultsXML();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToVoidResultsXML_ReturnResultWhenHasNoError()
        {
            var result = new Result();
            var expected = "<VoidResults></VoidResults>";
            var results = new List<Result> { result };

            var actual = results.ToVoidResultsXML();

            Assert.Equal(expected, actual);
        }
    }
}