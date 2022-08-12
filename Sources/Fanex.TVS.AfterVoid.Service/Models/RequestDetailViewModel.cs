using System.Collections.Generic;

namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class RequestDetailViewModel
    {
        public RequestModel Request { get; set; }
        public IEnumerable<Message> Messages { get; set; }
        public RequestEventModel EventByMatch { get; set; }
    }
}