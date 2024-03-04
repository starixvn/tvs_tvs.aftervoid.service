using Fanex.Data;
using Fanex.Data.Repository;
using TicketVoidingSystem.Utilities;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class DeleteZzzBetTransCommand : NonQueryCommand
{
    [SpParam(Ignored = true)]
    public int SiteDbGroupId { get; set; }

    public string TransIds { get; set; }

    public int? Type { get; set; }

    public override string GetSettingKey() => Helper.GetSettingKeyBySiteDbGroupId(SiteDbGroupId, "TicketVoiding_zzzBettrans_Delete");

    public override bool IsValid() => !string.IsNullOrWhiteSpace(TransIds);
}