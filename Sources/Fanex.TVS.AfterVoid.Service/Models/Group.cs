using System.Collections.Generic;

namespace Fanex.TVS.AfterVoid.Service.Models;

public class Group<T>
{
    public string Name { get; set; } = string.Empty;

    public int DisplayTotalTickets { get; set; }

    public List<T> Tickets { get; set; } = new();
}