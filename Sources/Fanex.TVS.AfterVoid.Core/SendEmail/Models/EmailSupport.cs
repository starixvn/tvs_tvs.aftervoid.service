namespace Fanex.TVS.AfterVoid.Core.SendEmail.Models
{
    public interface EmailSupport
    {
        string EmailGroup { get; set; }

        string Site { get; set; }

        bool IsCashOut { get; set; }

        bool IsItSupport { get; set; }

        bool IsITSupportCreatedRequest { get; set; }

        int Currency { get; set; }
    }

    public interface EmailRecoverSupport
    {
        bool IsItSupport { get; set; }

        string CurrencyCode { get; set; }
    }
}