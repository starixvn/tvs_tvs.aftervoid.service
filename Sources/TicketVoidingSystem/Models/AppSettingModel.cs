namespace TicketVoidingSystem.Models;

public class AppSettingModel
{
    public bool OnProcessingAfterVoid { get; set; }

    public bool OnProcessingLetstalk { get; set; }

    public RedisConfig RedisConfig { get; init; }

    public LetstalkConfig LetstalkConfig { get; init; }

    public string ServiceName { get; init; }

    public string TVSWebSiteUrl { get; init; }

    public string MTSApiBaseUrl { get; init; }

    public string FanexRemotingRefServiceUrl { get; init; }

    public string IgnoreSiteDb { get; init; }
}

public class RedisConfig
{
    public string EndPoint { get; init; }
    public string Password { get; init; }
    public int DefaultDatabase { get; init; }
    public string SignalRChannelPrefix { get; init; }
    public string SiteDbChannelPrefix { get; init; }
}

public class LetstalkConfig
{
    public string VoidSettledBets { get; init; }
    public string VoidAbnormalBets { get; init; }
    public string TicketListingAlert { get; init; }
    public string AbnormalBetRequest { get; init; }
    public LetstalkTargetName TargetName { get; init; }
    public string LetstalkServiceApi { get; init; }
    public bool IsProdEnv { get; init; }
}

public class LetstalkTargetName
{
    public string TicketVoidingSystem { get; init; }
    public string BCAutoVoid { get; init; }
}