using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Utilities;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Utilities;

public class HelperTests
{
    private const string MessageContentVoid = "kindly double check your statement again";

    [Theory]
    [InlineData(IssueTypeConstant.AbnormalBet, "", MessageContentType.VoidAbnormal)]
    [InlineData(IssueTypeConstant.AbnormalBetRealMember, "", MessageContentType.VoidAbnormal)]
    [InlineData(IssueTypeConstant.AbnormalAccount, "", MessageContentType.VoidAbnormal)]
    [InlineData(IssueTypeConstant.DangerAccepted, MessageContentVoid, MessageContentType.VoidSettled)]
    [InlineData(IssueTypeConstant.DangerAccepted, "", MessageContentType.VoidUnsettled)]
    public void GetMessageContentType_ReturnResult(int issueTypeId, string messageContent, int expected)
    {
        var actual = Helper.GetMessageContentType(issueTypeId, messageContent);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(IssueTypeConstant.AbnormalBet, 1, true)]
    [InlineData(IssueTypeConstant.AbnormalBet, 0, false)]
    [InlineData(IssueTypeConstant.DangerAccepted, 0, false)]
    public void IsAbnormalBetMixParlay_ReturnResult(int issueTypeId, int? filterBy, bool expected)
    {
        var actual = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy);

        Assert.Equal(expected, actual);
    }
}