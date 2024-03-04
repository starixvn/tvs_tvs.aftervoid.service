using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class DeleteMessageCommand : NonQueryCommand
{
    public string TransIds { get; set; }

    public int UserId { get; set; }

    public override string GetSettingKey() => "NAP_RecoverBet_MessageDel";

    public override bool IsValid() => UserId > 0 && !string.IsNullOrWhiteSpace(TransIds);
}