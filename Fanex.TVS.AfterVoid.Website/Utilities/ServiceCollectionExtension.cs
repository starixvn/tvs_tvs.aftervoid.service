using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fanex.Caching.Redis;
using Fanex.Data;
using Fanex.Data.Repository;
using Fanex.Data.SqlServer;
using Fanex.LetstalkService.Client;
using Fanex.Logging;
using Fanex.Logging.Sentry;
using Fanex.MailDeliver.MailClient;
using Fanex.RemotingRef;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Utilities.AutoMapper;
using Fanex.TVS.AfterVoid.Website.Jobs;
using Fanex.TVS.AfterVoid.Website.Jobs.Listener;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Services;
using ONEbook.UM.Transport;
using Quartz;
using Sentry;

namespace Fanex.TVS.AfterVoid.Website.Utilities
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static void LoadConfig(this IServiceCollection services)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var text = File.ReadAllText("appsettings.json");
            AppConfigService.AppConfig = JsonConvert.DeserializeObject<ConfigSettingModel>(text);
        }

        public static void RegisterSentry(this IServiceCollection services, IConfiguration configuration)
        {
            LogManager
                .SetDefaultLogCategory(configuration.GetSection("CategorySentryLog").Value)
                .Use(new List<ILogEngine>
                {
                    new SentryLogging(new SentryEngineOptions
                    {
                        Dsn = new Dsn(configuration.GetSection("SentryClientKey").Value)
                    })
                });
        }

        public static void RegisterFanexData(this IServiceCollection services, IConfiguration configuration)
        {
            var connections = configuration.GetSection("ConnectionStrings")
                .GetChildren()
                .AsEnumerable()
                .ToDictionary(
                    x => x.Key,
                    y => new ConnectionConfiguration(y.Key, y.Value)
                );

            DbSettingProviderManager
                .StartNewSession()
                .Use(connections)
                .WithSqlServer()
                .UseDefaultDbSettingProvider(configuration.GetSection("StoredProcedurePath").Value)
                .Run();
        }

        public static void RegisterSignalR(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSignalR().AddStackExchangeRedis(options =>
            {
                options.Configuration.EndPoints.Add(configuration.GetSection("RedisConfig:EndPoint").Value);
                options.Configuration.Password = configuration.GetSection("RedisConfig:Password").Value;
                options.Configuration.DefaultDatabase = int.Parse(configuration.GetSection("RedisConfig:DefaultDatabase").Value);
                options.Configuration.ChannelPrefix = configuration.GetSection("RedisConfig:ChannelPrefix").Value;
            });
        }

        public static void RegisterONEbookUM(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(new UMClientConfiguration().Initialize(configuration));
            services.AddSingleton<IUMCacheService, UMCacheService>();
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
                .ConnectTo(new Uri(AppConfigService.AppConfig.FanexRemotingRefServiceUrl))
                .FromApplication(AppConfigService.AppConfig.ServiceName)
                .RegisterSupportedLanguages("en-US", "zh-TW", "zh-CN", "zh-Hans");
        }

        public static void RegisterMailService(this IServiceCollection services)
        {
            AppSettings.AppMailId = AppConfigService.AppConfig.AppMailId;
            AppSettings.AppMailAddress = AppConfigService.AppConfig.AppMailAddress;
        }

        public static void RegisterLetstalk(this IServiceCollection services)
        {
            LetstalkService.Core.Services.AppConfigService.Config.LetstalkServiceApi = AppConfigService.AppConfig.LetstalkServiceApi;
        }

        public static void RegisterAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(OrganizationProfile));
        }

        public static void RegisterInternalServices(this IServiceCollection services)
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
            services.AddSingleton<NotificationLetstalkService, NotificationLetstalkServiceImp>();
            services.AddSingleton<NotificationSignalRService, NotificationSignalRServiceImp>();
            services.AddSingleton<SendMessageService, SendMessageServiceImp>();
            services.AddSingleton<LetstalkClientService, LetstalkClientServiceImp>();
            services.AddSingleton<AfterVoidEmailService, AfterVoidEmailServiceImp>();
        }

        public static void RegisterQuartzScheduler(this IServiceCollection services)
        {
            const int maxConcurrencyThread = 10;
            services.AddTransient<AfterVoidJob>();

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
}