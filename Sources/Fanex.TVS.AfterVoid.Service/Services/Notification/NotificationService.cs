using System;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Microsoft.AspNetCore.SignalR;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.SignalR;

namespace Fanex.TVS.AfterVoid.Service.Services.Notification;

public interface NotificationService
{
    void ExecuteBackground(Func<Task> action);

    Task InsertNotification(string type, string content, int requestId);
}

public class NotificationServiceImp(
    IDynamicRepository repository,
    IHubContext<NotificationHub, ClientEvent> hubContext) : NotificationService
{
    public void ExecuteBackground(Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _ = LogService.ErrorAsync($"Notification background execution error: {ex.Message}");
            }
        });
    }

    public async Task InsertNotification(string type, string content, int requestId)
    {
        var criteria = new UpdateNotificationCriteria
        {
            RequestId = requestId,
            Username = "starixIT",
            Content = content,
            Type = type
        };

        await SaveAndPushNotificationToClient(criteria);
    }

    private async Task SaveAndPushNotificationToClient(UpdateNotificationCriteria criteria)
    {
        var (notifications, targetUsers) = await repository.FetchMultipleAsync<NotificationModel, string>(criteria);

        var tasks = targetUsers.Select(username =>
            hubContext.Clients.User(username).pushNotification(notifications.First()));
        _ = Task.WhenAll(tasks);
    }
}