using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Constants;

public static class TemplateContentConstant
{
    public static readonly TemplateContent PlacedFrom = new()
    {
        MsgEnglish = "placed from",
        MsgChineseTraditional = "投注從",
        MsgChineseSimplified = "投注从",
        MsgChineseNewSimplified = "投注从"
    };

    public static readonly TemplateContent Gmt8 = new()
    {
        MsgEnglish = "(GMT+8)",
        MsgChineseTraditional = "(北京時間)",
        MsgChineseSimplified = "(北京时间)",
        MsgChineseNewSimplified = "(北京时间)"
    };

    public static readonly TemplateContent Empty = new()
    {
        MsgEnglish = "",
        MsgChineseTraditional = "",
        MsgChineseSimplified = "",
        MsgChineseNewSimplified = ""
    };

    public static readonly TemplateContent Live = new()
    {
        MsgEnglish = "*Live*",
        MsgChineseTraditional = "*走地*",
        MsgChineseSimplified = "*走地*",
        MsgChineseNewSimplified = "*走地*"
    };
}