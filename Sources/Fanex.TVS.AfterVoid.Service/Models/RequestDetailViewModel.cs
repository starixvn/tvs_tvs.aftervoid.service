using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Models;

public class RequestDetailViewModel
{
    public RequestModel Request { get; set; }
    public IEnumerable<Message> Messages { get; set; }
    public RequestEventModel EventByMatch { get; set; }

    /// <summary>
    /// For init auto void template Message
    /// </summary>
    public ContentByLanguageModel PlayerNamesSelected { get; set; }

    public bool IsAllLiveTickets { get; set; }

    public ContentByLanguageModel SpecialBetTypeContent { get; set; }

    public int? SportType { get; set; }
}