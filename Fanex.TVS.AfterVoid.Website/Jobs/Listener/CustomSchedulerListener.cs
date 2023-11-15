using System;
using System.Threading;
using System.Threading.Tasks;
using Fanex.Logging;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Fanex.TVS.AfterVoid.Website.Jobs.Listener;

public class CustomSchedulerListener : ISchedulerListener
{
    private readonly ILogger<CustomSchedulerListener> logger;

    public CustomSchedulerListener(ILogger<CustomSchedulerListener> logger)
    {
        this.logger = logger;
    }

    public Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobUnscheduled(TriggerKey triggerKey, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task TriggerFinalized(ITrigger trigger, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task TriggerPaused(TriggerKey triggerKey, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task TriggersPaused(string triggerGroup, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task TriggerResumed(TriggerKey triggerKey, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task TriggersResumed(string triggerGroup, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobAdded(IJobDetail jobDetail, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobDeleted(JobKey jobKey, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobPaused(JobKey jobKey, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobInterrupted(JobKey jobKey, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobsPaused(string jobGroup, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobResumed(JobKey jobKey, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task JobsResumed(string jobGroup, CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task SchedulerError(string msg, SchedulerException cause,
        CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task SchedulerInStandbyMode(CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public async Task SchedulerStarted(CancellationToken cancellationToken = new())
    {
        var message = $"Fanex.TVS.AfterVoid.Service - Started - {DateTimeOffset.Now}";
        logger.LogInformation(message);
        await LogService.InfoAsync(message);
    }

    public Task SchedulerStarting(CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public async Task SchedulerShutdown(CancellationToken cancellationToken = new())
    {
        var message = $"Fanex.TVS.AfterVoid.Service - Shutdown!!! - {DateTimeOffset.Now}";
        logger.LogInformation(message);
        await LogService.InfoAsync(message);
    }

    public Task SchedulerShuttingdown(CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }

    public Task SchedulingDataCleared(CancellationToken cancellationToken = new())
    {
        return Task.CompletedTask;
    }
}