using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class SendMessageCommand : NonQueryCommand
{
    public int CreatorId { get; set; }

    public string XmlMessage { get; set; }

    public int ErrorCode { get; set; }

    public string ErrorMsg { get; set; }

    public override string GetSettingKey() => "NAP_SpecialDelete_BatchMessage_Upload";

    public override bool IsValid() => !string.IsNullOrWhiteSpace(XmlMessage);
}