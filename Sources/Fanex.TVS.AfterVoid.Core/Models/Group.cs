using System.Collections.Generic;

namespace Fanex.TVS.AfterVoid.Core.Models
{
    public class Group<T>
    {
        public Group()
        {
            Tickets = new List<T>();
            Name = string.Empty;
        }

        public string Name { get; set; }

        public int DisplayTotalTickets { get; set; }

        public List<T> Tickets { get; set; }
    }
}