using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Services.Notification;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.SignalR;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Notification;

public class NotificationServiceTest
{
    private readonly IDynamicRepository repository = Substitute.For<IDynamicRepository>();
    private readonly IHubContext<NotificationHub, ClientEvent> hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
    private readonly NotificationServiceImp service;

    public NotificationServiceTest()
    {
        service = new NotificationServiceImp(repository, hubContext);
    }

    [Fact]
    public async Task InsertUpdateRequest_ShouldPushNotificationsToTargetUsers()
    {
        // Arrange
        const string type = "TestType";
        const string content = "TestContent";
        const int requestId = 123;

        var notifications = new List<NotificationModel>
        {
            new() { Id = 1, Content = content, Type = type }
        };
        var targetUsers = new List<string> { "user1", "user2" }; // Includes the current user
        repository.FetchMultipleAsync<NotificationModel, string>(Arg.Any<UpdateNotificationCriteria>())
            .Returns((notifications, targetUsers));

        // Act
        await service.InsertNotification(type, content, requestId);

        // Assert
        await hubContext.Clients.Received(1).User("user1").pushNotification(notifications[0]);
        await hubContext.Clients.Received(1).User("user2").pushNotification(notifications[0]);
    }
}