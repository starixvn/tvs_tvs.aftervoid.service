using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class MainRecoverProcessStageServiceTests
{
    [Theory]
    [InlineData("", true, false)]
    [InlineData("", false, false)]
    [InlineData("1", false, false)]
    [InlineData("1", true, true)]
    public async Task DeletedMessage_ReturnExpected(string transIds, bool isDeletedMessage, bool expected)
    {
        // Arrange
        var deletedMessageStage = Substitute.For<DeletedMessageStage>();

        deletedMessageStage.Execute(Arg.Any<VoidBetAutomationInfo>(), transIds).ReturnsForAnyArgs(isDeletedMessage);
        var service = new MainRecoverProcessStageServiceImp(deletedMessageStage, null, null, null);

        // Act
        var actual = await service.DeletedMessage(null, transIds);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task DeleteTransaction_ReturnExpected(bool isContinue, bool isDeleteTransaction, bool expected)
    {
        // Arrange
        var deleteTransactionStage = Substitute.For<DeleteTransactionStage>();

        deleteTransactionStage.Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<BetId>>()).ReturnsForAnyArgs(isDeleteTransaction);
        var service = new MainRecoverProcessStageServiceImp(null, deleteTransactionStage, null, null);

        // Act
        var actual = await service.DeleteTransaction(isContinue, null, null);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task UpdateTicketVoidingHistory_ReturnExpected(bool isContinue, bool isUpdateTicketVoidingHistory, bool expected)
    {
        // Arrange
        var updateTicketVoidingHistoryStage = Substitute.For<UpdateTicketVoidingHistoryStage>();
        updateTicketVoidingHistoryStage.Execute(Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(isUpdateTicketVoidingHistory);
        var service = new MainRecoverProcessStageServiceImp(null, null, null, updateTicketVoidingHistoryStage);

        // Act
        var actual = await service.UpdateTicketVoidingHistory(isContinue, default);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task SendEmail_ReturnExpected(bool isContinue, bool isSendEmail, bool expected)
    {
        // Arrange
        var sendEmailRecoverStage = Substitute.For<SendEmailRecoverStage>();

        sendEmailRecoverStage.Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<TicketRecoverEmailModel>>()).ReturnsForAnyArgs((isSendEmail, new List<Result>()));
        var service = new MainRecoverProcessStageServiceImp(null, null, sendEmailRecoverStage, default);

        // Act
        var (actual, _) = await service.SendEmail(isContinue, null, null);

        // Assert
        Assert.Equal(expected, actual);
    }
}