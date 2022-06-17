using Fanex.TVS.AfterVoid.Core.Extensions;
using Fanex.TVS.AfterVoid.Core.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions
{
    public class StringExtensionTests
    {
        [Fact]
        public void ParseXmlToObject_ReturnNewObject()
        {
            const string text = "<root><r OperatorId=\"6657460\" TypeId=\"1\"/><r OperatorId=\"37058259\" TypeId=\"2\"/></root>";
            var actual = text.ParseXmlToObject<RootOperators>();

            Assert.NotNull(actual);
        }
    }
}