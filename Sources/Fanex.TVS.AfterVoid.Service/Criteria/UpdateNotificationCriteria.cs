using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class UpdateNotificationCriteria : CriteriaBase
{
    public int? IdList { get; set; }

    public int? RequestId { get; set; }

    public string Username { get; set; }

    public bool? IsRead { get; set; }

    public string Content { get; set; }

    public string Type { get; set; }

    public override string GetSettingKey() => "TicketVoiding_Notification_Update";

    public override bool IsValid() => true;
}