using Fanex.TVS.AfterVoid.Service.Models;

namespace Fanex.TVS.AfterVoid.Service.Constants;

public static class OperatorConstant
{
    public const byte TraderType = 1;
    public const byte SuspenderType = 2;
    public const string TraderName = "Trader";
    public const string SuspenderName = "Suspender";

    public static readonly OperatorInfo TraderNA = new OperatorInfo
    {
        OperatorId = 0,
        OperatorType = TraderType,
        UserName = "N/A"
    };

    public static readonly OperatorInfo SuspenderNA = new OperatorInfo
    {
        OperatorId = 0,
        OperatorType = SuspenderType,
        UserName = "N/A"
    };
}