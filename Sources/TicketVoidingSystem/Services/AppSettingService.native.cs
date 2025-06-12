using Microsoft.Extensions.Configuration;
using TicketVoidingSystem.Models;

namespace TicketVoidingSystem.Services;

public static class AppSettingService
{
    public static AppSettingModel AppSetting { get; set; }

    public static void InitSetting(IConfiguration configuration)
    {
        var setting = new AppSettingModel
        {
            RedisConfig = new RedisConfig
            {
                EndPoint = configuration.GetValue<string>($"{nameof(RedisConfig)}:{nameof(RedisConfig.EndPoint)}"),
                Password = configuration.GetValue<string>($"{nameof(RedisConfig)}:{nameof(RedisConfig.Password)}"),
                DefaultDatabase = configuration.GetValue<int>($"{nameof(RedisConfig)}:{nameof(RedisConfig.DefaultDatabase)}"),
                SignalRChannelPrefix = configuration.GetValue<string>($"{nameof(RedisConfig)}:{nameof(RedisConfig.SignalRChannelPrefix)}"),
                SiteDbChannelPrefix = configuration.GetValue<string>($"{nameof(RedisConfig)}:{nameof(RedisConfig.SiteDbChannelPrefix)}")
            },
            LetstalkConfig = new LetstalkConfig
            {
                LetstalkServiceApi = configuration.GetValue<string>($"{nameof(LetstalkConfig)}:{nameof(LetstalkConfig.LetstalkServiceApi)}"),
                VoidSettledBets = configuration.GetValue<string>($"{nameof(LetstalkConfig)}:{nameof(LetstalkConfig.VoidSettledBets)}"),
                VoidAbnormalBets = configuration.GetValue<string>($"{nameof(LetstalkConfig)}:{nameof(LetstalkConfig.VoidAbnormalBets)}"),
                TicketListingAlert = configuration.GetValue<string>($"{nameof(LetstalkConfig)}:{nameof(LetstalkConfig.TicketListingAlert)}"),
                AbnormalBetRequest = configuration.GetValue<string>($"{nameof(LetstalkConfig)}:{nameof(LetstalkConfig.AbnormalBetRequest)}"),
                IsProdEnv = configuration.GetValue<bool>($"{nameof(LetstalkConfig)}:{nameof(LetstalkConfig.IsProdEnv)}")
            },
            TVSWebSiteUrl = configuration.GetValue<string>(nameof(AppSettingModel.TVSWebSiteUrl)),
            MTSApiBaseUrl = configuration.GetValue<string>(nameof(AppSettingModel.MTSApiBaseUrl)),
            ServiceName = configuration.GetValue<string>(nameof(AppSettingModel.ServiceName)),
            FanexRemotingRefServiceUrl = configuration.GetValue<string>(nameof(AppSettingModel.FanexRemotingRefServiceUrl)),
            IgnoreSiteDb = configuration.GetValue<string>(nameof(AppSettingModel.IgnoreSiteDb))
        };

        AppSetting = setting;
    }
}