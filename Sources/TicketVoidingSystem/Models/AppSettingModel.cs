namespace TicketVoidingSystem.Models;

public class AppSettingModel
{
    public bool OnProcessingAfterVoid { get; set; }

    public bool OnProcessingLetstalk { get; set; }

    public RedisConfig RedisConfig { get; init; }

    public FanexMailService FanexMailService { get; init; }

    public LetstalkConfig LetstalkConfig { get; init; }

    public string ServiceName { get; init; }

    public string SentryClientKey { get; init; }

    public string CategorySentryLog { get; init; }

    public string TicketVoidingSystemUrl { get; init; }

    public string TVSNewSiteUrl { get; init; }

    public string FanexRemotingRefServiceUrl { get; init; }

    public string IgnoreSiteDb { get; init; }
}

public class RedisConfig
{
    public string EndPoint { get; init; }
    public string Password { get; init; }
    public int DefaultDatabase { get; init; }
    public string SignalRChannelPrefix { get; init; }
}

public class FanexMailService
{
    public string MailingList { get; init; }
    public string AppMailId { get; init; }
    public string AppMailAddress { get; init; }
}

public class LetstalkConfig
{
    public string VoidSettledBets { get; init; }
    public string VoidAbnormalBets { get; init; }
    public string TicketListingAlert { get; init; }
    public string AbnormalBetRequest { get; init; }
    public string LetstalkServiceApi { get; init; }
    public bool IsProdEnv { get; init; }
}