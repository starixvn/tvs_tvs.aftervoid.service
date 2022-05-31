namespace Fanex.TVS.AfterVoid.Core.Models
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
}