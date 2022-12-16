using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Extensions
{
    public class StringExtensionTests
    {
        [Fact]
        public void ParseXmlToObject_ReturnObject()
        {
            const string text = "<root><r OperatorId=\"6657460\" TypeId=\"1\"/><r OperatorId=\"37058259\" TypeId=\"2\"/></root>";
            var actual = text.ParseXmlToObject<RootOperators>();

            Assert.NotNull(actual);
        }

        [Fact]
        public void ParseXmlToObject_ReturnNewObject()
        {
            var model = new RootOperators();
            const string text = "";
            var actual = text.ParseXmlToObject<RootOperators>();

            AssertExtension.Equal(model, actual);
        }

        [Fact]
        public void XmlToJson_ReturnJson()
        {
            const string text = "<root><r OperatorId=\"6657460\" TypeId=\"1\"/><r OperatorId=\"37058259\" TypeId=\"2\"/></root>";
            var actual = text.XmlToJson<RootOperators>();

            Assert.NotNull(actual);
        }
    }
}