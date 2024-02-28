namespace Fanex.TVS.AfterVoid.Service.Models.Parameters;

public class CreateVoidReasonStageParameter
{
    public VoidBetAutomationInfo AutomationInfo { get; set; }
    public int SportTypeId { get; set; }
    public VoidDataXmlModel VoidData { get; set; }
}