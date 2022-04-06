using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Caching.Redis;
using Fanex.Data;
using Fanex.Data.Repository;
using Fanex.Data.SqlServer;
using Fanex.LetstalkService.Client;
using Fanex.Logging;
using Fanex.Logging.Sentry;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Services;
using Fanex.TVS.AfterVoid.Core.SendEmail.Services.Subject;
using Fanex.TVS.AfterVoid.Core.SendEmail.Services.Void;
using Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels;
using Fanex.TVS.AfterVoid.Core.Services;
using Fanex.TVS.AfterVoid.Core.Services.Main;
using Fanex.TVS.AfterVoid.Core.Utilities.AutoMapper;
using Fanex.TVS.AfterVoid.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Sentry;

namespace Fanex.TVS.AfterVoid.Service.Extensions
{
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

        public static void RegisterInternalServices(this IServiceCollection services)
        {
            services.AddSingleton<IDynamicRepository, DynamicRepository>();
            services.AddSingleton<LoggingService, LoggingServiceImp>();
            services.AddSingleton<AfterVoidService, AfterVoidServiceImp>();
            services.AddSingleton<VoidBetAutomationService, VoidBetAutomationServiceImp>();
            services.AddSingleton<RequestAutoVoidService, RequestAutoVoidServiceImp>();
            services.AddSingleton<RequestManualVoidService, RequestManualVoidServiceImp>();
            services.AddSingleton<RequestVoid14Service, RequestVoid14ServiceImp>();
            services.AddSingleton<BetListService, BetListServiceImp>();
            services.AddSingleton<HandleAfterVoidStepService, HandleAfterVoidStepServiceImp>();
            services.AddSingleton<MessageItemService, MessageItemServiceImp>();
            services.AddSingleton<NotificationLetstalkService, NotificationLetstalkServiceImp>();
            services.AddSingleton<NotificationSignalRService, NotificationSignalRServiceImp>();
            services.AddSingleton<SendMessageService, SendMessageServiceImp>();
            services.AddSingleton<SendEmailService, SendEmailServiceImp>();
            services.AddSingleton<LetstalkClientService, LetstalkClientServiceImp>();
            services.AddSingleton<TicketVoidingSystemApi, TicketVoidingSystemApiImp>();
            services.AddSingleton<AfterVoidEmailService, AfterVoidEmailServiceImp>();

            // Email service
            services.AddSingleton<IBetListCoreService, BetListCoreService>();
            services.AddSingleton<RecordVoidService<TicketEmailViewModel, TicketEmailModel>, RecordService>();
            services.AddSingleton<ITicketService, TicketService>();
            services.AddSingleton<SubjectService, SubjectServiceImp>();
            services.AddSingleton(typeof(EmailVoidService<,>), typeof(EmailVoidServiceImp<,>));
            services.AddSingleton<SitesEmailService, SitesEmailServiceImp>();
            services.AddSingleton<MailMessageService, MailMessageServiceImp>();
            services.AddSingleton<SendEmailService, SendEmailServiceImp>();
            services.AddSingleton<TemplateEmailService, TemplateEmailServiceImp>();

            services.AddSingleton<AfterVoidJob>();

            OnebookUMInitializer.InitializeUMClientManager();
            OnebookUMInitializer.RegisterServiceCollection(services);
        }

        public static void RegisterFanexCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRedisCache(configuration.GetSection("FanexCachingRedis"));
        }

        public static void RegisterAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(OrganizationProfile));
        }

        public static async Task StartScheduler(this IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            scheduler.JobFactory = new AfterVoidJobFactory(serviceProvider);

            var job = JobBuilder.Create<AfterVoidJob>().Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity("AfterVoidService", "AfterVoidService")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();
        }
    }
}