using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.LetstalkService.Client;
using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.AspNetCore;
using Fanex.Logging.Extensions.Database;
using Fanex.Logging.Sentry;
using Fanex.RemotingRef;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestManualVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestVoid14;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using Fanex.TVS.AfterVoid.Service.Utilities.AutoMapper;
using Fanex.TVS.AfterVoid.Website.Jobs;
using Fanex.TVS.AfterVoid.Website.Jobs.Listener;
using Fanex.TVS.AfterVoid.Website.Utilities.CustomServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Services;
using ONEbook.UM.Transport;
using Quartz;
using Sentry;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Utilities;

[ExcludeFromCodeCoverage(Justification = "non-logic")]
public static class ServiceCollectionExtensions
{
    public static void LoadConfig(this IServiceCollection services, IConfiguration configuration)
    {
        AppSettingService.InitSetting(configuration);
    }

    public static void RegisterSentry(this IServiceCollection services)
    {
        LogManager
            .SetDefaultLogCategory(AppSettingService.AppSetting.CategorySentryLog)
            .Use(new List<ILogEngine>
            {
                new SentryLogging(new SentryEngineOptions
                {
                    Dsn = new Dsn(AppSettingService.AppSetting.SentryClientKey)
                })
            });
    }

    public static void RegisterSignalR(this IServiceCollection services)
    {
        services.AddSignalR().AddStackExchangeRedis(options =>
        {
            options.Configuration.EndPoints.Add(AppSettingService.AppSetting.RedisConfig.EndPoint);
            options.Configuration.Password = AppSettingService.AppSetting.RedisConfig.Password;
            options.Configuration.DefaultDatabase = AppSettingService.AppSetting.RedisConfig.DefaultDatabase;
            options.Configuration.ChannelPrefix = AppSettingService.AppSetting.RedisConfig.SignalRChannelPrefix;
        });
    }

    public static void RegisterFanexExceptionHandler(this IServiceCollection services)
    {
        var configuration = new LoggingConfiguration()
            .GetHttpHeaderInfo()
            .GetHttpSessionInfo()
            .UseGetDbExceptionInfo(DatabaseInfoGetters.GetDbExceptionInfo);
        ExceptionHandler.UseConfig(configuration);
    }

    public static void RegisterONEbookUM(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new UMClientConfiguration().Initialize(configuration));
        services.AddSingleton<IUMCacheService, MemoryUMCacheService>();
        services.AddSingleton<IUMRestClient, UMRestClient>();
        services.AddSingleton<IPHelper, IPHelperImpl>();
        services.AddSingleton<IHttpContextHelper, CustomHttpContextHelper>();
        services.AddSingleton<IMaintenanceService, MaintenanceService>();
    }

    public static void RegisterFanexCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRedisCache(configuration.GetSection("FanexCachingRedis"));
    }

    public static void RegisterRemotingRef(this IServiceCollection services)
    {
        RefDataTypeContainer
            .ConnectTo(new Uri(AppSettingService.AppSetting.FanexRemotingRefServiceUrl))
            .FromApplication(AppSettingService.AppSetting.ServiceName)
            .RegisterSupportedLanguages(LanguageCode.English, LanguageCode.ChineseTraditional, LanguageCode.ChineseSimplified, LanguageCode.NewChineseSimplified);
    }

    public static void RegisterLetstalk(this IServiceCollection services)
    {
        LetstalkService.Core.Services.AppConfigService.Config.LetstalkServiceApi = AppSettingService.AppSetting.LetstalkConfig.LetstalkServiceApi;
    }

    public static void RegisterAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(OrganizationProfile));
    }

    public static void RegisterBusinessService(this IServiceCollection services)
    {
        services.AddSingleton<IDynamicRepository, DynamicRepository>();
        services.AddSingleton<LoggingService, LoggingServiceImp>();
        services.AddSingleton<AfterVoidService, AfterVoidServiceImp>();
        services.AddSingleton<VoidBetAutomationService, VoidBetAutomationServiceImp>();
        services.AddSingleton<RequestAutoVoidService, RequestAutoVoidServiceImp>();
        services.AddSingleton<RequestManualVoidService, RequestManualVoidServiceImp>();
        services.AddSingleton<RequestVoid14Service, RequestVoid14ServiceImp>();
        services.AddSingleton<AfterVoidProcessStage, AfterVoidProcessStageImp>();
        services.AddSingleton<MessageItemService, MessageItemServiceImp>();
        services.AddSingleton<NotificationSignalRService, NotificationSignalRServiceImp>();
        services.AddSingleton<SendMessageService, SendMessageServiceImp>();
        services.AddSingleton<LetstalkClientService, LetstalkClientServiceImp>();
        services.AddSingleton<AfterVoidEmailService, AfterVoidEmailServiceImp>();
        services.AddSingleton<SendLetstalkService, SendLetstalkServiceImp>();
        services.AddSingleton<AbnormalBetLetstalkService, AbnormalBetLetstalkServiceImp>();
        services.AddSingleton<SendLetstalkServiceStage, SendLetstalkServiceStageImp>();
        services.AddSingleton<SettledBetLetstalkService, SettledBetLetstalkServiceImp>();
        services.AddSingleton<UpdateTicketVoidingHistoryStage, UpdateTicketVoidingHistoryStageImp>();
        services.AddSingleton<SendBatchMessageStage, SendBatchMessageStageImp>();
        services.AddSingleton<InsertVoidedTransactionStage, InsertVoidedTransactionStageImp>();
        services.AddSingleton<CreateMessageStage, CreateMessageStageImp>();
        services.AddSingleton<SpecialPlatformService, SpecialPlatformServiceImp>();
    }

    public static void RegisterQuartzScheduler(this IServiceCollection services)
    {
        const int maxConcurrencyThread = 10;
        services.AddTransient<AfterVoidJob>();
        services.AddTransient<LetstalkJob>();

        services.AddQuartz(q =>
        {
            // handy when part of cluster or you want to otherwise identify multiple schedulers
            q.SchedulerId = "Scheduler-TVS-After-Void-Service";
            q.UseMicrosoftDependencyInjectionJobFactory();

            // these are the defaults
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = maxConcurrencyThread;
            });

            q.ScheduleJob<AfterVoidJob>(trigger => trigger
                .WithIdentity("AfterVoidJob Trigger")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
            );

            q.ScheduleJob<LetstalkJob>(trigger => trigger
                .WithIdentity("NotificationLetstalkJob Trigger")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
            );

            q.AddSchedulerListener<CustomSchedulerListener>();

            //// auto-interrupt long-running job
            //// q.UseJobAutoInterrupt(options =>
            //// {
            ////    // this is the default
            ////    options.DefaultMaxRunTime = TimeSpan.FromMinutes(5);
            //// });
        });

        services.AddQuartzHostedService(options =>
        {
            // when shutting down we want jobs to complete gracefully
            options.WaitForJobsToComplete = true;
        });
    }
}