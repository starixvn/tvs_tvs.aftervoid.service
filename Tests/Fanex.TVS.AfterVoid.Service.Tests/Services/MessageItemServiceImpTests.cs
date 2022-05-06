using Fanex.TVS.AfterVoid.Core.Models;
using FizzWare.NBuilder;
using Xunit;

namespace Fanex.TVS.AfterVoid.Core.Services.Tests
{
    public class MessageItemServiceImpTests
    {
        [Fact()]
        public void CreateMessageItems_()
        {
            var fakeUsers = Builder<ReceivedUser>.CreateListOfSize(200).Build();
            var service = new MessageItemServiceImp();

            var actual = service.CreateMessageItems(fakeUsers, 991);

            Assert.Contains("</Messages>", actual);
        }
    }
}