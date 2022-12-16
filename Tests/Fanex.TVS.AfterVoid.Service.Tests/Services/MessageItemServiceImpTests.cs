using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using FizzWare.NBuilder;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services
{
    public class MessageItemServiceImpTests
    {
        [Fact]
        public void CreateMessageItems_()
        {
            var fakeUsers = Builder<ReceivedUser>.CreateListOfSize(200).Build();
            var service = new MessageItemServiceImp();

            var actual = service.CreateMessageItems(fakeUsers, 991);

            Assert.Contains("</Messages>", actual);
        }
    }
}