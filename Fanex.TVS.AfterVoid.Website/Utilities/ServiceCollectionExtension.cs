using System;
using Fanex.Data.Repository;
using Fanex.LetstalkService.Client;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.AspNetCore;
using Fanex.RemotingRef;
using Fanex.SiteDbConnectionManager.Client;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoRecover;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestManualVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestVoid14;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using Fanex.TVS.AfterVoid.Service.Utilities.AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Utilities;

using CustomServices;
using Jobs;
using Service.Services.Notification;

public static class ServiceCollectionExtensions
{
    public static void LoadConfig(this IServiceCollection services, IConfiguration configuration)
    {
        AppSettingService.InitSetting(configuration);
    }

    public static void AddExceptionHandler(this IServiceCollection services)
    {
        var configuration = new LoggingConfiguration()
            .GetHttpHeaderInfo()
            .GetHttpSessionInfo();
        ExceptionHandler.UseConfig(configuration);
    }

    public static void AddRemotingRef(this IServiceCollection services)
    {
        RefDataTypeContainer
            .ConnectTo(new Uri(AppSettingService.AppSetting.FanexRemotingRefServiceUrl))
            .FromApplication(AppSettingService.AppSetting.ServiceName)
            .RegisterSupportedLanguages(LanguageCode.English, LanguageCode.ChineseTraditional, LanguageCode.ChineseSimplified, LanguageCode.NewChineseSimplified);
    }

    public static void AddLetstalk(this IServiceCollection services)
    {
        AppConfigService.Config.LetstalkServiceApi = AppSettingService.AppSetting.LetstalkConfig.LetstalkServiceApi;
    }

    public static void AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(OrganizationProfile));
    }

    public static void AddTvsSignalR(this IServiceCollection services)
    {
        services.AddSignalR().AddStackExchangeRedis(options =>
        {
            var redisConfig = AppSettingService.AppSetting.RedisConfig;
            options.Configuration.EndPoints.Add(redisConfig.EndPoint);
            options.Configuration.Password = redisConfig.Password;
            options.Configuration.DefaultDatabase = redisConfig.DefaultDatabase;
            options.Configuration.ChannelPrefix = RedisChannel.Literal(redisConfig.SignalRChannelPrefix);
        });
    }

    public static void AddTvsSiteDbStatus(this IServiceCollection services)
    {
        services.AddSiteDbConnectionManager();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = LazyRedisMultiplexer.GetRedisConfig();
            config.ChannelPrefix = RedisChannel.Literal(AppSettingService.AppSetting.RedisConfig.SiteDbChannelPrefix);
            return ConnectionMultiplexer.Connect(config);
        });
    }

    public static void AddBusinessService(this IServiceCollection services)
    {
        services.AddSingleton<IRemotingRefService, RemotingRefService>();
        services.AddSingleton<IDynamicRepository, DynamicRepository>();
        services.AddSingleton<LoggingService, LoggingServiceImp>();
        services.AddSingleton<AfterVoidService, AfterVoidServiceImp>();
        services.AddSingleton<VoidBetAutomationService, VoidBetAutomationServiceImp>();
        services.AddSingleton<RequestAutoVoidService, RequestAutoVoidServiceImp>();
        services.AddSingleton<RequestManualVoidService, RequestManualVoidServiceImp>();
        services.AddSingleton<RequestVoid14Service, RequestVoid14ServiceImp>();
        services.AddSingleton<MessageItemService, MessageItemServiceImp>();
        services.AddSingleton<SignalRService, SignalRServiceImp>();
        services.AddSingleton<MessageContentParameterService, MessageContentParameterServiceImp>();
        services.AddSingleton<SendMessageService, SendMessageServiceImp>();
        services.AddSingleton<LetstalkClientService, LetstalkClientServiceImp>();
        services.AddSingleton<AfterVoidEmailService, AfterVoidEmailServiceImp>();
        services.AddSingleton<SendLetstalkService, SendLetstalkServiceImp>();
        services.AddSingleton<AbnormalBetLetstalkService, AbnormalBetLetstalkServiceImp>();
        services.AddSingleton<TechnicalErrorProviderMistakeLetstalkService, TechnicalErrorProviderMistakeLetstalkServiceImp>();
        services.AddSingleton<SendLetstalkServiceStage, SendLetstalkServiceStageImp>();
        services.AddSingleton<SettledBetLetstalkService, SettledBetLetstalkServiceImp>();
        services.AddSingleton<UpdateTicketVoidingHistoryStage, UpdateTicketVoidingHistoryStageImp>();
        services.AddSingleton<SendBatchMessageStage, SendBatchMessageStageImp>();
        services.AddSingleton<InsertVoidedTransactionStage, InsertVoidedTransactionStageImp>();
        services.AddSingleton<CreateMessageStage, CreateMessageStageImp>();
        services.AddSingleton<SpecialPlatformService, SpecialPlatformServiceImp>();
        services.AddSingleton<MainProcessStageService, MainProcessStageServiceImp>();
        services.AddSingleton<CallCashOutApiStage, CallCashOutApiStageImp>();
        services.AddSingleton<CallSignalRStage, CallSignalRStageImp>();
        services.AddSingleton<CreateVoidReasonStage, CreateVoidReasonStageImp>();
        services.AddSingleton<SendEmailStage, SendEmailStageImp>();
        services.AddSingleton<SendMessageToAdminSiteStage, SendMessageToAdminSiteStageImp>();
        services.AddSingleton<UpdateEmailResultStage, UpdateEmailResultStageImp>();
        services.AddSingleton<WriteLogAfterVoidStage, WriteLogAfterVoidStageImp>();
        services.AddSingleton<RemotingRefWrapperService, RemotingRefWrapperServiceImp>();
        services.AddSingleton<MatchService, MatchServiceImp>();
        services.AddSingleton<DeleteMessageStage, DeleteMessageStageImp>();
        services.AddSingleton<SendEmailRecoverStage, SendEmailRecoverStageImp>();
        services.AddSingleton<DeleteTransactionStage, DeleteTransactionStageImp>();
        services.AddSingleton<DeleteZzzBetTransStage, DeleteZzzBetTransStageImp>();
        services.AddSingleton<RequestAutoRecoverService, RequestAutoRecoverServiceImp>();
        services.AddSingleton<MainRecoverProcessStageService, MainRecoverProcessStageServiceImp>();
        services.AddSingleton<NotificationService, NotificationServiceImp>();
        services.AddSingleton<CallMTSApiStage, CallMTSApiStageImp>();
    }

    public static IServiceCollection AddBackgroundService(this IServiceCollection services)
    {
        services.AddSingleton<AfterVoidJob>();
        services.AddSingleton<LetstalkJob>();
        services.AddSingleton<CheckServerStatusJob>();

        services.AddHostedService<AfterVoidJob>();
        services.AddHostedService<LetstalkJob>();
        services.AddHostedService<CheckServerStatusJob>();

        return services;
    }
}