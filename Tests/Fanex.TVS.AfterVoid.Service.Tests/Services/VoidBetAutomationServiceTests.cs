using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Core.Criteria;
using Fanex.TVS.AfterVoid.Core.Models;
using NSubstitute;
using Xunit;

namespace Fanex.TVS.AfterVoid.Core.Services.Tests
{
    public class VoidBetAutomationServiceTests
    {
        [Fact()]
        public async Task GetVoidBetAutomationStatistic_ReturnExpectedData()
        {
            var expected = new AutomationStatistic
            {
                RequestsWillUpdateStatus = new List<VoidBetAutomationInfo>() { new() { RequestId = 2 } },
                RequestsWillRunAfterVoid = new List<VoidBetAutomationInfo>() { new() { RequestId = 1 } }
            };
            var repository = Substitute.For<IDynamicRepository>();
            repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidBetAutomationInfo>(Arg.Any<GetAllVoidBetAutomationInfoCriteria>())
                .Returns((expected.RequestsWillUpdateStatus, expected.RequestsWillRunAfterVoid));

            var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

            var actual = await voidBetAutomationService.GetVoidBetAutomationStatistic();
            Assert.StrictEqual(expected.RequestsWillUpdateStatus, actual.RequestsWillUpdateStatus);
            Assert.StrictEqual(expected.RequestsWillRunAfterVoid, actual.RequestsWillRunAfterVoid);
        }

        //[Fact()]
        //public void GetRequestByIdTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void GetRequestSimpleInfoTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateServiceStatusByVoidingHistoryTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateServiceStatusBySessionTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateMessageIdByVoidingHistoryTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateMessageIdBySessionTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateAfterVoidStepTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateAllAfterVoidStepsTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateEmailResultByVoidingHistoryTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void GetVoidedTicketsForSendEmailTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void GetVoidDataFromVoidingHistoryTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void GetMatchInfoTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void UpdateTicketVoidingHistoryTest()
        //{
        //    Assert.True(false, "This test needs an implementation");
        //}
    }
}