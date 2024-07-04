using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Constants;

public static class TemplateContentConstant
{
    public static readonly ContentByLanguageModel PlacedFrom = new()
    {
        ContentEnglish = "placed from",
        ContentChineseTraditional = "投注從",
        ContentChineseSimplified = "投注从",
        ContentNewChineseSimplified = "投注从"
    };

    public static readonly ContentByLanguageModel PlacedAt = new()
    {
        ContentEnglish = "placed at ",
        ContentChineseTraditional = "投注於 ",
        ContentChineseSimplified = "投注于 ",
        ContentNewChineseSimplified = "投注于 "
    };

    public static readonly ContentByLanguageModel Gmt8 = new()
    {
        ContentEnglish = "(GMT+8)",
        ContentChineseTraditional = "(北京時間)",
        ContentChineseSimplified = "(北京时间)",
        ContentNewChineseSimplified = "(北京时间)"
    };

    public static readonly ContentByLanguageModel Empty = new()
    {
        ContentEnglish = "",
        ContentChineseTraditional = "",
        ContentChineseSimplified = "",
        ContentNewChineseSimplified = ""
    };

    public static readonly ContentByLanguageModel Live = new()
    {
        ContentEnglish = "*Live*",
        ContentChineseTraditional = "*走地*",
        ContentChineseSimplified = "*走地*",
        ContentNewChineseSimplified = "*走地*"
    };
}